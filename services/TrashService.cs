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

    public (ActionResponse Response, TrashEntry? Updated) RestoreEntry(TrashEntry entry)
    {
        var entries = LoadEntries();
        var (restored, failed) = RestoreMany([entry], entries);

        if (restored.Count > 0)
            return (new ActionResponse(true, $"[green]Restored '{entry.DisplayName.EscapeMarkup()}'[/]"), entry);

        var reason = failed.Count > 0 ? failed[0].Error : "unknown error";
        return (new ActionResponse(false, $"[red]Restore failed: {reason.EscapeMarkup()}[/]"), null);
    }

    private (List<TrashEntry> Restored, List<(TrashEntry Entry, string Error)> Failed) RestoreMany(
        List<TrashEntry> toRestore, List<TrashEntry> allEntries)
    {
        var restored = new List<TrashEntry>();
        var failed = new List<(TrashEntry, string)>();

        foreach (var entry in toRestore.OrderByDescending(e => e.DeletedAt))
        {
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

    public ActionResponse Purge(TrashEntry entry)
    {
        try
        {
            if (entry.IsDirectory && Directory.Exists(entry.TrashPath)) Directory.Delete(entry.TrashPath, true);
            else if (File.Exists(entry.TrashPath)) File.Delete(entry.TrashPath);

            var entries = LoadEntries();
            entries.RemoveAll(e => e.TrashPath == entry.TrashPath);
            SaveEntries(entries);

            return new ActionResponse(true, $"[green]Permanently deleted '{entry.DisplayName.EscapeMarkup()}'[/]");
        }
        catch (Exception ex)
        {
            return new ActionResponse(false, $"[red]Purge failed: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    public ActionResponse EmptyTrash()
    {
        try
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(_trashDir))
            {
                if (Directory.Exists(entry)) Directory.Delete(entry, true);
                else File.Delete(entry);
            }

            SaveEntries([]);
            return new ActionResponse(true, "[green]Trash emptied.[/]");
        }
        catch (Exception ex)
        {
            return new ActionResponse(false, $"[red]Failed to empty trash: {ex.Message.EscapeMarkup()}[/]");
        }
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
}