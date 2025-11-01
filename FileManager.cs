using Spectre.Console;
using Spectre.Console.Rendering;
using termix.Handlers;
using termix.models;
using termix.Services;
using termix.UI;
using System.Collections.Concurrent;

namespace termix;

public class FileManager
{
    public readonly FileManagerState State = new();
    public readonly ActionHandler ActionHandler;
    public readonly NavigationHandler NavigationHandler;
    public readonly FilterHandler FilterHandler;
    private readonly InputHandler _inputHandler;
    private readonly DoubleBufferedRenderer _doubleBuffer = new();
    private readonly FilePreviewService _filePreviewService;
    private readonly FileManagerRenderer _renderer;

    private bool _needsRedraw = true;
    private bool _shouldQuit;

    private readonly ConcurrentQueue<Action> _uiActions = new();

    public readonly List<(string Text, SortBy By, SortDirection Dir, bool Group)> SortOptions =
    [
        ("Name: A to Z", SortBy.Name, SortDirection.Ascending, true),
        ("Name: Z to A", SortBy.Name, SortDirection.Descending, true),
        ("Date: Newest First", SortBy.Date, SortDirection.Descending, true),
        ("Date: Oldest First", SortBy.Date, SortDirection.Ascending, true),
        ("Size: Largest First", SortBy.Size, SortDirection.Descending, true),
        ("Size: Smallest First", SortBy.Size, SortDirection.Ascending, true),
        ("Mixed Sort: Name A to Z", SortBy.Name, SortDirection.Ascending, false),
        ("Mixed Sort: Name Z to A", SortBy.Name, SortDirection.Descending, false),
        ("Mixed Sort: Date Newest First", SortBy.Date, SortDirection.Descending, false),
        ("Mixed Sort: Date Oldest First", SortBy.Date, SortDirection.Ascending, false)
    ];

    public FileManager(bool useIcons)
    {
        var iconProvider = new IconProvider(useIcons);
        _renderer = new FileManagerRenderer(iconProvider);
        _filePreviewService = new FilePreviewService(iconProvider);
        var bookmarkService = new BookmarkService();
        ActionHandler = new ActionHandler(this, bookmarkService);
        NavigationHandler = new NavigationHandler(this);
        FilterHandler = new FilterHandler(this);
        _inputHandler = new InputHandler(this);
    }

    public void ScheduleUiAction(Action action)
    {
        _uiActions.Enqueue(action);
    }

    public void Run()
    {
        AnsiConsole.Clear();
        RefreshDirectory(setInitialSelection: true);

        while (!_shouldQuit)
        {
            while (_uiActions.TryDequeue(out var action))
            {
                action.Invoke();
            }

            if (_needsRedraw)
            {
                _needsRedraw = false;
                var footerContent = CreateFooterRenderable();
                var layout = _renderer.GetLayout(this, footerContent);
                _doubleBuffer.Render(layout);
            }

            while (!Console.KeyAvailable && !_needsRedraw && !_shouldQuit && _uiActions.IsEmpty)
            {
                Thread.Sleep(50);
            }

            if (_shouldQuit) break;
            if (Console.KeyAvailable) _inputHandler.ProcessKey(Console.ReadKey(true));
        }
    }

    public void SetNeedsRedraw() => _needsRedraw = true;

    public void Quit(bool force = false)
    {
        if (force && State.IsOperationInProgress) State.OperationCts?.Cancel();
        _shouldQuit = true;
    }

    public void ResetToNormalMode()
    {
        State.DebounceCts.Cancel();
        State.IsDeepSearchRunning = false;
        State.CurrentMode = InputMode.Normal;
        State.InputText = "";
        State.PromptText = "";
        State.VisuallySelectedItems.Clear();
        State.VisuallySelectedBookmarks.Clear();
        SetNeedsRedraw();
    }

    public void RefreshDirectory(string? findAndSelect = null, bool preserveSelection = false,
        bool setInitialSelection = false)
    {
        var oldSelectedIndex = State.SelectedIndex;
        LoadCurrentDirectory();

        if (findAndSelect != null)
        {
            State.SelectedIndex = State.CurrentItems.FindIndex(item =>
                item.Name.Equals(findAndSelect, StringComparison.OrdinalIgnoreCase));
        }
        else if (preserveSelection)
        {
            State.SelectedIndex = Math.Clamp(oldSelectedIndex, 0, State.CurrentItems.Count - 1);
        }
        else if (setInitialSelection)
        {
            var firstSelectableIndex = State.CurrentItems.FindIndex(item => !item.IsParentDirectory);
            State.SelectedIndex = firstSelectableIndex != -1 ? firstSelectableIndex : 0;
            if (State.CurrentItems.Count == 0) State.SelectedIndex = -1;
        }

        if (State is { SelectedIndex: -1, CurrentItems.Count: > 0 }) State.SelectedIndex = 0;

        AdjustViewPort();
        UpdatePreview();
    }

    public void AdjustViewPort()
    {
        var pageSize = Console.WindowHeight - 12;
        pageSize = Math.Max(5, pageSize);
        State.ViewOffset = State.SelectedIndex < State.ViewOffset ? State.SelectedIndex :
            State.SelectedIndex >= State.ViewOffset + pageSize ? State.SelectedIndex - pageSize + 1 :
            State.ViewOffset;
        State.ViewOffset = Math.Clamp(State.ViewOffset, 0, Math.Max(0, State.CurrentItems.Count - pageSize));
        SetNeedsRedraw();
    }

    public void UpdatePreview(bool resetScroll = true)
    {
        if (resetScroll)
        {
            State.PreviewVerticalOffset = 0;
            State.PreviewHorizontalOffset = 0;
        }

        var selectedItem = State.SelectedIndex >= 0 && State.SelectedIndex < State.CurrentItems.Count
            ? State.CurrentItems[State.SelectedIndex]
            : null;
        State.CurrentPreview = selectedItem == null
            ? _filePreviewService.GetPreview(null, 0, 0)
            : _filePreviewService.GetPreview(selectedItem.Path, State.PreviewVerticalOffset,
                State.PreviewHorizontalOffset);
        SetNeedsRedraw();
    }

    private void LoadCurrentDirectory()
    {
        try
        {
            State.UnfilteredItems = FileSystemService.GetDirectoryContents(State.CurrentPath, State.SortBy,
                State.SortDirection, State.GroupDirectories);
            if (State.CurrentMode != InputMode.Filter) State.CurrentItems = [.. State.UnfilteredItems];
        }
        catch (Exception ex)
        {
            State.StatusMessage = $"[red]Error loading directory: {ex.Message.EscapeMarkup()}[/]";
            State.CurrentItems = [];
            State.UnfilteredItems = [];
            State.SelectedIndex = -1;
        }

        SetNeedsRedraw();
    }

    private IRenderable CreateFooterRenderable()
    {
        if (State.IsOperationInProgress)
        {
            var grid = new Grid().AddColumns(new GridColumn().NoWrap(), new GridColumn().PadLeft(1),
                new GridColumn().PadLeft(1));
            grid.AddRow(
                new Markup(State.ProgressTaskDescription ?? "Processing..."),
                new CustomProgressBar { Value = State.ProgressValue, Width = 30 },
                new Markup($"[bold]{State.ProgressValue:F0}%[/]")
            );
            return new Panel(grid)
                { Border = BoxBorder.Rounded, BorderStyle = new Style(Color.Yellow), Padding = new Padding(1, 1) };
        }

        if (State.StatusMessage != null)
        {
            var borderColor = Color.Fuchsia; 
            if (State.StatusMessage.Contains("[green]")) borderColor = Color.Green;
            if (State.StatusMessage.Contains("[red]")) borderColor = Color.Red;
            if (State.StatusMessage.Contains("[yellow]")) borderColor = Color.Yellow;

            return new Panel(new Markup(State.StatusMessage))
                { Border = BoxBorder.Rounded, BorderStyle = new Style(borderColor) };
        }

        switch (State.CurrentMode)
        {
            case InputMode.DeleteConfirm or InputMode.QuitConfirm or InputMode.CreateDirConfirm
                or InputMode.BookmarkDeleteConfirm or InputMode.PasteConflict:
                return new Panel(new Markup(State.PromptText))
                {
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Yellow)
                };

            case InputMode.Add or InputMode.Rename or InputMode.AddBookmark or InputMode.RenameBookmark:
                var simplePromptContent =
                    $"{State.PromptText.EscapeMarkup()}[yellow]{State.InputText.EscapeMarkup()}[/][grey]█[/]";
                return new Panel(new Markup(simplePromptContent))
                {
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Cyan1)
                };

            case InputMode.Filter:
                var searchIndicator = State.IsDeepSearchRunning ? "[grey](Searching...)[/]" : "";
                var filterPromptContent =
                    $"{State.PromptText.EscapeMarkup()}{searchIndicator} [yellow]{State.InputText.EscapeMarkup()}[/][grey]█[/] | [grey]Press[/] [cyan]Esc[/] [grey]to navigate results[/]";
                return new Panel(new Markup(filterPromptContent))
                {
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Cyan1)
                };

            case InputMode.BookmarkFilter:
                var bookmarkFilterPromptContent =
                    $"{State.PromptText.EscapeMarkup()}[yellow]{State.InputText.EscapeMarkup()}[/][grey]█[/] | [grey]Press[/] [cyan]Esc[/] [grey]to return to bookmark list[/]";
                return new Panel(new Markup(bookmarkFilterPromptContent))
                {
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Cyan1)
                };
        }

        var helpText = new Markup(GetFooterText());
        return new Panel(Align.Center(helpText)) { Border = BoxBorder.None };
    }

    private string GetFooterText()
    {
        switch (State.CurrentMode)
        {
            case InputMode.HelpScreen:
                return "[grey]Use[/] [cyan]↑↓/JK[/] [grey]to scroll | Press[/] [cyan]Esc[/] [grey]to close[/]";
            case InputMode.Visual:
                return
                    $"[bold yellow]-- VISUAL --[/] [grey]Selected:[/][yellow] {State.VisuallySelectedItems.Count} [/] | [cyan]a[/] [grey]Select All[/] |  [cyan]i[/] [grey]Inverse Selection[/] | [cyan]Space[/] [grey]Toggle[/] | [cyan]y[/] [grey]Yank[/] | [cyan]x[/] [grey]Move[/] | [cyan]d[/] [grey]Del[/] | [cyan]Esc[/] [grey]Cancel[/]";
            case InputMode.SortMenu:
                return
                    "[grey]Use[/] [cyan]↓↑/JK[/] [grey]to select[/] | [cyan]Enter[/] [grey]Apply[/] | [cyan]Esc[/] [grey]Cancel[/]";
            case InputMode.FilteredNavigation:
                return
                    "[grey]Use[/] [cyan]B[/] [grey]to return to search results[/] | [grey]Currently browsing from a search result.[/]";
            case InputMode.BookmarkMenu:
                return
                    "[grey]Use[/] [cyan]↑↓/JK[/] [grey]Move[/] | [cyan]Enter[/] [grey]Jump[/] | [cyan]s[/] [grey]Filter[/] | [cyan]r[/] [grey]Rename[/] | [cyan]d[/] [grey]Delete[/] | [cyan]v[/] [grey]Visual[/] | [cyan]Esc[/] [grey]Close[/]";
            case InputMode.BookmarkVisual:
                return
                    $"[bold yellow]-- VISUAL BOOKMARK --[/] [grey]Selected:[/][yellow] {State.VisuallySelectedBookmarks.Count} [/] | [cyan]Space[/] [grey]Toggle[/] | [cyan]d[/] [grey]Del[/] | [cyan]Esc[/] [grey]Cancel[/]";
            case InputMode.Normal when !string.IsNullOrEmpty(State.InputText):
                return
                    $"[grey]Results for '[yellow]{State.InputText.EscapeMarkup()}[/]'. Press [cyan]Esc[/] to clear, or [cyan]S[/] for new search.[/]";
            default:
                if (State.Clipboard == null)
                    return
                        "[grey]Use[/] [cyan]↓↑/JK[/] [grey]to move[/] | [cyan]Enter[/] [grey]to open[/] | [cyan]q[/] [grey]to quit[/] | [cyan]b[/] [grey]bookmarks[/] | [cyan]?[/] [grey]for help[/]";

                var mode = State.Clipboard.Mode == ClipboardMode.Copy ? "Yank" : "Move";
                var items = State.Clipboard.Items.Count == 1
                    ? State.Clipboard.Items[0].Name.EscapeMarkup()
                    : $"{State.Clipboard.Items.Count} items";

                return $"[grey]Clipboard ({mode}):[/] [yellow]{items}[/] | [cyan]p[/] Paste, [cyan]Esc[/] Clear";
        }
    }
}