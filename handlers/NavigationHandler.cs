using Spectre.Console;
using termix.models;
using termix.Services;

namespace termix.Handlers;

public class NavigationHandler(FileManager fileManager)
{
    private readonly FileManagerState _state = fileManager.State;

    public void MoveSelection(int direction)
    {
        if (_state.CurrentItems.Count == 0) return;
        var newIndex = Math.Clamp(_state.SelectedIndex + direction, 0, _state.CurrentItems.Count - 1);
        if (newIndex == _state.SelectedIndex) return;
        _state.SelectedIndex = newIndex;
        fileManager.AdjustViewPort();
        fileManager.UpdatePreview();
    }

    public void MoveBookmarkSelection(int direction)
    {
        if (_state.FilteredBookmarks.Count == 0) return;
        var newIndex = Math.Clamp(_state.BookmarkMenuSelectedIndex + direction, 0, _state.FilteredBookmarks.Count - 1);
        if (newIndex == _state.BookmarkMenuSelectedIndex) return;
        _state.BookmarkMenuSelectedIndex = newIndex;
        fileManager.SetNeedsRedraw();
    }

    public void MoveSelectionToEdge(bool toStart)
    {
        _state.SelectedIndex = toStart ? 0 : Math.Max(0, _state.CurrentItems.Count - 1);
        fileManager.AdjustViewPort();
        fileManager.UpdatePreview();
    }

    public void OpenSelectedItem()
    {
        if (_state.CurrentItems.Count == 0) return;
        var selectedItem = _state.CurrentItems[_state.SelectedIndex];

        if (selectedItem.IsDirectory)
        {
            if (!string.IsNullOrEmpty(_state.InputText) &&
                _state.CurrentMode != InputMode.FilteredNavigation)
            {
                _state.SavedFilterState = (_state.CurrentPath, _state.InputText, [.. _state.CurrentItems],
                    _state.SelectedIndex);
                _state.CurrentMode = InputMode.FilteredNavigation;
                fileManager.SetNeedsRedraw();
            }

            _state.NavigationStack.Push(selectedItem.Name);
            NavigateToDirectory(selectedItem.Path);
        }
        else
        {
            var response = FileSystemService.OpenFile(selectedItem.Path);
            if (!response.Success)
            {
                _state.StatusMessage = response.Message;
                fileManager.SetNeedsRedraw();
            }
        }
    }

    public void NavigateUp()
    {
        if (_state.CurrentMode is not (InputMode.Normal or InputMode.FilteredNavigation))
            return;

        if (!string.IsNullOrEmpty(_state.InputText))
        {
            fileManager.FilterHandler.ClearFilter();
            return;
        }

        var parent = Directory.GetParent(_state.CurrentPath);
        if (parent != null)
            NavigateToDirectory(parent.FullName, _state.NavigationStack.TryPop(out var result) ? result : null);
    }

    private void NavigateToDirectory(string path, string? findAndSelect = null)
    {
        if (_state.CurrentMode != InputMode.FilteredNavigation) fileManager.ResetToNormalMode();
        try
        {
            _state.CurrentPath = Path.GetFullPath(path);
            fileManager.RefreshDirectory(findAndSelect, setInitialSelection: true);
        }
        catch (Exception ex)
        {
            _state.StatusMessage = $"[red]Navigation failed: {ex.Message.EscapeMarkup()}[/]";
            fileManager.SetNeedsRedraw();
        }
    }

    public void MoveSortMenuSelection(int direction)
    {
        if (_state.CurrentMode != InputMode.SortMenu) return;
        var newIndex = _state.SortMenuSelectedIndex + direction;
        _state.SortMenuSelectedIndex = Math.Clamp(newIndex, 0, fileManager.SortOptions.Count - 1);
        fileManager.SetNeedsRedraw();
    }

    public void MoveOpenWithMenuSelection(int direction)
    {
        if (_state.CurrentMode != InputMode.OpenWithMenu) return;
        var newIndex = _state.OpenWithMenuSelectedIndex + direction;
        _state.OpenWithMenuSelectedIndex = Math.Clamp(newIndex, 0, fileManager.OpenWithOptions.Count - 1);
        fileManager.SetNeedsRedraw();
    }

    public void ScrollPreview(int vertical, int horizontal)
    {
        _state.PreviewVerticalOffset = Math.Max(0, _state.PreviewVerticalOffset + vertical);
        _state.PreviewHorizontalOffset = Math.Max(0, _state.PreviewHorizontalOffset + horizontal);
        fileManager.UpdatePreview(false);
    }

    public void ScrollSelection(int direction)
    {
        if (_state.CurrentItems.Count == 0) return;
        var pageSize = Console.WindowHeight - 12;
        pageSize = Math.Max(5, pageSize);
        var scrollAmount = Math.Max(1, pageSize / 2);
        var actualScrollAmount = direction > 0 ? scrollAmount : -scrollAmount;
        var newIndex = _state.SelectedIndex + actualScrollAmount;
        newIndex = Math.Clamp(newIndex, 0, _state.CurrentItems.Count - 1);
        if (newIndex == _state.SelectedIndex) return;
        _state.SelectedIndex = newIndex;
        fileManager.AdjustViewPort();
        fileManager.UpdatePreview();
    }

    public void ScrollHelpScreen(int direction)
    {
        if (_state.CurrentMode != InputMode.HelpScreen) return;

        var totalItems = HelpProvider.Keybindings.Count;
        var pageSize = Console.WindowHeight - 12;
        pageSize = Math.Max(5, pageSize);

        var maxOffset = Math.Max(0, totalItems - pageSize);
        var newOffset = _state.HelpVerticalOffset + direction;

        _state.HelpVerticalOffset = Math.Clamp(newOffset, 0, maxOffset);
        fileManager.SetNeedsRedraw();
    }

    public void MoveTrashSelection(int direction)
    {
        if (_state.TrashEntries.Count == 0) return;
        var newIndex = Math.Clamp(_state.TrashMenuSelectedIndex + direction, 0, _state.TrashEntries.Count - 1);
        if (newIndex == _state.TrashMenuSelectedIndex) return;
        _state.TrashMenuSelectedIndex = newIndex;
        fileManager.SetNeedsRedraw();
    }
}