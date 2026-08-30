using termix.models;

namespace termix.UI;

public class InputHandler(FileManager fileManager)
{
    private readonly FileManagerState _state = fileManager.State;
    private string _keyBuffer = "";
    private DateTime _lastKeyTime = DateTime.MinValue;
    private const int KeyTimeoutMs = 1000; // 1 second timeout for multi-key sequences

    public void ProcessKey(ConsoleKeyInfo keyInfo)
    {
        _state.StatusMessage = null;

        if ((DateTime.Now - _lastKeyTime).TotalMilliseconds > KeyTimeoutMs)
        {
            _keyBuffer = "";
        }

        _lastKeyTime = DateTime.Now;

        if (_state.CurrentMode == InputMode.Normal && keyInfo.Key == ConsoleKey.Escape && _state.Clipboard != null)
        {
            fileManager.ActionHandler.ClearClipboard();
            return;
        }

        switch (_state.CurrentMode)
        {
            case InputMode.Normal or InputMode.FilteredNavigation:
                HandleNormalKeyPress(keyInfo);
                break;
            case InputMode.SortMenu:
                HandleSortMenuInput(keyInfo);
                break;
            case InputMode.Visual:
                HandleVisualModeKeyPress(keyInfo);
                break;
            case InputMode.BookmarkMenu:
                HandleBookmarkMenuInput(keyInfo);
                break;
            case InputMode.BookmarkFilter:
                HandleBookmarkFilterInput(keyInfo);
                break;
            case InputMode.BookmarkVisual:
                HandleBookmarkVisualModeInput(keyInfo);
                break;
            case InputMode.PasteConflict:
                HandlePasteConflictInput(keyInfo);
                break;
            case InputMode.CreateDirConfirm:
                HandleCreateDirConfirmation(keyInfo.Key);
                break;
            case InputMode.HelpScreen:
                HandleHelpScreenInput(keyInfo);
                break;
            case InputMode.OpenWithMenu:
                HandleOpenWithMenuInput(keyInfo);
                break;
            case InputMode.TrashMenu:
                HandleTrashMenuInput(keyInfo);
                break;
            default:
                HandleInputModeKeyPress(keyInfo);
                break;
        }
    }

    private void HandleVisualModeKeyPress(ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.Escape:
            case ConsoleKey.V:
                fileManager.ResetToNormalMode();
                break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.J:
                fileManager.NavigationHandler.MoveSelection(1);
                break;
            case ConsoleKey.UpArrow:
            case ConsoleKey.K:
                fileManager.NavigationHandler.MoveSelection(-1);
                break;
            case ConsoleKey.Spacebar:
                ToggleVisualSelection();
                break;
            case ConsoleKey.A: // Select all
                foreach (var item in _state.CurrentItems.Where(i => !i.IsParentDirectory))
                {
                    _state.VisuallySelectedItems.Add(item.Path);
                }

                fileManager.SetNeedsRedraw();
                break;
            case ConsoleKey.I: // Invert selection
                var allItems = _state.CurrentItems.Where(i => !i.IsParentDirectory).Select(i => i.Path).ToHashSet();
                var currentSelection = _state.VisuallySelectedItems.ToHashSet();
                allItems.SymmetricExceptWith(currentSelection);
                _state.VisuallySelectedItems.Clear();
                foreach (var path in allItems) _state.VisuallySelectedItems.Add(path);
                fileManager.SetNeedsRedraw();
                break;
            case ConsoleKey.Y:
                fileManager.ActionHandler.BeginCopy();
                break;
            case ConsoleKey.X:
                fileManager.ActionHandler.BeginMove();
                break;
            case ConsoleKey.D:
                fileManager.ActionHandler.BeginDelete();
                break;
        }
    }

    private void ToggleVisualSelection()
    {
        if (_state.SelectedIndex < 0 || _state.SelectedIndex >= _state.CurrentItems.Count) return;

        var item = _state.CurrentItems[_state.SelectedIndex];
        if (item.IsParentDirectory) return;

        if (_state.VisuallySelectedItems.Contains(item.Path))
        {
            _state.VisuallySelectedItems.Remove(item.Path);
        }
        else
        {
            _state.VisuallySelectedItems.Add(item.Path);
        }

        fileManager.SetNeedsRedraw();
    }

    private void HandleBookmarkVisualModeInput(ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.Escape:
            case ConsoleKey.V:
                _state.CurrentMode = InputMode.BookmarkMenu;
                _state.VisuallySelectedBookmarks.Clear();
                fileManager.SetNeedsRedraw();
                break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.J:
                fileManager.NavigationHandler.MoveBookmarkSelection(1);
                break;
            case ConsoleKey.UpArrow:
            case ConsoleKey.K:
                fileManager.NavigationHandler.MoveBookmarkSelection(-1);
                break;
            case ConsoleKey.Spacebar:
                fileManager.ActionHandler.ToggleBookmarkVisualSelection();
                break;
            case ConsoleKey.D:
                fileManager.ActionHandler.BeginDeleteBookmark();
                break;
        }
    }

    private void HandlePasteConflictInput(ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.S: // Skip
                fileManager.ActionHandler.ResolveConflict(ConflictResolution.None, replace: false);
                break;
            case ConsoleKey.L: // Skip aLl
                fileManager.ActionHandler.ResolveConflict(ConflictResolution.SkipAll, replace: false);
                break;
            case ConsoleKey.R: // Replace
                fileManager.ActionHandler.ResolveConflict(ConflictResolution.None, replace: true);
                break;
            case ConsoleKey.A: // Replace All
                fileManager.ActionHandler.ResolveConflict(ConflictResolution.ReplaceAll, replace: true);
                break;
            case ConsoleKey.Escape: // Cancel
                fileManager.ActionHandler.CancelPasteOperation();
                break;
        }
    }

    private void HandleNormalKeyPress(ConsoleKeyInfo keyInfo)
    {
        var key = keyInfo.Key;

        if (HandleMultiKeySequence(keyInfo)) return;
        switch (keyInfo.KeyChar)
        {
            case 'y':
                fileManager.ActionHandler.BeginCopy();
                return;
            case 'Y':
                fileManager.ActionHandler.YankDirectoryPath();
                return;
            case '?':
                fileManager.ActionHandler.ShowHelpScreen();
                return;
            case 'm':
                fileManager.ActionHandler.BeginAddBookmark();
                return;
            case 'b':
                fileManager.ActionHandler.OpenBookmarkMenu();
                return;
        }

        switch (key)
        {
            case ConsoleKey.Escape when !string.IsNullOrEmpty(_state.InputText):
                fileManager.FilterHandler.ClearFilter();
                return;
            case ConsoleKey.Q:
                fileManager.ActionHandler.RequestQuit();
                return;
            case ConsoleKey.V:
                _state.CurrentMode = InputMode.Visual;
                _state.VisuallySelectedItems.Clear();
                ToggleVisualSelection(); // Select current item on entering
                return;
            case ConsoleKey.B when _state.CurrentMode == InputMode.FilteredNavigation:
                fileManager.FilterHandler.ReturnToFilter();
                return;
        }

        if (HandleSelectionMovement(key, keyInfo.Modifiers)) return;

        switch (key)
        {
            case ConsoleKey.Enter:
            case ConsoleKey.L:
            case ConsoleKey.O when keyInfo.Modifiers != ConsoleModifiers.Shift:
                fileManager.NavigationHandler.OpenSelectedItem();
                break;
            case ConsoleKey.O when keyInfo.Modifiers == ConsoleModifiers.Shift:
                fileManager.ActionHandler.BeginOpenWithMenu();
                break;
            case ConsoleKey.OemPeriod:
                fileManager.ToggleHiddenFiles();
                break;
            case ConsoleKey.Backspace:
            case ConsoleKey.H:
                if (keyInfo.Modifiers == ConsoleModifiers.None)
                    fileManager.NavigationHandler.NavigateUp();
                else if (keyInfo.Modifiers == ConsoleModifiers.Control)
                    fileManager.ToggleHiddenFiles();
                break;
            case ConsoleKey.A: fileManager.ActionHandler.BeginAdd(); break;
            case ConsoleKey.R: fileManager.ActionHandler.BeginRename(); break;
            case ConsoleKey.D: fileManager.ActionHandler.BeginDelete(); break;
            case ConsoleKey.S: fileManager.FilterHandler.BeginFilter(); break;
            case ConsoleKey.T when keyInfo.Modifiers == ConsoleModifiers.Shift:
                fileManager.ActionHandler.OpenTrashMenu();
                break;
            case ConsoleKey.T:
                fileManager.ActionHandler.BeginSortMenu();
                break;
            case ConsoleKey.X: fileManager.ActionHandler.BeginMove(); break;
            case ConsoleKey.P: fileManager.ActionHandler.BeginPaste(); break;
            case ConsoleKey.U: fileManager.ActionHandler.UndoDelete(); break;
        }
    }


    private void HandleOpenWithMenuInput(ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.Escape:
            case ConsoleKey.Q:
                fileManager.ResetToNormalMode();
                break;
            case ConsoleKey.Enter:
                fileManager.ActionHandler.ExecuteSelectedOpenWith();
                break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.J:
                fileManager.NavigationHandler.MoveOpenWithMenuSelection(1);
                break;
            case ConsoleKey.UpArrow:
            case ConsoleKey.K:
                fileManager.NavigationHandler.MoveOpenWithMenuSelection(-1);
                break;
        }
    }

    private void HandleBookmarkMenuInput(ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.DownArrow:
            case ConsoleKey.J:
                fileManager.NavigationHandler.MoveBookmarkSelection(1);
                break;
            case ConsoleKey.UpArrow:
            case ConsoleKey.K:
                fileManager.NavigationHandler.MoveBookmarkSelection(-1);
                break;

            case ConsoleKey.Enter:
                fileManager.ActionHandler.NavigateToSelectedBookmark();
                break;
            case ConsoleKey.S:
                fileManager.ActionHandler.BeginFilterBookmarks();
                break;
            case ConsoleKey.R:
                fileManager.ActionHandler.BeginRenameBookmark();
                break;
            case ConsoleKey.D:
                fileManager.ActionHandler.BeginDeleteBookmark();
                break;
            case ConsoleKey.V:
                _state.CurrentMode = InputMode.BookmarkVisual;
                _state.VisuallySelectedBookmarks.Clear();
                fileManager.ActionHandler.ToggleBookmarkVisualSelection();
                break;

            case ConsoleKey.Escape:
                fileManager.ActionHandler.CloseBookmarkMenu();
                break;
        }
    }

    private void HandleBookmarkFilterInput(ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.Enter:
            case ConsoleKey.Escape:
                _state.CurrentMode = InputMode.BookmarkMenu;
                _state.PromptText = "";
                fileManager.SetNeedsRedraw();
                break;
            case ConsoleKey.Backspace when _state.InputText.Length > 0:
                _state.InputText = _state.InputText[..^1];
                fileManager.ActionHandler.FilterBookmarks(_state.InputText);
                break;
            default:
                if (!char.IsControl(keyInfo.KeyChar))
                {
                    _state.InputText += keyInfo.KeyChar;
                    fileManager.ActionHandler.FilterBookmarks(_state.InputText);
                }

                break;
        }
    }

    private bool HandleMultiKeySequence(ConsoleKeyInfo keyInfo)
    {
        var keyChar = keyInfo.KeyChar;

        if (keyChar == 'G')
        {
            fileManager.NavigationHandler.MoveSelectionToEdge(false);
            _keyBuffer = "";
            return true;
        }

        _keyBuffer += keyChar.ToString().ToLower();

        if (_keyBuffer == "gg")
        {
            fileManager.NavigationHandler.MoveSelectionToEdge(true);
            _keyBuffer = "";
            return true;
        }

        if (_keyBuffer.Length > 2)
        {
            _keyBuffer = keyChar.ToString().ToLower();
        }

        if (keyChar != 'g')
        {
            _keyBuffer = "";
        }

        return false;
    }

    private void HandleInputModeKeyPress(ConsoleKeyInfo keyInfo)
    {
        switch (_state.CurrentMode)
        {
            case InputMode.Filter:
                HandleFilterInput(keyInfo);
                break;
            case InputMode.Add or InputMode.Rename or InputMode.AddBookmark or InputMode.RenameBookmark:
                HandleStandardTextInput(keyInfo);
                break;
            case InputMode.DeleteConfirm:
                HandleDeleteConfirmation(keyInfo);
                break;
            case InputMode.BookmarkDeleteConfirm:
                HandleBookmarkDeleteConfirmation(keyInfo.Key);
                break;
            case InputMode.QuitConfirm:
                HandleQuitConfirmation(keyInfo.Key);
                break;
            case InputMode.TrashPurgeConfirm:
                HandleTrashPurgeConfirmation(keyInfo.Key);
                break;
            case InputMode.TrashEmptyConfirm:
                HandleTrashEmptyConfirmation(keyInfo.Key);
                break;
        }
    }

    private void HandleBookmarkDeleteConfirmation(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.Y:
                fileManager.ActionHandler.CommitDeleteBookmark();
                break;
            case ConsoleKey.N or ConsoleKey.Escape:
                _state.CurrentMode = InputMode.BookmarkMenu;
                fileManager.SetNeedsRedraw();
                break;
        }
    }

    private void HandleSortMenuInput(ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.Escape:
            case ConsoleKey.Q:
                fileManager.ResetToNormalMode();
                break;
            case ConsoleKey.Enter:
                fileManager.ActionHandler.ApplySort();
                break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.J:
                fileManager.NavigationHandler.MoveSortMenuSelection(1);
                break;
            case ConsoleKey.UpArrow:
            case ConsoleKey.K:
                fileManager.NavigationHandler.MoveSortMenuSelection(-1);
                break;
        }
    }

    private void HandleQuitConfirmation(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.Y: fileManager.Quit(true); break;
            case ConsoleKey.N or ConsoleKey.Escape: fileManager.ResetToNormalMode(); break;
        }
    }

    private void HandleCreateDirConfirmation(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.Y: fileManager.ActionHandler.ConfirmCreateDirectoryAndPaste(); break;
            case ConsoleKey.N or ConsoleKey.Escape: fileManager.ActionHandler.CancelCreateDirectoryAndPaste(); break;
        }
    }

    private void HandleFilterInput(ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.Enter:
                fileManager.FilterHandler.AcceptFilter();
                break;

            case ConsoleKey.UpArrow:
                fileManager.NavigationHandler.MoveSelection(-1);
                break;

            case ConsoleKey.DownArrow:
                fileManager.NavigationHandler.MoveSelection(1);
                break;

            case ConsoleKey.Escape:
                fileManager.FilterHandler.AcceptFilter();
                break;

            case ConsoleKey.Backspace when _state.InputText.Length > 0:
                fileManager.FilterHandler.UpdateFilter(_state.InputText[..^1]);
                break;

            default:
                if (!char.IsControl(keyInfo.KeyChar))
                    fileManager.FilterHandler.UpdateFilter(_state.InputText + keyInfo.KeyChar);
                break;
        }
    }

    private void HandleStandardTextInput(ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.Enter: fileManager.ActionHandler.CommitStandardTextInput(); break;
            case ConsoleKey.Escape:
                if (_state.CurrentMode is InputMode.AddBookmark or InputMode.RenameBookmark)
                {
                    _state.CurrentMode = InputMode.BookmarkMenu;
                    fileManager.SetNeedsRedraw();
                }
                else
                {
                    fileManager.ResetToNormalMode();
                }

                break;
            case ConsoleKey.Backspace when _state.InputText.Length > 0:
                _state.InputText = _state.InputText[..^1];
                fileManager.SetNeedsRedraw();
                break;
            default:
                if (!char.IsControl(keyInfo.KeyChar))
                {
                    _state.InputText += keyInfo.KeyChar;
                    fileManager.SetNeedsRedraw();
                }

                break;
        }
    }

    private void HandleDeleteConfirmation(ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.Y when keyInfo.Modifiers == ConsoleModifiers.Shift:
                fileManager.ActionHandler.CommitDelete(permanent: true);
                break;
            case ConsoleKey.Y:
                fileManager.ActionHandler.CommitDelete();
                break;
            case ConsoleKey.N or ConsoleKey.Escape:
                fileManager.ResetToNormalMode();
                break;
        }
    }

    private bool HandleSelectionMovement(ConsoleKey key, ConsoleModifiers modifier)
    {
        if (modifier == ConsoleModifiers.Control)
        {
            switch (key)
            {
                case ConsoleKey.D:
                    fileManager.NavigationHandler.ScrollSelection(1); // Scroll down (positive direction)
                    return true;
                case ConsoleKey.U:
                    fileManager.NavigationHandler.ScrollSelection(-1); // Scroll up (negative direction)
                    return true;
            }
        }

        if (modifier == ConsoleModifiers.Alt)
        {
            (int v, int h) offset = key switch
            {
                ConsoleKey.UpArrow or ConsoleKey.K => (-1, 0),
                ConsoleKey.DownArrow or ConsoleKey.J => (1, 0),
                ConsoleKey.LeftArrow or ConsoleKey.H => (0, -5),
                ConsoleKey.RightArrow or ConsoleKey.L => (0, 5),
                _ => (0, 0)
            };
            if (offset == (0, 0)) return false;
            fileManager.NavigationHandler.ScrollPreview(offset.v, offset.h);
            return true;
        }

        var direction = key switch
        {
            ConsoleKey.DownArrow or ConsoleKey.J => 1,
            ConsoleKey.UpArrow or ConsoleKey.K => -1,
            _ => 0
        };
        if (direction != 0)
        {
            fileManager.NavigationHandler.MoveSelection(direction);
            return true;
        }

        if (key is not (ConsoleKey.Home or ConsoleKey.End)) return false;
        fileManager.NavigationHandler.MoveSelectionToEdge(key == ConsoleKey.Home);
        return true;
    }

    private void HandleHelpScreenInput(ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.Escape:
                fileManager.ResetToNormalMode();
                break;
            case ConsoleKey.UpArrow:
            case ConsoleKey.K:
                fileManager.NavigationHandler.ScrollHelpScreen(-1);
                break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.J:
                fileManager.NavigationHandler.ScrollHelpScreen(1);
                break;
        }
    }

    private void HandleTrashMenuInput(ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.DownArrow:
            case ConsoleKey.J:
                fileManager.NavigationHandler.MoveTrashSelection(1);
                break;
            case ConsoleKey.UpArrow:
            case ConsoleKey.K:
                fileManager.NavigationHandler.MoveTrashSelection(-1);
                break;
            case ConsoleKey.Enter:
                fileManager.ActionHandler.RestoreSelectedTrashItem();
                break;
            case ConsoleKey.D:
                fileManager.ActionHandler.BeginPurgeTrashItem();
                break;
            case ConsoleKey.E when keyInfo.Modifiers == ConsoleModifiers.Shift:
                fileManager.ActionHandler.BeginEmptyTrash();
                break;
            case ConsoleKey.Escape:
            case ConsoleKey.Q:
                fileManager.ActionHandler.CloseTrashMenu();
                break;
        }
    }

    private void HandleTrashPurgeConfirmation(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.Y: fileManager.ActionHandler.CommitPurgeTrashItem(); break;
            case ConsoleKey.N or ConsoleKey.Escape:
                _state.PendingPurgeEntry = null;
                _state.CurrentMode = InputMode.TrashMenu;
                fileManager.SetNeedsRedraw();
                break;
        }
    }

    private void HandleTrashEmptyConfirmation(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.Y: fileManager.ActionHandler.CommitEmptyTrash(); break;
            case ConsoleKey.N or ConsoleKey.Escape:
                _state.CurrentMode = InputMode.TrashMenu;
                fileManager.SetNeedsRedraw();
                break;
        }
    }
}