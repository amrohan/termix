# Installation

This guide explains how to install **Termix** on your system.
We offer multiple installation methods so you can choose the one that best fits your setup.

## Prerequisites

Before installing Termix, ensure you have:

- A terminal application (e.g., Windows Terminal, iTerm2, Warp, Kitty, GNOME Terminal)
- **(Optional, but highly recommended)** A [Nerd Font](https://www.nerdfonts.com/) for enhanced icon support

## Install via Script (All Platforms) <Badge type="tip" text="Recommended"/>

This method downloads the latest native, self-contained version of Termix, which does **not** require the .NET SDK to be installed. It's the simplest way to get started.

### macOS / Linux

Requirements:

- `curl`
- `unzip` or `tar`
- `jq`

```bash
# Install or update to the latest version
curl -fsSL https://raw.githubusercontent.com/amrohan/termix/main/install.sh | bash

# Install a specific version (e.g., v2.6.0)
curl -fsSL https://raw.githubusercontent.com/amrohan/termix/main/install.sh | bash -s v2.6.0

# Uninstall
curl -fsSL https://raw.githubusercontent.com/amrohan/termix/main/install.sh | bash -s uninstall
```

### Windows (PowerShell)

Requirements:

- PowerShell 5.1 or later (PowerShell Core 7+ recommended)

```powershell
# Install or update to the latest version
iex (iwr "https://raw.githubusercontent.com/amrohan/termix/main/install.ps1")

# Install a specific version (e.g., v2.6.0)
iex (iwr "https://raw.githubusercontent.com/amrohan/termix/main/install.ps1") -Tag v2.6.0

# Uninstall
iex (iwr "https://raw.githubusercontent.com/amrohan/termix/main/install.ps1") -Uninstall
```

## Install as a .NET Global Tool

This method is for users who prefer to manage packages within the .NET ecosystem.

### Requirements

- **Termix v3.0.0 and newer**: Requires the **.NET 10 SDK** or later.
- **Termix v2.x.x (e.g., v2.6.0)**: Requires the **.NET 9 SDK**.

### Installation Commands

```bash
# Install the latest version (requires .NET 10 SDK)
dotnet tool install --global termix

# To install a specific older version (e.g., for .NET 9 SDK)
dotnet tool install --global termix --version 2.6.0
```

### Usage

Run Termix:
```bash
termix
```

Update to the latest compatible version:
```bash
dotnet tool update --global termix
```

Uninstall:
```bash
dotnet tool uninstall --global termix
```

## Install from Source

Requires:
- **.NET 10 SDK** (to build the `main` branch)
- Git

```bash
# Clone the repository
git clone https://github.com/amrohan/termix.git
cd termix

# Build and install the tool from the local source
dotnet pack
dotnet tool install --global --add-source ./nupkg termix
```

## Font Setup (Recommended)

For the best visual experience with icons, you should install a [Nerd Font](https://www.nerdfonts.com/).

**Recommended Fonts:**
- FiraCode Nerd Font
- JetBrains Mono Nerd Font
- Cascadia Code Nerd Font

1. Download your chosen font from [nerdfonts.com](https://www.nerdfonts.com/).
2. Install the font on your operating system.
3. Configure your terminal application to use the newly installed font.

::: tip Icon Fallback
If a Nerd Font isn’t detected, Termix will gracefully fall back to standard ASCII characters, ensuring full functionality.
:::

## Command-Line Options

```bash
# Launch normally
termix

# Launch without Nerd Font icons
termix --no-icons
```

## Troubleshooting

### 'termix' Command Not Found

If your shell can't find the `termix` command after installing it as a .NET tool, it's likely a `PATH` issue.

- First, ensure the .NET tools directory is in your shell's `PATH`.
- Restart your terminal session for the changes to take effect.
- You can verify the tool was installed correctly by running:
  ```bash
  dotnet tool list --global
  ```

### .NET SDK Not Found

- To install or run the **latest version of Termix (v3.0.0+)** as a .NET tool, you need the **.NET 10 SDK**. You can download it from [dotnet.microsoft.com](https://dotnet.microsoft.com/).
- For older versions of Termix (v2.x.x), you will need the **.NET 9 SDK**.
- Check your installed SDK version with `dotnet --version`.

### Permission Errors during Installation

If you encounter permission errors, you may need to run the installation command with elevated privileges.

```bash
# On Linux/macOS
sudo dotnet tool install --global termix

# On Windows, open a new PowerShell or Command Prompt
# session as an Administrator and run the command.
dotnet tool install --global termix
```

::: info Version Info
**Current stable version:** 3.0.0

**Minimum .NET (for .NET tool):** 10.0

**Platforms:** Windows, macOS, Linux
:::
