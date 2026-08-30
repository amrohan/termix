using System.Text.Json;
using Spectre.Console;
using termix.models;

namespace termix.Services;

public class TrashService
{
    private readonly string _trashDir;
    private readonly string _manifestPath;

    public TrashService()
    {
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "termix");
        _trashDir = Path.Combine(configDir, "trash");
        Directory.CreateDirectory(_trashDir);
        _manifestPath = Path.Combine(configDir, "trash.json");
    }

    public List<TrashEntry> LoadEntries()
    {
        if (!File.Exists(_manifestPath)) return [];
        try
        {
            var json = File.ReadAllText(_manifestPath);
            var entries = JsonSerializer.Deserialize(json, TrashJsonContext.Default.ListTrashEntry) ?? [];
            return entries.OrderByDescending(e => e.DeletedAt).ToList();
        }
        catch
        {
            return [];
        }
    }

    private void SaveEntries(List<TrashEntry> entries)
    {
        var json = JsonSerializer.Serialize(entries, TrashJsonContext.Default.ListTrashEntry);
        File.WriteAllText(_manifestPath, json);
    }

    public (ActionResponse Response, TrashEntry? Entry) MoveToTrash(FileSystemItem item, Guid batchId)
    {
        try
        {
            var uniqueName =
                $"{Path.GetFileNameWithoutExtension(item.Name)}_{DateTime.Now:yyyyMMddHHmmssfff}{Path.GetExtension(item.Name)}";
            var trashPath = Path.Combine(_trashDir, uniqueName);

            MoveWithFallback(item.Path, trashPath, item.IsDirectory);
            var entry = new TrashEntry(item.Path, trashPath, item.IsDirectory, DateTime.Now, batchId);

            var entries = LoadEntries();
            entries.Add(entry);
            SaveEntries(entries);

            return (new ActionResponse(true, $"[green]Moved '{item.Name.EscapeMarkup()}' to trash[/]"), entry);
        }
        catch (Exception ex)
        {
            return (new ActionResponse(false, $"[red]Trash failed: {ex.Message.EscapeMarkup()}[/]"), null);
        }
    }

    public (List<TrashEntry> Restored, List<(TrashEntry Entry, string Error)> Failed) RestoreBatch(Guid batchId)
    {
        var entries = LoadEntries();
        var batch = entries.Where(e => e.BatchId == batchId).ToList();
        return RestoreMany(batch, entries);
    }

    private static void MoveWithFallback(string sourcePath, string destPath, bool isDirectory)
    {
        try
        {
            if (isDirectory) Directory.Move(sourcePath, destPath);
            else File.Move(sourcePath, destPath);
        }
        catch (IOException)
        {
            if (isDirectory)
            {
                CopyDirectory(sourcePath, destPath);
                Directory.Delete(sourcePath, true);
            }
            else
            {
                File.Copy(sourcePath, destPath);
                File.Delete(sourcePath);
            }
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)));
        foreach (var dir in Directory.GetDirectories(sourceDir))
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
    }

    public (List<TrashEntry> Restored, List<(TrashEntry Entry, string Error)> Failed) RestoreEntries(
        List<TrashEntry> entriesToRestore, Action<int, int, string>? onProgress = null)
    {
        var entries = LoadEntries();
        return RestoreMany(entriesToRestore, entries, onProgress);
    }

    private (List<TrashEntry> Restored, List<(TrashEntry Entry, string Error)> Failed) RestoreMany(
        List<TrashEntry> toRestore, List<TrashEntry> allEntries, Action<int, int, string>? onProgress = null)
    {
        var restored = new List<TrashEntry>();
        var failed = new List<(TrashEntry, string)>();
        var ordered = toRestore.OrderByDescending(e => e.DeletedAt).ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            var entry = ordered[i];
            onProgress?.Invoke(i + 1, ordered.Count, entry.DisplayName);

            try
            {
                if (File.Exists(entry.OriginalPath) || Directory.Exists(entry.OriginalPath))
                {
                    failed.Add((entry, "an item already exists at the original location"));
                    continue;
                }

                var parentDir = Path.GetDirectoryName(entry.OriginalPath);
                if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                    Directory.CreateDirectory(parentDir);

                MoveWithFallback(entry.TrashPath, entry.OriginalPath, entry.IsDirectory);
                restored.Add(entry);
                allEntries.RemoveAll(e => e.TrashPath == entry.TrashPath);
            }
            catch (Exception ex)
            {
                failed.Add((entry, ex.Message));
            }
        }

        SaveEntries(allEntries);
        return (restored, failed);
    }

    public ActionResponse PurgeMany(List<TrashEntry> entriesToPurge, Action<int, int, string>? onProgress = null)
    {
        try
        {
            var entries = LoadEntries();
            for (var i = 0; i < entriesToPurge.Count; i++)
            {
                var entry = entriesToPurge[i];
                onProgress?.Invoke(i + 1, entriesToPurge.Count, entry.DisplayName);

                if (entry.IsDirectory && Directory.Exists(entry.TrashPath)) Directory.Delete(entry.TrashPath, true);
                else if (File.Exists(entry.TrashPath)) File.Delete(entry.TrashPath);
                entries.RemoveAll(e => e.TrashPath == entry.TrashPath);
            }

            SaveEntries(entries);
            return new ActionResponse(true, $"[green]Permanently deleted {entriesToPurge.Count} item(s).[/]");
        }
        catch (Exception ex)
        {
            return new ActionResponse(false, $"[red]Purge failed: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    public ActionResponse EmptyTrash(Action<int, int, string>? onProgress = null)
    {
        try
        {
            var items = Directory.EnumerateFileSystemEntries(_trashDir).ToList();
            for (var i = 0; i < items.Count; i++)
            {
                var path = items[i];
                onProgress?.Invoke(i + 1, items.Count, Path.GetFileName(path));

                if (Directory.Exists(path)) Directory.Delete(path, true);
                else File.Delete(path);
            }

            SaveEntries([]);
            return new ActionResponse(true, "[green]Trash emptied.[/]");
        }
        catch (Exception ex)
        {
            return new ActionResponse(false, $"[red]Failed to empty trash: {ex.Message.EscapeMarkup()}[/]");
        }
    }
}