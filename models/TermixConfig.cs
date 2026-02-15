namespace termix.models;

public class TermixConfig
{
    public bool UseIcons { get; set; } = true;
    public bool ShowHiddenFiles { get; set; } = false;
    public SortBy DefaultSortBy { get; set; } = SortBy.Name;
    public SortDirection DefaultSortDirection { get; set; } = SortDirection.Ascending;
    public bool GroupDirectories { get; set; } = true;
}