using Spectre.Console;
using termix.models;
using termix.Services;

namespace termix.Handlers
{
    public class ActionHandler(FileManager fileManager, BookmarkService bookmarkService)
    {
        private readonly FileManagerState _state = fileManager.State;

        public void ShowHelpScreen()
        {
            _state.CurrentMode = InputMode.HelpScreen;
            fileManager.SetNeedsRedraw();
        }
        public void BeginPaste()
        {
            if (_state.Clipboard == null)
            {
                _state.StatusMessage = "[red]Clipboard is empty.[/]";
                fileManager.SetNeedsRedraw();
                return;
            }

            var destinationBasePath = _state.CurrentPath;
            var selectedItem = _state.CurrentItems.Count > 0 && _state.SelectedIndex >= 0
                ? _state.CurrentItems[_state.SelectedIndex]
                : null;
            if (selectedItem is { IsDirectory: true, IsParentDirectory: false })
            {
                destinationBasePath = selectedItem.Path;
            }

            if (!Directory.Exists(destinationBasePath))
            {
                _state.CurrentMode = InputMode.CreateDirConfirm;
                _state.PendingCreateDirectoryPath = destinationBasePath;
                var dirName = Path.GetFileName(destinationBasePath.TrimEnd(Path.DirectorySeparatorChar));
                _state.PromptText =
                    $"Directory '[yellow]{dirName.EscapeMarkup()}[/]' does not exist. Create it? [bold green]y[/]/[bold red]n[/]";
                fileManager.SetNeedsRedraw();
                return;
            }

            var itemsToPaste = new Queue<FileSystemItem>(_state.Clipboard.Items);
            var totalItems = itemsToPaste.Count;

            _state.ActiveConflictResolution = ConflictResolution.None;
            var originalMode = _state.Clipboard.Mode;

            ProcessPasteQueue(itemsToPaste, destinationBasePath, originalMode, totalItems, 0);
        }

        public void ConfirmCreateDirectoryAndPaste()
        {
            var pathToCreate = _state.PendingCreateDirectoryPath;
            if (string.IsNullOrEmpty(pathToCreate))
            {
                fileManager.ResetToNormalMode();
                return;
            }

            try
            {
                var newDirName = Path.GetFileName(pathToCreate.TrimEnd(Path.DirectorySeparatorChar));
                Directory.CreateDirectory(pathToCreate);

                _state.StatusMessage =
                    $"[green]Directory '{newDirName.EscapeMarkup()}' created. Preparing to paste...[/]";
                _state.PendingCreateDirectoryPath = null;
                fileManager.ResetToNormalMode();

                fileManager.RefreshDirectory(findAndSelect: newDirName);

                fileManager.ScheduleUiAction(BeginPaste);
            }
            catch (Exception ex)
            {
                _state.StatusMessage = $"[red]Error creating directory: {ex.Message.EscapeMarkup()}[/]";
                _state.PendingCreateDirectoryPath = null;
                fileManager.ResetToNormalMode();
            }
        }

        public void CancelCreateDirectoryAndPaste()
        {
            _state.PendingCreateDirectoryPath = null;
            _state.StatusMessage = "[yellow]Paste operation cancelled.[/]";
            fileManager.ResetToNormalMode();
        }

        private void ProcessPasteQueue(Queue<FileSystemItem> items, string destBasePath, ClipboardMode mode, int total,
            int currentIndex)
        {
            if (items.Count == 0)
            {
                _state.StatusMessage = $"[green]Successfully processed {total} item(s).[/]";
                _state.Clipboard = null;
                fileManager.RefreshDirectory();
                return;
            }

            var currentItem = items.Peek();
            var destPath = Path.Combine(destBasePath, currentItem.Name);

            if (File.Exists(destPath) || Directory.Exists(destPath))
            {
                if (_state.ActiveConflictResolution == ConflictResolution.SkipAll)
                {
                    items.Dequeue();
                    ProcessPasteQueue(items, destBasePath, mode, total, currentIndex + 1);
                    return;
                }

                if (_state.ActiveConflictResolution == ConflictResolution.ReplaceAll)
                {
                    var itemToDelete = new FileSystemItem(destPath, Path.GetFileName(destPath),
                        Directory.Exists(destPath), 0, DateTime.Now);
                    ActionService.Delete(itemToDelete);
                    items.Dequeue();
                    PasteItem(currentItem, destPath, items, destBasePath, mode, total, currentIndex);
                    return;
                }

                _state.CurrentMode = InputMode.PasteConflict;
                _state.CurrentConflict =
                    new PasteConflictState(currentItem, destPath, items, mode, total, currentIndex);

                _state.PromptText =
                    $"Destination '{currentItem.Name.EscapeMarkup()}' already exists. | [grey]Skip[/] [cyan]S[/] | [grey]Skip All[/] [cyan]L[/] | [grey]Replace[/] [cyan]R[/] | [grey]Replace All[/] [cyan]A[/] | [grey]Cancel[/] [cyan]Esc[/]";

                fileManager.SetNeedsRedraw();
                return;
            }

            items.Dequeue();
            PasteItem(currentItem, destPath, items, destBasePath, mode, total, currentIndex);
        }

        public void ResolveConflict(ConflictResolution resolution, bool replace)
        {
            if (_state.CurrentConflict == null) return;

            _state.ActiveConflictResolution = resolution;
            var conflict = _state.CurrentConflict;
            _state.CurrentConflict = null;
            fileManager.ResetToNormalMode();

            var destBasePath = Path.GetDirectoryName(conflict.DestinationPath)!;

            if (replace)
            {
                var itemToDelete = new FileSystemItem(conflict.DestinationPath,
                    Path.GetFileName(conflict.DestinationPath), Directory.Exists(conflict.DestinationPath), 0,
                    DateTime.Now);
                ActionService.Delete(itemToDelete);

                conflict.RemainingItems.Dequeue();
                PasteItem(conflict.SourceItem, conflict.DestinationPath, conflict.RemainingItems, destBasePath,
                    conflict.OriginalMode, conflict.TotalItems, conflict.CurrentItemIndex);
            }
            else
            {
                conflict.RemainingItems.Dequeue();
                ProcessPasteQueue(conflict.RemainingItems, destBasePath, conflict.OriginalMode, conflict.TotalItems,
                    conflict.CurrentItemIndex + 1);
            }
        }

        public void CancelPasteOperation()
        {
            _state.CurrentConflict = null;
            _state.StatusMessage = "[yellow]Paste operation cancelled.[/]";
            fileManager.ResetToNormalMode();
            fileManager.RefreshDirectory();
        }

        private void PasteItem(FileSystemItem item, string destPath, Queue<FileSystemItem> remainingItems,
            string destBasePath, ClipboardMode mode, int totalItems, int currentIndex)
        {
            _state.IsOperationInProgress = true;
            _state.OperationCts = new CancellationTokenSource();
            var token = _state.OperationCts.Token;

            var description =
                $"({currentIndex + 1}/{totalItems}) {(mode == ClipboardMode.Copy ? "Copying" : "Moving")} {item.Name.EscapeMarkup()}";

            _state.ProgressTaskDescription = description;
            _state.ProgressValue = (double)currentIndex / totalItems * 100;
            fileManager.SetNeedsRedraw();

            Task.Run(async () =>
            {
                var progress = new Progress<(long, long, string)>(_ => { });

                return mode == ClipboardMode.Copy
                    ? await ActionService.CopyAsync(item.Path, destPath, progress, token)
                    : await ActionService.MoveAsync(item.Path, destPath, progress, token);
            }, token).ContinueWith(t =>
            {
                fileManager.ScheduleUiAction(() =>
                {
                    _state.IsOperationInProgress = false;
                    if (t is { IsCompletedSuccessfully: true, Result.Success: true })
                    {
                        ProcessPasteQueue(remainingItems, destBasePath, mode, totalItems, currentIndex + 1);
                    }
                    else
                    {
                        _state.StatusMessage = t.Result.Message;
                        fileManager.RefreshDirectory();
                    }
                });
            }, token);
        }

        public void CommitDelete()
        {
            var itemsToDelete = _state.PendingDeleteItems.ToList();
            _state.PendingDeleteItems.Clear();
            fileManager.ResetToNormalMode();

            _state.IsOperationInProgress = true;
            _state.OperationCts = new CancellationTokenSource();
            var token = _state.OperationCts.Token;

            Task.Run(() =>
            {
                ActionResponse response = new(true, "");
                for (var i = 0; i < itemsToDelete.Count; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        response = new ActionResponse(false, "[yellow]Delete operation cancelled.[/]");
                        break;
                    }

                    var item = itemsToDelete[i];
                    var description = $"Deleting ({i + 1}/{itemsToDelete.Count}): {item.Name.EscapeMarkup()}";
                    var progressValue = ((double)(i + 1) / itemsToDelete.Count) * 100;

                    fileManager.ScheduleUiAction(() =>
                    {
                        _state.ProgressTaskDescription = description;
                        _state.ProgressValue = progressValue;
                        fileManager.SetNeedsRedraw();
                    });

                    response = ActionService.Delete(item);
                    if (!response.Success) break;
                }

                var finalResponse = response.Success
                    ? new ActionResponse(true, response.Message)
                    : response;

                fileManager.ScheduleUiAction(() =>
                {
                    _state.IsOperationInProgress = false;
                    _state.StatusMessage = finalResponse.Message;
                    fileManager.RefreshDirectory(preserveSelection: true);
                });
            }, token);
        }

        public void BeginAdd()
        {
            _state.CurrentMode = InputMode.Add;
            _state.InputText = "";
            var selectedItem = _state.SelectedIndex >= 0 && _state.SelectedIndex < _state.CurrentItems.Count
                ? _state.CurrentItems[_state.SelectedIndex]
                : null;

            if (selectedItem is { IsDirectory: true, IsParentDirectory: false })
            {
                _state.AddBasePath = selectedItem.Path;
                _state.PromptText = $"Create in [{selectedItem.Name.EscapeMarkup()}]: ";
            }
            else
            {
                _state.AddBasePath = _state.CurrentPath;
                var currentFolderName = Path.GetFileName(Path.GetFullPath(_state.CurrentPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (string.IsNullOrEmpty(currentFolderName)) currentFolderName = _state.CurrentPath;
                _state.PromptText = $"Create in [{currentFolderName.EscapeMarkup()}]: ";
            }

            fileManager.SetNeedsRedraw();
        }

        public void BeginRename()
        {
            if (_state.CurrentItems.Count == 0 || _state.CurrentItems[_state.SelectedIndex].IsParentDirectory) return;
            _state.CurrentMode = InputMode.Rename;
            _state.PromptText = "Rename: ";
            _state.InputText = _state.CurrentItems[_state.SelectedIndex].Name;
            fileManager.SetNeedsRedraw();
        }

        public void BeginDelete()
        {
            var itemsToDelete = GetSelectedItems();
            if (itemsToDelete.Count == 0) return;

            _state.PendingDeleteItems = itemsToDelete;
            _state.CurrentMode = InputMode.DeleteConfirm;
            var itemName = itemsToDelete.Count == 1
                ? $"'{itemsToDelete[0].Name.EscapeMarkup()}'"
                : $"{itemsToDelete.Count} items";
            _state.PromptText = $"Delete {itemName}? [bold green]y[/]/[bold red]n[/]";
            fileManager.SetNeedsRedraw();
        }

        public void BeginCopy()
        {
            var itemsToCopy = GetSelectedItems();
            if (itemsToCopy.Count == 0) return;

            _state.Clipboard = new ClipboardItem(itemsToCopy, ClipboardMode.Copy);
            var message = itemsToCopy.Count == 1
                ? $"[yellow]{itemsToCopy[0].Name.EscapeMarkup()}[/]"
                : $"[yellow]{itemsToCopy.Count} items[/]";
            _state.StatusMessage = $"{message} copied to clipboard.";

            if (_state.CurrentMode == InputMode.Visual) fileManager.ResetToNormalMode();
            fileManager.SetNeedsRedraw();
        }

        public void BeginMove()
        {
            var itemsToMove = GetSelectedItems();
            if (itemsToMove.Count == 0) return;

            _state.Clipboard = new ClipboardItem(itemsToMove, ClipboardMode.Move);
            var message = itemsToMove.Count == 1
                ? $"[yellow]{itemsToMove[0].Name.EscapeMarkup()}[/]"
                : $"[yellow]{itemsToMove.Count} items[/]";
            _state.StatusMessage = $"{message} marked for move.";

            if (_state.CurrentMode == InputMode.Visual) fileManager.ResetToNormalMode();
            fileManager.SetNeedsRedraw();
        }

        public void ClearClipboard()
        {
            _state.Clipboard = null;
            _state.StatusMessage = "[grey]Clipboard cleared.[/]";
            fileManager.SetNeedsRedraw();
        }

        public void CommitStandardTextInput()
        {
            switch (_state.CurrentMode)
            {
                case InputMode.Add or InputMode.Rename:
                    CommitFileAction();
                    break;
                case InputMode.AddBookmark:
                    AddBookmark(_state.InputText);
                    break;
                case InputMode.RenameBookmark:
                    RenameBookmark(_state.InputText);
                    break;
            }
        }

        private void CommitFileAction()
        {
            var item = _state.CurrentItems.Count > _state.SelectedIndex && _state.SelectedIndex >= 0
                ? _state.CurrentItems[_state.SelectedIndex]
                : null;
            var response = _state.CurrentMode == InputMode.Add
                ? ActionService.Create(_state.AddBasePath, _state.InputText)
                : ActionService.Rename(_state.CurrentPath, item?.Name ?? "", _state.InputText);

            _state.StatusMessage = response.Message;
            fileManager.ResetToNormalMode();
            if (response.Success) fileManager.RefreshDirectory((string?)response.Payload);
        }

        public void RequestQuit()
        {
            if (_state.IsOperationInProgress)
            {
                _state.CurrentMode = InputMode.QuitConfirm;
                _state.PromptText = "[bold yellow]A file operation is in progress. Quit and cancel? (y/n)[/]";
                fileManager.SetNeedsRedraw();
            }
            else
            {
                fileManager.Quit();
            }
        }

        public void BeginSortMenu()
        {
            _state.CurrentMode = InputMode.SortMenu;
            _state.SortMenuSelectedIndex = 0;
            fileManager.SetNeedsRedraw();
        }
        
        public void BeginOpenWithMenu()
        {
            _state.CurrentMode = InputMode.OpenWithMenu;
            _state.OpenWithMenuSelectedIndex = 0;
            fileManager.SetNeedsRedraw();
        }

        public void ExecuteSelectedOpenWith()
        {
            if (_state.CurrentMode != InputMode.OpenWithMenu) return;

            var selectedOption = fileManager.OpenWithOptions[_state.OpenWithMenuSelectedIndex];
            
            string pathToOpen = _state.CurrentPath;
            var selectedItem = _state.SelectedIndex >= 0 && _state.SelectedIndex < _state.CurrentItems.Count 
                ? _state.CurrentItems[_state.SelectedIndex] : null;

            if (selectedItem is { IsDirectory: true })
            {
                pathToOpen = selectedItem.Path;
            }

            var response = ExternalProgramService.Open(selectedOption.Command, pathToOpen);
            _state.StatusMessage = response.Message;

            fileManager.ResetToNormalMode();
        }

        public void ApplySort()
        {
            if (_state.CurrentMode != InputMode.SortMenu) return;

            var selected = fileManager.SortOptions[_state.SortMenuSelectedIndex];
            _state.SortBy = selected.By;
            _state.SortDirection = selected.Dir;
            _state.GroupDirectories = selected.Group;

            fileManager.ResetToNormalMode();
            fileManager.RefreshDirectory(setInitialSelection: true);
        }

        public void YankDirectoryPath()
        {
            if (_state.SelectedIndex < 0 || _state.SelectedIndex >= _state.CurrentItems.Count) return;

            var selectedItem = _state.CurrentItems[_state.SelectedIndex];
            var pathToYank = selectedItem.Path;

            var response = ClipboardService.SetText(pathToYank);
            _state.StatusMessage = response.Message;
            fileManager.SetNeedsRedraw();
        }

        private List<FileSystemItem> GetSelectedItems()
        {
            var selectedItems = new List<FileSystemItem>();
            if (_state.CurrentMode == InputMode.Visual && _state.VisuallySelectedItems.Any())
            {
                var selectedPaths = _state.VisuallySelectedItems.ToHashSet();
                selectedItems.AddRange(_state.CurrentItems.Where(i => selectedPaths.Contains(i.Path)));
            }
            else if (_state.SelectedIndex >= 0 && _state.SelectedIndex < _state.CurrentItems.Count)
            {
                var item = _state.CurrentItems[_state.SelectedIndex];
                if (!item.IsParentDirectory)
                {
                    selectedItems.Add(item);
                }
            }

            return selectedItems;
        }


        #region Bookmark 
        public void OpenBookmarkMenu()
        {
            _state.Bookmarks = bookmarkService.LoadBookmarks().OrderBy(b => b.Name).ToList();
            _state.FilteredBookmarks = new List<Bookmark>(_state.Bookmarks);
            _state.CurrentMode = InputMode.BookmarkMenu;
            _state.BookmarkMenuSelectedIndex = 0;
            _state.InputText = "";
            fileManager.SetNeedsRedraw();
        }

        public void CloseBookmarkMenu()
        {
            fileManager.ResetToNormalMode();
        }

        public void BeginFilterBookmarks()
        {
            _state.CurrentMode = InputMode.BookmarkFilter;
            _state.PromptText = "Filter Bookmarks: ";
            _state.InputText = "";
            FilterBookmarks("");
            fileManager.SetNeedsRedraw();
        }
        public void NavigateToSelectedBookmark()
        {
            if (_state.BookmarkMenuSelectedIndex < 0 || _state.BookmarkMenuSelectedIndex >= _state.FilteredBookmarks.Count) return;

            var bookmark = _state.FilteredBookmarks[_state.BookmarkMenuSelectedIndex];
            fileManager.ResetToNormalMode();
            _state.CurrentPath = bookmark.Path;
            fileManager.RefreshDirectory(setInitialSelection: true);
        }
        public void BeginAddBookmark()
        {
            _state.CurrentMode = InputMode.AddBookmark;
            _state.PromptText = "Bookmark name: ";
            _state.InputText = "";
            fileManager.SetNeedsRedraw();
        }
        private void AddBookmark(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _state.StatusMessage = "[red]Bookmark name cannot be empty.[/]";
                _state.CurrentMode = InputMode.Normal;
                fileManager.SetNeedsRedraw();
                return;
            }

            var bookmarks = bookmarkService.LoadBookmarks();
            if (bookmarks.Any(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                _state.StatusMessage = $"[red]Bookmark '{name.EscapeMarkup()}' already exists.[/]";
                _state.CurrentMode = InputMode.Normal;
                fileManager.SetNeedsRedraw();
                return;
            }

            string pathToBookmark = _state.CurrentPath;

            var selectedItem = _state.SelectedIndex >= 0 && _state.SelectedIndex < _state.CurrentItems.Count
                ? _state.CurrentItems[_state.SelectedIndex]
                : null;

            if (selectedItem is { IsDirectory: true })
            {
                pathToBookmark = selectedItem.Path;
            }
            bookmarks.Add(new Bookmark(name, pathToBookmark));
            bookmarkService.SaveBookmarks(bookmarks);

            var bookmarkedItemName = Path.GetFileName(pathToBookmark.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(bookmarkedItemName)) bookmarkedItemName = pathToBookmark;

            _state.StatusMessage = $"[green]Bookmark '{name.EscapeMarkup()}' added for '[yellow]{bookmarkedItemName.EscapeMarkup()}[/]'[/]";
            fileManager.ResetToNormalMode();
        }
        public void BeginRenameBookmark()
        {
            if (_state.BookmarkMenuSelectedIndex < 0 || _state.BookmarkMenuSelectedIndex >= _state.FilteredBookmarks.Count) return;
            var bookmark = _state.FilteredBookmarks[_state.BookmarkMenuSelectedIndex];

            _state.CurrentMode = InputMode.RenameBookmark;
            _state.PromptText = "New name: ";
            _state.InputText = bookmark.Name;
            fileManager.SetNeedsRedraw();
        }

        private void RenameBookmark(string newName)
        {
            if (_state.BookmarkMenuSelectedIndex < 0 || _state.BookmarkMenuSelectedIndex >= _state.FilteredBookmarks.Count) return;
            if (string.IsNullOrWhiteSpace(newName))
            {
                _state.StatusMessage = "[red]Bookmark name cannot be empty.[/]";
                return;
            }

            var originalBookmark = _state.FilteredBookmarks[_state.BookmarkMenuSelectedIndex];
            var bookmarks = bookmarkService.LoadBookmarks();
            var bookmarkToUpdate = bookmarks.FirstOrDefault(b => b.Name.Equals(originalBookmark.Name, StringComparison.OrdinalIgnoreCase));

            if (bookmarkToUpdate != null)
            {
                bookmarks.Remove(bookmarkToUpdate);
                bookmarks.Add(bookmarkToUpdate with { Name = newName });
                bookmarkService.SaveBookmarks(bookmarks);
                _state.StatusMessage = $"[green]Bookmark renamed to '{newName.EscapeMarkup()}'[/]";
            }

            OpenBookmarkMenu();
        }

        public void BeginDeleteBookmark()
        {
            List<Bookmark> bookmarksToDelete = [];
            if (_state.CurrentMode == InputMode.BookmarkVisual && _state.VisuallySelectedBookmarks.Any())
            {
                var selectedNames = _state.VisuallySelectedBookmarks.ToHashSet();
                bookmarksToDelete.AddRange(_state.FilteredBookmarks.Where(b => selectedNames.Contains(b.Name)));
            }
            else if (_state.BookmarkMenuSelectedIndex >= 0 && _state.BookmarkMenuSelectedIndex < _state.FilteredBookmarks.Count)
            {
                bookmarksToDelete.Add(_state.FilteredBookmarks[_state.BookmarkMenuSelectedIndex]);
            }

            if (bookmarksToDelete.Count == 0) return;

            _state.PendingDeleteBookmarks = bookmarksToDelete;
            _state.CurrentMode = InputMode.BookmarkDeleteConfirm;
            var prompt = bookmarksToDelete.Count == 1
                ? $"Delete bookmark '{bookmarksToDelete[0].Name.EscapeMarkup()}'?"
                : $"Delete {bookmarksToDelete.Count} bookmarks?";

            _state.PromptText = $"{prompt} [bold green]y[/]/[bold red]n[/]";
            fileManager.SetNeedsRedraw();
        }

        public void CommitDeleteBookmark()
        {
            var bookmarks = bookmarkService.LoadBookmarks();
            var namesToDelete = _state.PendingDeleteBookmarks.Select(b => b.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            bookmarks.RemoveAll(b => namesToDelete.Contains(b.Name));
            bookmarkService.SaveBookmarks(bookmarks);

            _state.StatusMessage = $"[green]Deleted {_state.PendingDeleteBookmarks.Count} bookmark(s).[/]";
            _state.PendingDeleteBookmarks.Clear();

            OpenBookmarkMenu();
        }

        public void FilterBookmarks(string filter)
        {
            _state.InputText = filter;
            if (string.IsNullOrEmpty(filter))
            {
                _state.FilteredBookmarks = new List<Bookmark>(_state.Bookmarks);
            }
            else
            {
                _state.FilteredBookmarks = _state.Bookmarks
                    .Where(b => b.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                                b.Path.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            _state.BookmarkMenuSelectedIndex = _state.FilteredBookmarks.Count > 0 ? 0 : -1;
            fileManager.SetNeedsRedraw();
        }

        public void ToggleBookmarkVisualSelection()
        {
            if (_state.BookmarkMenuSelectedIndex < 0 || _state.BookmarkMenuSelectedIndex >= _state.FilteredBookmarks.Count) return;

            var item = _state.FilteredBookmarks[_state.BookmarkMenuSelectedIndex];

            if (!_state.VisuallySelectedBookmarks.Add(item.Name))
            {
                _state.VisuallySelectedBookmarks.Remove(item.Name);
            }

            fileManager.SetNeedsRedraw();
        }

        #endregion
    }
}