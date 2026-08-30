using Spectre.Console;
using Spectre.Console.Rendering;
using termix.models;
using termix.Services;

namespace termix.UI;

public class FileManagerRenderer(IconProvider iconProvider)
{
    public Layout GetLayout(FileManager fm, IRenderable footerRenderable)
    {
        var header = CreateHeader(fm.State.CurrentPath);
        IRenderable body;

        switch (fm.State.CurrentMode)
        {
            case InputMode.SortMenu:
                body = CreateSortMenuBody(fm.SortOptions, fm.State.SortMenuSelectedIndex);
                break;
            case InputMode.OpenWithMenu: // Add this case
                body = CreateOpenWithMenuBody(fm.OpenWithOptions, fm.State.OpenWithMenuSelectedIndex);
                break;
            case InputMode.HelpScreen:
                body = CreateHelpScreen(fm.State);
                break;
            case InputMode.BookmarkMenu or InputMode.BookmarkFilter or InputMode.BookmarkVisual or InputMode.AddBookmark
                or InputMode.RenameBookmark or InputMode.BookmarkDeleteConfirm:
                body = CreateBookmarkMenuBody(fm.State);
                break;
            case InputMode.TrashMenu or InputMode.TrashPurgeConfirm or InputMode.TrashEmptyConfirm
                or InputMode.TrashVisual:
                body = CreateTrashMenuBody(fm.State);
                break;
            default:
                body = CreateFileBrowserBody(fm.State, fm.State.CurrentPreview);
                break;
        }

        var footer = new Panel(Align.Center(footerRenderable)) { Border = BoxBorder.None };
        return new Layout("Root")
            .SplitRows(
                new Layout("Header").Update(header).Size(3),
                new Layout("Body").Update(body),
                new Layout("Footer").Update(footer).Size(3)
            );
    }

    private static IRenderable CreateSortMenuBody(
        List<(string Text, SortBy By, SortDirection Dir, bool Group)> options,
        int selectedIndex)
    {
        var table = new Table()
            .Title("[bold]Sort Options[/]")
            .Border(TableBorder.Rounded)
            .BorderStyle("yellow");

        table.AddColumn("Options");

        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];
            var style = i == selectedIndex ? new Style(background: Color.DodgerBlue1) : Style.Plain;
            table.AddRow(new Markup(option.Text.EscapeMarkup(), style));
        }

        return new Align(table, HorizontalAlignment.Center, VerticalAlignment.Middle);
    }

    private static IRenderable CreateBookmarkMenuBody(FileManagerState state)
    {
        var title = "[bold]Bookmarks[/]";

        var table = new Table()
            .Title(title)
            .Border(TableBorder.Rounded)
            .BorderStyle("yellow")
            .Expand();

        table.AddColumn("Name", c => c.Width(1));
        table.AddColumn("Path", c => c.Width(3));

        if (state.FilteredBookmarks.Count == 0)
        {
            table.AddRow(new Markup("[grey]No bookmarks found...[/]"));
        }
        else
        {
            for (var i = 0; i < state.FilteredBookmarks.Count; i++)
            {
                var bookmark = state.FilteredBookmarks[i];
                var isSelected = i == state.BookmarkMenuSelectedIndex;
                var isVisuallySelected = state.VisuallySelectedBookmarks.Contains(bookmark.Name);

                var style = isSelected ? new Style(background: Color.DodgerBlue1)
                    : isVisuallySelected ? new Style(background: Color.Grey30)
                    : Style.Plain;

                var selectionMarker = isVisuallySelected ? "[yellow]*[/] " : "  ";

                table.AddRow(
                    new Markup(selectionMarker + bookmark.Name.EscapeMarkup(), style),
                    new Markup(bookmark.Path.EscapeMarkup(), style));
            }
        }

        return new Align(table, HorizontalAlignment.Center, VerticalAlignment.Top);
    }

    private static Panel CreateHeader(string currentPath)
    {
        var branchName = GitService.GetBranchName(currentPath);
        var branchInfo = "";
        if (!string.IsNullOrEmpty(branchName))
        {
            const string branchIcon = "\uE725";
            branchInfo = $" ([yellow]{branchIcon} {branchName.EscapeMarkup()}[/])";
        }

        var displayPath = currentPath.Length > 80 ? "..." + currentPath[^77..] : currentPath;
        var headerContent = new Markup($"[bold cyan3]\uE5FF {displayPath.EscapeMarkup()}[/]{branchInfo}");
        return new Panel(headerContent) { Border = BoxBorder.Rounded, BorderStyle = new Style(Color.Cyan1) };
    }

    private Layout CreateFileBrowserBody(FileManagerState state, IRenderable previewContent)
    {
        var fileTable = CreateFileTable(state);
        return new Layout("Body").SplitColumns(
            new Layout("FileList").Update(fileTable).Ratio(3),
            new Layout("Preview").Update(previewContent).Ratio(3)
        );
    }

    private Table CreateFileTable(FileManagerState state)
    {
        var table = new Table().Expand().Border(TableBorder.None);
        table.AddColumn("Name");
        table.AddColumn(new TableColumn("Size").RightAligned());
        table.AddColumn(new TableColumn("Modified").RightAligned());
        table.AddColumn(new TableColumn("").Width(1));

        var pageSize = Console.WindowHeight - 12;
        pageSize = Math.Max(5, pageSize);
        var visibleItems = state.CurrentItems.Skip(state.ViewOffset).Take(pageSize).ToList();

        for (var i = 0; i < visibleItems.Count; i++)
        {
            var item = visibleItems[i];
            var originalIndex = i + state.ViewOffset;
            var isHighlighted = originalIndex == state.SelectedIndex;
            var isVisuallySelected = state.VisuallySelectedItems.Contains(item.Path);

            var style = isHighlighted ? new Style(background: Color.DodgerBlue1)
                : isVisuallySelected ? new Style(background: Color.Grey30)
                : Style.Plain;

            var name = CreateNameMarkup(item, isVisuallySelected, state.GitStatuses);
            var scrollChar = GetScrollbarChar(i, state.CurrentItems.Count, pageSize, state.ViewOffset,
                visibleItems.Count);

            table.AddRow(
                new Markup(name, style),
                new Markup(item.FormattedSize, style),
                new Markup(item.FormattedDate, style),
                new Markup(scrollChar, style)
            );
        }

        return table;
    }

    private static string GetScrollbarChar(int currentIndex, int totalItems, int pageSize, int viewOffset,
        int visibleCount)
    {
        if (totalItems <= pageSize) return " ";

        if (currentIndex == 0 && viewOffset > 0) return "⬆";
        if (currentIndex == visibleCount - 1 && viewOffset + pageSize < totalItems) return "⬇";

        var thumbStart = (int)((double)viewOffset / totalItems * visibleCount);
        var thumbEnd = (int)((double)(viewOffset + pageSize) / totalItems * visibleCount);
        if (currentIndex >= thumbStart && currentIndex <= thumbEnd) return "█";

        return "║";
    }

    private string CreateNameMarkup(FileSystemItem item, bool isVisuallySelected,
        Dictionary<string, string> gitStatuses)
    {
        var icon = iconProvider.GetIcon(item);
        var name = item.Name.EscapeMarkup();
        var nameStyle = item.IsDirectory ? "bold" : "";
        var selectionMarker = isVisuallySelected ? "[yellow]*[/]" : " ";

        var gitMarkup = "";
        if (gitStatuses.TryGetValue(item.Name, out var status))
        {
            gitMarkup = status.Trim() switch
            {
                "??" => " [grey]?[/]", // Untracked
                "M" or " M" => " [yellow]M[/]", // Modified
                "A" or "A " => " [green]A[/]", // Added
                "D" or "D " => " [red]D[/]", // Deleted
                "R" or "R " => " [blue]R[/]", // Renamed
                _ => ""
            };
        }

        return $"{selectionMarker} {icon}  [{nameStyle}]{name}[/]{gitMarkup}";
    }

    public static void ShowError(string message)
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold red]Error:[/] [red]{message.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    private static IRenderable CreateHelpScreen(FileManagerState state)
    {
        var pageSize = Console.WindowHeight - 12;
        pageSize = Math.Max(5, pageSize);

        var totalItems = HelpProvider.Keybindings.Count;
        var maxOffset = Math.Max(0, totalItems - pageSize);

        var title = "[bold yellow]Keybindings[/]";
        if (state.HelpVerticalOffset > 0) title += " [grey]⬆[/]";
        if (state.HelpVerticalOffset < maxOffset) title += " [grey]⬇[/]";

        var table = new Table()
            .Title(title)
            .Border(TableBorder.Rounded)
            .BorderStyle("yellow")
            .Expand();

        table.AddColumn("[u]Key(s)[/]", c => c.Width(2));
        table.AddColumn("[u]Action[/]", c => c.Width(3));
        table.AddColumn("[u]Mode[/]", c => c.Width(2));

        var visibleItems = HelpProvider.Keybindings
            .Skip(state.HelpVerticalOffset)
            .Take(pageSize);

        foreach (var (keys, action, mode) in visibleItems)
        {
            if (keys.StartsWith("--") && keys.EndsWith("--"))
            {
                table.AddRow(new Markup($"[bold]{keys.Replace("--", "").Trim()}[/]", "yellow")).Centered();
            }
            else
            {
                table.AddRow(keys, action, mode);
            }
        }

        return new Align(table, HorizontalAlignment.Center, VerticalAlignment.Middle);
    }

    private static IRenderable CreateOpenWithMenuBody(
        List<(string Text, string Command)> options,
        int selectedIndex)
    {
        var table = new Table()
            .Title("[bold]Open With...[/]")
            .Border(TableBorder.Rounded)
            .BorderStyle("yellow");

        table.AddColumn("Program");

        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];
            var style = i == selectedIndex ? new Style(background: Color.DodgerBlue1) : Style.Plain;
            table.AddRow(new Markup(option.Text.EscapeMarkup(), style));
        }

        return new Align(table, HorizontalAlignment.Center, VerticalAlignment.Middle);
    }

    private static IRenderable CreateTrashMenuBody(FileManagerState state)
    {
        var table = new Table().Title("[bold]Trash[/]").Border(TableBorder.Rounded).BorderStyle("yellow").Expand();

        table.AddColumn("Name", c => c.Width(2));
        table.AddColumn("Original Location", c => c.Width(3));
        table.AddColumn(new TableColumn("Deleted").RightAligned().Width(2));

        if (state.TrashEntries.Count == 0)
        {
            table.AddRow(new Markup("[grey]Trash is empty.[/]"));
        }
        else
        {
            for (var i = 0; i < state.TrashEntries.Count; i++)
            {
                var entry = state.TrashEntries[i];
                var isSelected = i == state.TrashMenuSelectedIndex;
                var isVisuallySelected = state.VisuallySelectedTrashItems.Contains(entry.TrashPath);

                var style = isSelected ? new Style(background: Color.DodgerBlue1)
                    : isVisuallySelected ? new Style(background: Color.Grey30)
                    : Style.Plain;

                var marker = isVisuallySelected ? "[yellow]*[/] " : "  ";
                var originalDir = Path.GetDirectoryName(entry.OriginalPath) ?? "";

                table.AddRow(
                    new Markup(marker + entry.DisplayName.EscapeMarkup(), style),
                    new Markup(originalDir.EscapeMarkup(), style),
                    new Markup(entry.DeletedAt.ToString("yyyy-MM-dd HH:mm"), style));
            }
        }

        return new Align(table, HorizontalAlignment.Center, VerticalAlignment.Top);
    }
}