<div align="center">

# ⚡️ Termix

**A modern, high-performance file navigator for your terminal**
****
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE.txt)
[![NuGet Version](https://img.shields.io/nuget/v/Termix)](https://www.nuget.org/packages/Termix/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Termix)](https://www.nuget.org/packages/Termix/)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![GitHub release](https://img.shields.io/github/release/amrohan/termix.svg)](https://GitHub.com/amrohan/termix/releases/)
[![GitHub issues](https://img.shields.io/github/issues/amrohan/termix.svg)](https://GitHub.com/amrohan/termix/issues/)
[![GitHub stars](https://img.shields.io/github/stars/amrohan/termix.svg?style=social&label=Star)](https://GitHub.com/amrohan/termix/stargazers/)

Built with .NET 10 and Spectre.Console, Termix delivers a fluid, visually rich experience for navigating, searching, and
managing files and your most-used directories, all from your terminal.

**[Read the Full Documentation](https://termix.pages.dev)**

</div>

---

## Demo

https://github.com/user-attachments/assets/c7b47493-ed6b-4b29-b334-f11f65d2dd18

> **Watch the full walkthrough** → [Termix on Vimeo](https://vimeo.com/1105824424) _(1 minute)_

## Features

- **Intuitive Vim-Style Navigation**: Move efficiently with `h j k l`, `gg` / `G`, and `Ctrl+u` / `Ctrl+d`.
- **Real-time Recursive Search**: Press `s` and start typing to instantly filter your entire directory tree.
- **Advanced Bookmark System**: Press `m` to **mark** a directory, then press `b` to open a filterable menu and
  instantly jump to any saved location.
- **Powerful Visual Mode**: Press `v` to select multiple files and perform batch operations (yank, cut, delete) with
  ease.
- **Rich Previews**: View text files with syntax highlighting, render images in the terminal, and browse archive
  contents.
- **Smart Ignoring**: Automatically respects `.gitignore` files to keep your views clean and relevant.
- **Nerd Font Support**: Enhanced with icons for file types, with a graceful ASCII fallback for universal compatibility.
- **Cross-Platform**: A single, consistent experience on Windows, macOS, and Linux.

## Installation

### Recommended: Install via Script (All Platforms)

Our installation scripts automatically download the latest native, self-contained version of Termix, which does **not**
require the .NET SDK to be installed.

**macOS / Linux:**

```bash
# Install or update to the latest version
curl -fsSL https://raw.githubusercontent.com/amrohan/termix/main/install.sh | bash
```

**Windows (PowerShell):**

```powershell
# Install or update to the latest version
iex (iwr "https://raw.githubusercontent.com/amrohan/termix/main/install.ps1")
```

_For more options, like installing a specific version or uninstalling, see
the [Installation Guide](https://termix.pages.dev/guide/installation.html)._

### Alternative: .NET Global Tool

This method is for users who prefer to manage Termix through the `dotnet` tool ecosystem.

#### Requirements

- **Termix v3.0.0 and newer**: Requires the **.NET 10 SDK** or later.
- **Termix v2.x.x (e.g., v2.6.0)**: Requires the **.NET 9 SDK**.

#### Commands

To install the latest version (requires .NET 10 SDK):

```bash
dotnet tool install --global termix
```

To install a specific older version compatible with the .NET 9 SDK:

```bash
dotnet tool install --global termix --version 2.6.0
```

## Core Commands

Termix uses Vim-style, case-sensitive keybindings. **Press `?` in the app for a full, scrollable list.**

| Key(s)            | Action                                          |
|:------------------|:------------------------------------------------|
| `↑` / `k`         | Move selection up                               |
| `↓` / `j`         | Move selection down                             |
| `h` / `Backspace` | Navigate to parent directory                    |
| `l` / `Enter`     | Open file or enter directory                    |
| `gg` / `G`        | Jump to top / bottom of list                    |
| `s`               | Start real-time recursive search                |
| `O`               | Open in external editors/apps                   |
| `v`               | Enter/Exit **Visual Mode** for multi-select     |
| `m`               | **Mark (Add)** a new bookmark                   |
| `b`               | Open the **Bookmark** menu                      |
| `y`               | **Yank** (copy) selected item(s) to clipboard   |
| `Y`               | **Yank Path** to system clipboard               |
| `x`               | **Cut** (move) selected item(s) to clipboard    |
| `p`               | **Paste** from clipboard                        |
| `d`               | **Delete** selected item(s) (with confirmation) |
| `a`               | **Add** a new file or directory                 |
| `r`               | **Rename** selected item                        |
| `t`               | Open the interactive **sort** menu              |
| `?`               | **Show/Hide the Help Screen**                   |
| `q`               | **Quit** Termix                                 |

## Documentation

For a complete guide on workflows, features, and advanced usage, please visit the *
*[official documentation site](https://termix.pages.dev)**.

## Contributing

Contributions are welcome! Whether it's bug reports, feature requests, or code contributions, please feel free to get
involved.

- **Report a Bug or Request a Feature**: Please open an issue on
  the [GitHub Issues page](https://github.com/amrohan/termix/issues).
- **Contribute Code**: Fork the repository and submit a pull request. Please see
  our [CONTRIBUTING.md](https://github.com/amrohan/termix/blob/main/CONTRIBUTING.md) file for development setup and
  guidelines.

## Acknowledgements

- This project is made possible by the fantastic [Spectre.Console](https://spectreconsole.net/) library.
- File previews are enhanced by [ImageSharp](https://github.com/SixLabors/ImageSharp)
  and [SharpCompress](https://github.com/adamhathcock/sharpcompress).
- Icons are provided by the [Nerd Fonts](https://www.nerdfonts.com/) project.

## License

Termix is licensed under the [MIT License](./LICENSE.txt).
