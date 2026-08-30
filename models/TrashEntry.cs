namespace termix.models;

public record TrashEntry(
    string OriginalPath,
    string TrashPath,
    bool IsDirectory,
    DateTime DeletedAt,
    Guid BatchId)
{
    public string DisplayName =>
        Path.GetFileName(OriginalPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
}