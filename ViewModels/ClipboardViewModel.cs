using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using ClipClick.Models;
using ClipClick.Services;

namespace ClipClick.ViewModels
{
    public class ClipboardViewModel : ViewModelBase
    {
        private readonly DatabaseService _dbService;
        private readonly ClipboardMonitorService _monitorService;
        private readonly Dispatcher _dispatcher;

        private ObservableCollection<ClipboardItem> _items;
        private string _searchText = string.Empty;
        private string _statusText = string.Empty;
        private int _totalCount;
        private int _pinnedCount;

        public ObservableCollection<ClipboardItem> Items
        {
            get { return _items; }
            set
            {
                if (SetProperty(ref _items, value))
                {
                    OnPropertyChanged("IsEmpty");
                    OnPropertyChanged("HasItems");
                }
            }
        }

        public bool IsEmpty
        {
            get { return _items == null || _items.Count == 0; }
        }

        public bool HasItems
        {
            get { return !IsEmpty; }
        }

        public bool HasSearchText
        {
            get { return !string.IsNullOrEmpty(_searchText); }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    OnPropertyChanged("HasSearchText");
                    LoadItems();
                }
            }
        }

        public string StatusText
        {
            get { return _statusText; }
            set { SetProperty(ref _statusText, value); }
        }

        public int TotalCount
        {
            get { return _totalCount; }
            set
            {
                if (SetProperty(ref _totalCount, value))
                {
                    UpdateStatusText();
                }
            }
        }

        public int PinnedCount
        {
            get { return _pinnedCount; }
            set
            {
                if (SetProperty(ref _pinnedCount, value))
                {
                    UpdateStatusText();
                }
            }
        }

        public RelayCommand CopyItemCommand { get; private set; }
        public RelayCommand TogglePinCommand { get; private set; }
        public RelayCommand DeleteItemCommand { get; private set; }
        public RelayCommand ClearAllCommand { get; private set; }
        public RelayCommand ClearSearchCommand { get; private set; }

        public ClipboardViewModel(DatabaseService dbService)
            : this(dbService, null, null)
        {
        }

        public ClipboardViewModel(DatabaseService dbService, ClipboardMonitorService monitorService, Dispatcher dispatcher)
        {
            _dbService = dbService;
            _monitorService = monitorService;
            _dispatcher = dispatcher;

            Items = new ObservableCollection<ClipboardItem>();

            CopyItemCommand = new RelayCommand(p => CopyItem(p as ClipboardItem));
            TogglePinCommand = new RelayCommand(p => TogglePin(p as ClipboardItem));
            DeleteItemCommand = new RelayCommand(p => DeleteItem(p as ClipboardItem));
            ClearAllCommand = new RelayCommand(ClearAll);
            ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);

            if (_monitorService != null)
            {
                _monitorService.ClipboardTextChanged += OnNewClipboardText;
            }

            LoadItems();
        }

        public void LoadItems()
        {
            var list = _dbService.GetClipboardEntries(_searchText);
            Items = new ObservableCollection<ClipboardItem>(list);

            int total, pinned;
            _dbService.GetStats(out total, out pinned);
            TotalCount = total;
            PinnedCount = pinned;
            OnPropertyChanged("IsEmpty");
            OnPropertyChanged("HasItems");
        }

        private void OnNewClipboardText(string text)
        {
            _dispatcher.BeginInvoke(new Action(() =>
            {
                var newItem = _dbService.AddClipboardEntry(text);
                if (newItem != null)
                {
                    // If no search filter or it matches the search, prepend it
                    if (string.IsNullOrEmpty(_searchText) || text.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Insert after any pinned items
                        int insertIndex = 0;
                        while (insertIndex < Items.Count && Items[insertIndex].IsPinned)
                        {
                            insertIndex++;
                        }
                        Items.Insert(insertIndex, newItem);

                        // Prune UI list if over limit
                        int limit = _dbService.MaxEntries;
                        while (Items.Count > limit)
                        {
                            Items.RemoveAt(Items.Count - 1);
                        }
                    }
                }
                else
                {
                    // Item was touched/bumped, reload to reflect timestamp
                    LoadItems();
                }

                int total, pinned;
                _dbService.GetStats(out total, out pinned);
                TotalCount = total;
                PinnedCount = pinned;
                OnPropertyChanged("IsEmpty");
                OnPropertyChanged("HasItems");
            }));
        }

        private void CopyItem(ClipboardItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.Content)) return;

            _monitorService.SetClipboardTextSilently(item.Content);

            // Visual feedback
            item.IsJustCopied = true;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            timer.Tick += (s, e) =>
            {
                item.IsJustCopied = false;
                timer.Stop();
            };
            timer.Start();
        }

        private void TogglePin(ClipboardItem item)
        {
            if (item == null) return;
            item.IsPinned = !item.IsPinned;
            _dbService.TogglePin(item.Id, item.IsPinned);

            // Refresh to update ordering (pinned first)
            LoadItems();
        }

        private void DeleteItem(ClipboardItem item)
        {
            if (item == null) return;
            _dbService.DeleteEntry(item.Id);
            Items.Remove(item);

            int total, pinned;
            _dbService.GetStats(out total, out pinned);
            TotalCount = total;
            PinnedCount = pinned;
        }

        private void ClearAll()
        {
            _dbService.ClearAllUnpinned();
            LoadItems();
        }

        private void UpdateStatusText()
        {
            StatusText = string.Format("{0} / {1} items ({2} pinned)", _totalCount, _dbService.MaxEntries, _pinnedCount);
        }
    }
}
