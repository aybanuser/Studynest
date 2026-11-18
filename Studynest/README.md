# StudyNest (C# / WPF prototype)

A digital notebook for college students: type, draw and highlight on the same lined page.

## Run it

Requires Windows and the .NET 8 SDK.

```
cd StudyNest
dotnet run
```

Or open `StudyNest.csproj` in Visual Studio 2022 and press F5.

## What is in this version

- Notebooks and pages in a sidebar (new, rename notebook, delete page).
- Four tools in the toolbar: Type, Pen, Highlighter, Eraser (Ctrl+1 to Ctrl+4).
- Eight colours and a size slider for the pen and highlighter.
- Text formatting: bold, italic, underline, bullet list, and a Highlight button for selected text.
- Undo and redo for ink strokes (Ctrl+Z and Ctrl+Y while an ink tool is active).
- Autosave about a second after each change. Everything is stored in
  `%AppData%\StudyNest\library.json`.

## How it works

Each page is a lined sheet with a `RichTextBox` for typing and a transparent `InkCanvas` on top for
drawing. With the Type tool selected the ink layer ignores the mouse, so clicks reach the text.
With any other tool selected the ink layer takes over.

| File | Purpose |
| --- | --- |
| `App.xaml` | Colours (from the icon), lined paper brush, button styles |
| `MainWindow.xaml` | Layout: sidebar, toolbar, paper |
| `MainWindow.xaml.cs` | Tools, ink undo, notebooks and pages, loading and saving |
| `Models.cs` | `NotePage`, `Notebook`, `Library`, JSON storage, rename dialog |

## Ideas for the next version

- Search across all pages.
- Export a page to PDF.
- Image and PDF import to annotate lecture slides.
- Page templates (Cornell notes, grid, blank).
- Sync across devices.
