# Keyboard Shortcuts

Master all of Termix's keyboard shortcuts for maximum productivity. This comprehensive reference covers every key binding organized by function and context. To view this guide inside the app, press `?` at any time.

## Quick Reference Card

### Essential Shortcuts

| Key               | Action           | Key           | Action            |
|:------------------|:-----------------|:--------------|:------------------|
| `↑↓` / `kj`       | Move selection   | `Enter` / `l` | Open/Enter        |
| `Backspace` / `h` | Go up            | `s`           | Search            |
| `a`               | Add              | `r`           | Rename            |
| `y`               | Yank (Copy)      | `x`           | Move/Cut          |
| `p`               | Paste            | `d`           | Delete            |
| `m`               | **Bookmark** Add | `b`           | **Bookmark** Menu |
| `t`               | Sort             | `v`           | **Visual Mode**   |
| `Y`               | Yank Path        | `?`           | **Help Screen**   |
| `q`               | Quit             | `Esc`         | Cancel/Clear      |
| `O`               | Open in menu     | `Esc`         | Cancel/Clear      |

## Navigation Shortcuts

### Basic Movement

| Key      | Action          | Description               |
|:---------|:----------------|:--------------------------|
| `↑`      | Move up         | Select previous item      |
| `↓`      | Move down       | Select next item          |
| `k`      | Move up (Vim)   | Vim-style up movement     |
| `j`      | Move down (Vim) | Vim-style down movement   |
| `gg`     | Jump to top     | Select first item in list |
| `G`      | Jump to bottom  | Select last item in list  |
| `Ctrl+d` | Scroll down     | Scroll down half a page   |
| `Ctrl+u` | Scroll up       | Scroll up half a page     |

### Directory Navigation

| Key         | Action           | Description                  |
|:------------|:-----------------|:-----------------------------|
| `Enter`     | Open/Enter       | Open file or enter directory |
| `l`         | Open/Enter (Vim) | Alternative key for opening  |
| `Backspace` | Go up            | Move to parent directory     |
| `h`         | Go up (Vim)      | Vim-style parent navigation  |

### Preview Pane Control

| Key       | Action       | Description                  |
|:----------|:-------------|:-----------------------------|
| `Alt + ↑` | Scroll up    | Scroll preview content up    |
| `Alt + ↓` | Scroll down  | Scroll preview content down  |
| `Alt + ←` | Scroll left  | Scroll preview content left  |
| `Alt + →` | Scroll right | Scroll preview content right |

## File Operations

### Creation and Modification

| Key | Action | Description                               |
| :-- | :----- | :---------------------------------------- |
| `a` | Add    | Create new file or directory              |
| `r` | Rename | Rename selected item                      |
| `d` | Delete | Delete selected item(s) with confirmation |

### Clipboard and Batch Operations

| Key | Action          | Description                                   |
| :-- | :-------------- | :-------------------------------------------- |
| `v` | **Visual Mode** | Enter/Exit mode for selecting multiple items  |
| `y` | Yank (Copy)     | Yank selected item(s) to internal clipboard   |
| `x` | Cut (Move)      | Cut selected item(s) to internal clipboard    |
| `p` | Paste           | Paste from internal clipboard                 |
| `Y` | Yank Path       | Yank item's full path to **system clipboard** |

## Bookmark Management

| Key     | Context       | Action                  | Description                                               |
| :------ | :------------ | :---------------------- | :-------------------------------------------------------- |
| `m`     | Normal Mode   | **Mark** (Add Bookmark) | Save the current directory as a new bookmark.             |
| `b`     | Normal Mode   | **Open** Bookmark Menu  | Display the list of all saved bookmarks.                  |
| `Enter` | Bookmark Menu | Jump to Bookmark        | Navigate to the selected bookmark's directory.            |
| `s`     | Bookmark Menu | Filter Bookmarks        | Enter filter mode to search bookmarks by name or path.    |
| `r`     | Bookmark Menu | Rename Bookmark         | Edit the name of the selected bookmark.                   |
| `d`     | Bookmark Menu | Delete Bookmark         | Delete the selected bookmark(s) with confirmation.        |
| `v`     | Bookmark Menu | **Visual** Mode         | Enter visual mode to select multiple bookmarks to delete. |
| `Esc`   | Bookmark Menu | Close Menu              | Return to the file navigator.                             |

## Search and Filter

### Search Control

| Key   | Mode         | Action          | Description                                         |
| :---- | :----------- | :-------------- | :-------------------------------------------------- |
| `s`   | Normal       | Start search    | Enter search mode                                   |
| `Esc` | Search       | Apply filter    | Finish search, navigate results                     |
| `Esc` | Filtered     | Clear filter    | Remove filter, show all files                       |
| `b`   | Filtered Nav | Back to results | Return to search results after entering a directory |

### Search Input

| Key            | Mode   | Action                     |
| :------------- | :----- | :------------------------- |
| Character keys | Search | Add to query               |
| `Backspace`    | Search | Remove from query          |
| `Enter`        | Search | (Same as Esc) Apply filter |

## Application Control

### Session Management

| Key   | Action | Description               |
| :---- | :----- | :------------------------ |
| `q`   | Quit   | Exit Termix               |
| `?`   | Help   | Show/hide the help screen |
| `Esc` | Cancel | Cancel current operation  |

### Special Contexts

| Key | Context        | Action                            |
| :-- | :------------- | :-------------------------------- |
| `y` | Delete confirm | Confirm deletion                  |
| `n` | Delete confirm | Cancel deletion                   |
| `y` | Quit confirm   | Force quit with operations        |
| `n` | Quit confirm   | Cancel quit                       |
| `S` | Paste Conflict | **Skip** current file             |
| `L` | Paste Conflict | **Skip All** conflicting files    |
| `R` | Paste Conflict | **Replace** current file          |
| `A` | Paste Conflict | **Replace All** conflicting files |

## Mode-Specific Shortcuts

### Normal Mode

_Default mode for navigation and file operations_

| Category       | Keys                                       | Actions                |
| :------------- | :----------------------------------------- | :--------------------- |
| **Movement**   | `↑↓kj`, `gg`, `G`                          | Navigate through files |
| **Navigation** | `Enter/l`, `Backspace/h`                   | Open/close directories |
| **Operations** | `a`, `r`, `y`, `Y`, `x`, `p`, `d`          | File operations        |
| **Bookmarks**  | `m` (Mark), `b` (Open Menu)                | Bookmark management    |
| **Modes**      | `s` (Search), `t` (Sort), `v` (**Visual**) | Switch to other modes  |
| **App**        | `q` (Quit), `?` (Help)                     | Application control    |

### Visual Mode

_Active when selecting multiple files for batch operations_

| Key         | Action           | Notes                                           |
| :---------- | :--------------- | :---------------------------------------------- |
| `Space`     | Toggle Selection | Mark or unmark the highlighted item             |
| `a`         | Select All       | Select every item in the current view           |
| `i`         | Invert Selection | Deselect all selected and select all unselected |
| `y` / `x`   | Yank / Cut       | Yank/Cut all selected items to the clipboard    |
| `d`         | Delete           | Delete all selected items                       |
| `v` / `Esc` | Exit Mode        | Return to Normal Mode                           |

### Bookmark Menu Mode

_Active when viewing your list of saved bookmarks_

| Key         | Action                | Notes                                   |
| :---------- | :-------------------- | :-------------------------------------- |
| `↑↓` / `kj` | Move Selection        | Navigate the list of bookmarks          |
| `Enter`     | Jump to Bookmark      | Go to the selected bookmark's directory |
| `s`         | Filter Bookmarks      | Enter filter mode to search bookmarks   |
| `r`         | Rename Bookmark       |                                         |
| `d`         | Delete Bookmark       |                                         |
| `v`         | Enter **Visual** Mode | Select multiple bookmarks for deletion  |
| `Esc`       | Close Menu            | Return to the file navigator            |

### Bookmark Visual Mode

_Active when selecting multiple bookmarks for batch deletion_

| Key         | Action           | Notes                                   |
| :---------- | :--------------- | :-------------------------------------- |
| `Space`     | Toggle Selection | Mark or unmark the highlighted bookmark |
| `d`         | Delete           | Delete all selected bookmarks           |
| `v` / `Esc` | Exit Mode        | Return to the Bookmark Menu             |

## Context-Sensitive Behavior

### Esc Key Behavior

The `Esc` key behaves differently based on context:

| Context                    | Action                            |
| :------------------------- | :-------------------------------- |
| Normal mode with filter    | Clear active filter               |
| Normal mode with clipboard | Clear clipboard                   |
| Visual Mode                | Exit Visual Mode                  |
| Search mode                | Apply filter and enter navigation |
| Add/Rename mode            | Cancel operation                  |
| Delete/Quit confirm        | Cancel action                     |
| Paste Conflict             | Cancel paste operation            |
| **Bookmark Menu**          | Close the menu                    |
| **Bookmark Visual Mode**   | Exit Visual Mode                  |
| **Bookmark Filter Mode**   | Exit filter mode                  |

## Memory Aids

### Mnemonic Devices

| Key | Mnemonic          | Action                   |
| :-- | :---------------- | :----------------------- |
| `a` | **A**dd           | Create new item          |
| `b` | **B**ookmark      | Open bookmark menu       |
| `d` | **D**elete        | Delete item              |
| `m` | **M**ark          | Create a bookmark        |
| `p` | **P**aste         | Paste from clipboard     |
| `q` | **Q**uit          | Exit application         |
| `r` | **R**ename        | Rename item              |
| `s` | **S**earch        | Start search             |
| `t` | Sor**t**          | Open sort menu           |
| `v` | **V**isual        | Enter Visual Mode        |
| `x` | Cut (e**X**tract) | Move to clipboard        |
| `y` | **Y**ank          | Yank to clipboard (copy) |

## Troubleshooting Key Issues

### Keys Not Working

If shortcuts aren't responding:

1.  **Check terminal focus**: Ensure the terminal window has focus.
2.  **Verify key support**: Some terminals may not support all key combinations (e.g., `Alt`).
3.  **Check for conflicts**: Other applications might intercept keys.

All essential functions have alternative key bindings that work in any standard terminal.
