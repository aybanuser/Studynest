using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace StudyNest;

public partial class MainWindow : Window
{
    enum Tool { Type, Pen, Highlighter, Eraser }

    static readonly string[] Palette =
    {
        "#0A2D4A", "#1A8791", "#FF7A5C", "#7C4DFF",
        "#2E9E5B", "#FFE45E", "#FF8FB1", "#6EC8FF"
    };

    static Color Hex(string s) => (Color)ColorConverter.ConvertFromString(s);

    readonly Library _lib;
    readonly DispatcherTimer _saveTimer;
    readonly Stack<Stroke> _undo = new();
    readonly Stack<Stroke> _redo = new();

    Notebook? _notebook;
    NotePage? _current;
    bool _loading;

    Tool _tool = Tool.Type;
    Color _penColor = Hex("#0A2D4A");
    Color _highlightColor = Hex("#FFE45E");

    public MainWindow()
    {
        InitializeComponent();

        _lib = Storage.Load();
        BuildSwatches();

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); SaveCurrent(); };

        NotebookBox.ItemsSource = _lib.Notebooks;
        NotebookBox.SelectedIndex = 0;

        TypeTool.IsChecked = true;
    }

    // ---------- Tools ----------

    void BuildSwatches()
    {
        foreach (var hex in Palette)
        {
            var swatch = new RadioButton
            {
                GroupName = "swatch",
                Tag = hex,
                Background = new SolidColorBrush(Hex(hex)),
                Style = (Style)FindResource("SwatchButton"),
                ToolTip = hex
            };
            swatch.Click += Swatch_Click;
            SwatchPanel.Children.Add(swatch);
        }
    }

    void Tool_Checked(object sender, RoutedEventArgs e)
    {
        if (Ink == null) return; // fires during window construction

        _tool = sender == PenTool ? Tool.Pen
              : sender == HighlighterTool ? Tool.Highlighter
              : sender == EraserTool ? Tool.Eraser
              : Tool.Type;

        ApplyTool();
        SyncSwatch();
    }

    void ApplyTool()
    {
        double size = SizeSlider.Value;

        // The ink layer only takes the mouse or pen when an ink tool is active.
        Ink.IsHitTestVisible = _tool != Tool.Type;

        switch (_tool)
        {
            case Tool.Pen:
                Ink.EditingMode = InkCanvasEditingMode.Ink;
                Ink.DefaultDrawingAttributes = new DrawingAttributes
                {
                    Color = _penColor,
                    Width = size,
                    Height = size,
                    FitToCurve = true,
                    IsHighlighter = false,
                    StylusTip = StylusTip.Ellipse
                };
                break;

            case Tool.Highlighter:
                Ink.EditingMode = InkCanvasEditingMode.Ink;
                Ink.DefaultDrawingAttributes = new DrawingAttributes
                {
                    Color = _highlightColor,
                    Width = Math.Max(6, size * 1.5),
                    Height = Math.Max(20, size * 4),
                    FitToCurve = false,
                    IsHighlighter = true,
                    StylusTip = StylusTip.Rectangle
                };
                break;

            case Tool.Eraser:
                Ink.EditingMode = InkCanvasEditingMode.EraseByStroke;
                break;

            default:
                Ink.EditingMode = InkCanvasEditingMode.None;
                break;
        }

        bool inking = _tool is Tool.Pen or Tool.Highlighter;
        SizeSlider.IsEnabled = inking;
        SwatchPanel.IsEnabled = inking;
    }

    void SyncSwatch()
    {
        var current = _tool == Tool.Highlighter ? _highlightColor : _penColor;
        foreach (RadioButton swatch in SwatchPanel.Children)
            swatch.IsChecked = Hex((string)swatch.Tag) == current;
    }

    void Swatch_Click(object sender, RoutedEventArgs e)
    {
        var color = Hex((string)((RadioButton)sender).Tag);
        if (_tool == Tool.Highlighter) _highlightColor = color;
        else _penColor = color;
        ApplyTool();
    }

    void Size_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Ink == null) return;
        ApplyTool();
    }

    void HighlightText_Click(object sender, RoutedEventArgs e)
    {
        var selection = Editor.Selection;
        if (selection.IsEmpty) return;

        var yellow = Hex("#FFE45E");
        var existing = selection.GetPropertyValue(TextElement.BackgroundProperty);
        bool alreadyOn = existing is SolidColorBrush b && b.Color == yellow;

        selection.ApplyPropertyValue(TextElement.BackgroundProperty,
            alreadyOn ? DependencyProperty.UnsetValue : new SolidColorBrush(yellow));
        Editor.Focus();
    }

    void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;

        switch (e.Key)
        {
            case Key.D1: TypeTool.IsChecked = true; e.Handled = true; break;
            case Key.D2: PenTool.IsChecked = true; e.Handled = true; break;
            case Key.D3: HighlighterTool.IsChecked = true; e.Handled = true; break;
            case Key.D4: EraserTool.IsChecked = true; e.Handled = true; break;
            case Key.Z when _tool != Tool.Type: UndoInk(); e.Handled = true; break;
            case Key.Y when _tool != Tool.Type: RedoInk(); e.Handled = true; break;
        }
    }

    // ---------- Ink undo / redo ----------

    void Ink_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
    {
        _undo.Push(e.Stroke);
        _redo.Clear();
        MarkDirty();
    }

    void Ink_StrokeErased(object sender, RoutedEventArgs e) => MarkDirty();

    void UndoInk_Click(object sender, RoutedEventArgs e) => UndoInk();
    void RedoInk_Click(object sender, RoutedEventArgs e) => RedoInk();

    void UndoInk()
    {
        while (_undo.Count > 0)
        {
            var stroke = _undo.Pop();
            if (!Ink.Strokes.Contains(stroke)) continue; // already erased
            Ink.Strokes.Remove(stroke);
            _redo.Push(stroke);
            MarkDirty();
            return;
        }
    }

    void RedoInk()
    {
        if (_redo.Count == 0) return;
        var stroke = _redo.Pop();
        Ink.Strokes.Add(stroke);
        _undo.Push(stroke);
        MarkDirty();
    }

    // ---------- Notebooks and pages ----------

    void NotebookBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SaveCurrent();
        _notebook = NotebookBox.SelectedItem as Notebook;
        PageList.ItemsSource = _notebook?.Pages;
        if (_notebook is { Pages.Count: > 0 }) PageList.SelectedIndex = 0;
    }

    void PageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SaveCurrent();
        if (PageList.SelectedItem is NotePage page) LoadPage(page);
    }

    void NewPage_Click(object sender, RoutedEventArgs e)
    {
        if (_notebook == null) return;
        var page = new NotePage { Title = $"Page {_notebook.Pages.Count + 1}" };
        _notebook.Pages.Add(page);
        PageList.SelectedItem = page;
        Editor.Focus();
    }

    void NewNotebook_Click(object sender, RoutedEventArgs e)
    {
        var notebook = new Notebook { Name = $"Notebook {_lib.Notebooks.Count + 1}" };
        notebook.Pages.Add(new NotePage { Title = "Page 1" });
        _lib.Notebooks.Add(notebook);
        NotebookBox.SelectedItem = notebook;
    }

    void RenameNotebook_Click(object sender, RoutedEventArgs e)
    {
        if (_notebook == null) return;
        var name = Prompt.Ask(this, "Rename notebook", _notebook.Name);
        if (name == null) return;
        _notebook.Name = name;
        NotebookBox.Items.Refresh();
        MarkDirty();
    }

    void DeletePage_Click(object sender, RoutedEventArgs e)
    {
        if (_notebook == null || _current == null) return;

        var answer = MessageBox.Show($"Delete \"{_current.Title}\"? This cannot be undone.",
            "StudyNest", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;

        var doomed = _current;
        int index = _notebook.Pages.IndexOf(doomed);
        _current = null; // so the page being deleted is not saved again

        _notebook.Pages.Remove(doomed);
        if (_notebook.Pages.Count == 0) _notebook.Pages.Add(new NotePage { Title = "Page 1" });
        PageList.SelectedIndex = Math.Min(index, _notebook.Pages.Count - 1);

        TrySaveLibrary();
    }

    // ---------- Load and save ----------

    void LoadPage(NotePage page)
    {
        _loading = true;
        _current = page;

        TitleBox.Text = page.Title;

        var doc = new FlowDocument
        {
            PagePadding = new Thickness(0),
            LineHeight = 32,
            LineStackingStrategy = LineStackingStrategy.BlockLineHeight
        };
        Editor.Document = doc;

        if (!string.IsNullOrEmpty(page.TextXaml))
        {
            var range = new TextRange(doc.ContentStart, doc.ContentEnd);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(page.TextXaml));
            range.Load(ms, DataFormats.Xaml);
        }

        if (string.IsNullOrEmpty(page.InkBase64))
        {
            Ink.Strokes = new StrokeCollection();
        }
        else
        {
            using var ms = new MemoryStream(Convert.FromBase64String(page.InkBase64));
            Ink.Strokes = new StrokeCollection(ms);
        }

        _undo.Clear();
        _redo.Clear();
        _loading = false;
        StatusText.Text = "";
    }

    void SaveCurrent()
    {
        _saveTimer.Stop();
        if (_current == null || _loading) return;

        var range = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd);
        using (var ms = new MemoryStream())
        {
            range.Save(ms, DataFormats.Xaml);
            _current.TextXaml = Encoding.UTF8.GetString(ms.ToArray());
        }

        using (var ms = new MemoryStream())
        {
            Ink.Strokes.Save(ms);
            _current.InkBase64 = Convert.ToBase64String(ms.ToArray());
        }

        TrySaveLibrary();
    }

    void TrySaveLibrary()
    {
        try
        {
            Storage.Save(_lib);
            StatusText.Text = $"Saved {DateTime.Now:HH:mm}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Could not save: " + ex.Message;
        }
    }

    void MarkDirty()
    {
        if (_loading) return;
        StatusText.Text = "Saving…";
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    void Editor_TextChanged(object sender, TextChangedEventArgs e) => MarkDirty();

    void TitleBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || _current == null) return;
        _current.Title = TitleBox.Text;
        MarkDirty();
    }

    void Window_Closing(object? sender, CancelEventArgs e) => SaveCurrent();
}