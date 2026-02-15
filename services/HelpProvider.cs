namespace termix.Services;

public static class HelpProvider
{
    public static readonly List<(string Keys, string Action, string Mode)> Keybindings =
    [
        ("-- Navigation --", "", ""),
        ("[cyan]↓[/], [cyan]j[/]", "Move selection down", "Normal / Visual"),
        ("[cyan]↑[/], [cyan]k[/]", "Move selection up", "Normal / Visual"),
        ("[cyan]l[/], [cyan]o[/], [cyan]Enter[/]", "Open file or enter directory", "Normal"),
        ("[cyan]h[/], [cyan]Backspace[/]", "Navigate to parent directory", "Normal"),
        ("[cyan]gg[/]", "Go to top of file list", "Normal"),
        ("[cyan]G[/]", "Go to bottom of file list", "Normal"),
        ("[cyan]Ctrl+d[/]", "Scroll down half a page", "Normal"),
        ("[cyan]Ctrl+u[/]", "Scroll up half a page", "Normal"),
        ("[cyan]Alt + Arrows[/]", "Scroll preview pane", "Normal"),
        ("-- File Operations --", "", ""),
        ("[cyan]y[/]", "Yank (copy) selected item(s)", "Normal / Visual"),
        ("[cyan]Y[/] (Shift+y)", "Yank full path to system clipboard", "Normal"),
        ("[cyan]x[/]", "Cut (mark for move) selected item(s)", "Normal / Visual"),
        ("[cyan]p[/]", "Paste yanked/cut item(s)", "Normal"),
        ("[cyan]d[/]", "Delete selected item(s) (with confirmation)", "Normal / Visual"),
        ("[cyan]a[/]", "Add a new file or directory", "Normal"),
        ("[cyan]r[/]", "Rename the selected item", "Normal"),
        ("[cyan]O[/] (Shift+o)", "Open with an external application", "Normal"),
        ("-- Bookmarks --", "", ""),
        ("[cyan]m[/]", "Create a bookmark for the current directory", "Normal"),
        ("[cyan]b[/]", "Open the bookmark menu", "Normal"),
        ("[cyan]s[/]", "Live filter bookmarks by name/path", "Bookmark Menu"),
        ("[cyan]r[/]", "Rename selected bookmark", "Bookmark Menu"),
        ("[cyan]d[/]", "Delete selected bookmark(s)", "Bookmark / Visual"),
        ("[cyan]v[/]", "Enter visual mode for bulk selection", "Bookmark Menu"),
        ("[cyan]Space[/]", "Toggle selection", "Bookmark Visual"),
        ("-- Modes & Application --", "", ""),
        ("[cyan].[/]", "Toggle hidden files (dotfiles)", "Normal"),
        ("[cyan]s[/]", "Search/filter current directory", "Normal"),
        ("[cyan]Esc[/]", "Clear filter, cancel input, or clear clipboard", "All"),
        ("[cyan]v[/]", "Enter/exit Visual mode for multi-select", "Normal"),
        ("[cyan]a[/]", "Select all items", "Visual"),
        ("[cyan]i[/]", "Invert selection", "Visual"),
        ("[cyan]t[/]", "Open the sort menu", "Normal"),
        ("[cyan]b[/]", "Go back to search results from browsed directory", "Filtered Nav"),
        ("[cyan]q[/]", "Quit the application", "All"),
        ("[cyan]?[/]", "Show/hide this help screen", "Normal")
    ];
}