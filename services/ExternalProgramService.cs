using System.Diagnostics;

namespace termix.Services;

public static class ExternalProgramService
{
    public static ActionResponse Open(string command, string path)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = $"\"{path}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WorkingDirectory = path
                }
            };

            process.Start();
            return new ActionResponse(true, $"[green]Launched '{command}' for '{Path.GetFileName(path)}'[/]");
        }
        catch (Exception ex)
        {
            return new ActionResponse(false, $"[red]Error launching '{command}': {ex.Message}[/]");
        }
    }
}
