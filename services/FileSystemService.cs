using System.Diagnostics;
using Spectre.Console;
using termix.models;

namespace termix.Services;

public abstract class FileSystemService
{
    public static List<FileSystemItem> GetDirectoryContents(string path, SortBy sortBy, SortDirection sortDirection,
        bool groupDirectories, bool showHidden)
    {
        var items = new List<FileSystemItem>();
        var directoryInfo = new DirectoryInfo(path);

        if (directoryInfo.Parent != null)
            items.Add(new FileSystemItem(
                directoryInfo.Parent.FullName, "..", true, 0,
                directoryInfo.Parent.LastWriteTime, true
            ));

        var filteredEntries = directoryInfo.GetFileSystemInfos()
            .Where(e =>
            {
                if (showHidden) return true;
                if (e.Name.StartsWith('.') && e.Name != "..") return false;
                try
                {
                    return (e.Attributes & FileAttributes.Hidden) == 0;
                }
                catch
                {
                    return true;
                }
            })
            .ToList();

        var allItems = filteredEntries.Select(e => new FileSystemItem(
            e.FullName,
            e.Name,
            e is DirectoryInfo,
            e is FileInfo f ? f.Length : 0,
            e.LastWriteTime
        ));

        var primarySort = groupDirectories
            ? allItems.OrderByDescending(item => item.IsDirectory)
            : allItems.OrderBy(_ => 0);

        var sortedItems = sortBy switch
        {
            SortBy.Date => sortDirection == SortDirection.Ascending
                ? primarySort.ThenBy(item => item.LastModified)
                : primarySort.ThenByDescending(item => item.LastModified),
            SortBy.Size => sortDirection == SortDirection.Ascending
                ? primarySort.ThenBy(item => item.Size)
                : primarySort.ThenByDescending(item => item.Size),
            _ => sortDirection == SortDirection.Ascending
                ? primarySort.ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                : primarySort.ThenByDescending(item => item.Name, StringComparer.OrdinalIgnoreCase)
        };

        items.AddRange(sortedItems);
        return items;
    }

    public static void OpenFile(string filePath)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo(filePath) { UseShellExecute = true };
            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error opening file: {ex.Message}[/]");
            Console.ReadKey(true);
        }
    }
}