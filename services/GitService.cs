using System.Diagnostics;

namespace termix.Services;

public static class GitService
{
    public static string? GetBranchName(string path)
    {
        try
        {
            var repoPath = FindGitRepository(path);
            if (repoPath == null)
            {
                return null;
            }

            var headFile = Path.Combine(repoPath, ".git", "HEAD");
            if (!File.Exists(headFile))
            {
                return null;
            }

            var headContent = File.ReadAllText(headFile).Trim();

            const string refPrefix = "ref: refs/heads/";
            if (headContent.StartsWith(refPrefix))
            {
                return headContent[refPrefix.Length..];
            }

            return headContent.Length > 7 ? headContent[..7] : headContent;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindGitRepository(string startPath)
    {
        var currentDirectory = new DirectoryInfo(startPath);
        while (currentDirectory != null)
        {
            if (Directory.Exists(Path.Combine(currentDirectory.FullName, ".git")))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return null;
    }

    public static Dictionary<string, string> GetRepoStatuses(string directoryPath)
    {
        var statuses = new Dictionary<string, string>();
        try
        {
            var repoRoot = FindGitRepository(directoryPath);
            if (repoRoot == null) return statuses;

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "status --porcelain --ignored=no --untracked-files=all",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = directoryPath
                }
            };

            process.Start();
            while (process.StandardOutput.ReadLine() is { } line)
            {
                if (line.Length < 4) continue;

                var state = line[..2];
                var filePath = line[3..].Trim('"');

                var fileName = Path.GetFileName(filePath);
                statuses[fileName] = state;
            }

            process.WaitForExit();
        }
        catch
        {
            /* Git not installed or not a repo */
        }

        return statuses;
    }
}