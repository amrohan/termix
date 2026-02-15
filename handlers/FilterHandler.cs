using termix.Helpers;
using termix.models;
using termix.Services;

namespace termix.Handlers;

public class FilterHandler(FileManager fileManager)
{
    private readonly FileManagerState _state = fileManager.State;

    public void BeginFilter()
    {
        _state.CurrentMode = InputMode.Filter;
        _state.PromptText = "Search: ";
        _state.InputText = "";
        _state.RecursiveSearchCache = null;
        fileManager.SetNeedsRedraw();
    }

    public void UpdateFilter(string newFilterText)
    {
        _state.InputText = newFilterText;
        fileManager.SetNeedsRedraw();
        _state.DebounceCts.Cancel();
        _state.DebounceCts = new CancellationTokenSource();
        var token = _state.DebounceCts.Token;

        if (_state.RecursiveSearchCache == null && !_state.IsDeepSearchRunning &&
            !string.IsNullOrEmpty(_state.InputText))
        {
            _state.IsDeepSearchRunning = true;
            fileManager.SetNeedsRedraw();
            ActionService.GetDeepDirectoryContentsAsync(_state.CurrentPath, token).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    _state.RecursiveSearchCache = task.Result;
                    if (!token.IsCancellationRequested && _state.InputText.Length > 0) ApplyFilter();
                }

                _state.IsDeepSearchRunning = false;
                fileManager.SetNeedsRedraw();
            }, token);
        }

        ApplyFilter();
    }

    public void ClearFilter()
    {
        _state.InputText = "";
        _state.RecursiveSearchCache = null;
        _state.CurrentItems = new List<FileSystemItem>(_state.UnfilteredItems);
        fileManager.ResetToNormalMode();
        RefreshViewAfterFilter();
    }

    public void AcceptFilter()
    {
        _state.CurrentMode = InputMode.Normal;
        _state.PromptText = "";
        fileManager.SetNeedsRedraw();
    }

    public void ReturnToFilter()
    {
        if (!_state.SavedFilterState.HasValue) return;

        var state = _state.SavedFilterState.Value;
        _state.CurrentPath = state.Path;
        _state.InputText = state.Filter;
        _state.CurrentItems = state.Items;
        _state.SelectedIndex = state.SelectedIndex;
        _state.SavedFilterState = null;
        _state.CurrentMode = InputMode.Filter;

        fileManager.AdjustViewPort();
        fileManager.UpdatePreview();
    }

    private void ApplyFilter()
    {
        var sourceList = _state.RecursiveSearchCache ?? _state.UnfilteredItems;

        if (string.IsNullOrEmpty(_state.InputText))
        {
            _state.CurrentItems = [.. _state.UnfilteredItems];
        }
        else
        {
            _state.CurrentItems = sourceList
                .Where(item => FuzzyMatcher.IsMatch(item.Name, _state.InputText))
                .ToList();
        }

        RefreshViewAfterFilter();
    }

    private void RefreshViewAfterFilter()
    {
        _state.SelectedIndex = _state.CurrentItems.Count != 0 ? 0 : -1;
        fileManager.AdjustViewPort();
        fileManager.UpdatePreview();
    }
}