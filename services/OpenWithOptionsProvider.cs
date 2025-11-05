using System.Runtime.InteropServices;

namespace termix.Services;

public abstract class OpenWithOptionsProvider
{
    public static List<(string Text, string Command)> GetOptions()
    {
        var options = new List<(string Text, string Command)>
        {
            ("Visual Studio Code", "code"),
            ("Neovim", "nvim"),
            ("Vim", "vim"),
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            options.Add(("Windows Explorer", "explorer"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            options.Add(("Finder", "open"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            options.Add(("Default File Manager", "xdg-open"));
        }

        return options;
    }
}