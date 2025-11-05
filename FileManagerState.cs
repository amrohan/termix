using Spectre.Console;
using Spectre.Console.Rendering;
using termix.models;

namespace termix;

public class FileManagerState
{
    public string CurrentPath { get; set; } = Directory.GetCurrentDirectory();
    public Stack<string> NavigationStack { get; } = new();
    public List<FileSystemItem> CurrentItems { get; set; } = [];
    public List<FileSystemItem> UnfilteredItems { get; set; } = [];
    public int SelectedIndex { get; set; } = -1;
    public int ViewOffset { get; set; }
    public InputMode CurrentMode { get; set; } = InputMode.Normal;
    public string InputText { get; set; } = "";
    public string PromptText { get; set; } = "";
    public string? StatusMessage { get; set; }
    public ClipboardItem? Clipboard { get; set; }
    public string AddBasePath { get; set; } = "";
    public string? PendingCreateDirectoryPath { get; set; }
    public int HelpVerticalOffset { get; set; }
    public IRenderable CurrentPreview { get; set; } = new Text("");
    public int PreviewVerticalOffset { get; set; }
    public int PreviewHorizontalOffset { get; set; }
    public bool IsOperationInProgress { get; set; }
    public string? ProgressTaskDescription { get; set; }
    public double ProgressValue { get; set; }
    public CancellationTokenSource? OperationCts { get; set; }
    public bool IsDeepSearchRunning { get; set; }
    public List<FileSystemItem>? RecursiveSearchCache { get; set; }
    public (string Path, string Filter, List<FileSystemItem> Items, int SelectedIndex)? SavedFilterState { get; set; }
    public CancellationTokenSource DebounceCts { get; set; } = new();

    public SortBy SortBy { get; set; } = SortBy.Name;
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
    public bool GroupDirectories { get; set; } = true;
    public int SortMenuSelectedIndex { get; set; }
    
    public int OpenWithMenuSelectedIndex { get; set; } 
    public HashSet<string> VisuallySelectedItems { get; } = [];
    public List<FileSystemItem> PendingDeleteItems { get; set; } = [];
    public PasteConflictState? CurrentConflict { get; set; }
    public ConflictResolution ActiveConflictResolution { get; set; } = ConflictResolution.None;
    public List<Bookmark> Bookmarks { get; set; } = [];
    public List<Bookmark> FilteredBookmarks { get; set; } = [];
    public int BookmarkMenuSelectedIndex { get; set; }
    public HashSet<string> VisuallySelectedBookmarks { get; } = new();
    public List<Bookmark> PendingDeleteBookmarks { get; set; } = [];
}

public enum ConflictResolution
{
    None,
    SkipAll,
    ReplaceAll
}

public record PasteConflictState(
    FileSystemItem SourceItem,
    string DestinationPath,
    Queue<FileSystemItem> RemainingItems,
    ClipboardMode OriginalMode,
    int TotalItems,
    int CurrentItemIndex
);