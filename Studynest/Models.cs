using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace StudyNest;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}

/// <summary>One notebook page: typed text (WPF XAML), plus ink strokes (ISF, base64).</summary>
public class NotePage : ObservableObject
{
    string _title = "Untitled page";

    public string Title { get => _title; set => Set(ref _title, value); }
    public string TextXaml { get; set; } = "";
    public string InkBase64 { get; set; } = "";
}

public class Notebook : ObservableObject
{
    string _name = "New notebook";

    public string Name { get => _name; set => Set(ref _name, value); }
    public ObservableCollection<NotePage> Pages { get; set; } = new();
}

public class Library
{
    public ObservableCollection<Notebook> Notebooks { get; set; } = new();
}

/// <summary>Reads and writes the whole library as JSON in %AppData%\StudyNest.</summary>
public static class Storage
{
    static readonly string FilePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "StudyNest", "library.json");

    public static Library Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var lib = JsonSerializer.Deserialize<Library>(File.ReadAllText(FilePath));
                if (lib is { Notebooks.Count: > 0 }) return lib;
            }
        }
        catch
        {
            // A damaged file falls through to a fresh library; the old file is overwritten on next save.
        }
        return Seed();
    }

    public static void Save(Library lib)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(lib));
    }

    static Library Seed()
    {
        var welcome = new NotePage
        {
            Title = "Welcome to StudyNest",
            TextXaml = Xaml(
                "Type here with the Type tool.",
                "Switch to Pen to sketch diagrams right on the page.",
                "Use Highlighter to mark key terms, and Eraser to remove a stroke.",
                "Select text and press Highlight to mark it in yellow.")
        };
        var nb = new Notebook { Name = "Getting started" };
        nb.Pages.Add(welcome);
        var lib = new Library();
        lib.Notebooks.Add(nb);
        return lib;
    }

    static string Xaml(params string[] paragraphs) =>
        "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xml:space=\"preserve\">" +
        string.Concat(paragraphs.Select(p => $"<Paragraph>{SecurityElement.Escape(p)}</Paragraph>")) +
        "</Section>";
}

/// <summary>A tiny "type a name" dialog used for renaming notebooks.</summary>
public static class Prompt
{
    public static string? Ask(Window owner, string title, string initial)
    {
        var box = new TextBox { Text = initial, FontSize = 14, Padding = new Thickness(6), Margin = new Thickness(0, 0, 0, 14) };
        var ok = new Button { Content = "OK", IsDefault = true, Width = 80, Padding = new Thickness(0, 6, 0, 6), Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new Button { Content = "Cancel", IsCancel = true, Width = 80, Padding = new Thickness(0, 6, 0, 6) };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);

        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(box);
        panel.Children.Add(buttons);

        var win = new Window
        {
            Title = title,
            Content = panel,
            Width = 360,
            SizeToContent = SizeToContent.Height,
            Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false
        };

        string? result = null;
        ok.Click += (_, _) => { result = box.Text.Trim(); win.DialogResult = true; };
        win.Loaded += (_, _) => { box.Focus(); box.SelectAll(); };
        win.ShowDialog();

        return string.IsNullOrWhiteSpace(result) ? null : result;
    }
}