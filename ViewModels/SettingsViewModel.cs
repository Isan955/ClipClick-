using System;
using ClipClick.Services;

namespace ClipClick.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly SettingsService _settingsService;
        private readonly DatabaseService _dbService;

        public bool StartWithWindows
        {
            get { return _settingsService.Current.StartWithWindows; }
            set
            {
                if (_settingsService.Current.StartWithWindows != value)
                {
                    _settingsService.SetStartWithWindows(value);
                    OnPropertyChanged();
                }
            }
        }

        public bool MinimizeToTrayOnClose
        {
            get { return _settingsService.Current.MinimizeToTrayOnClose; }
            set
            {
                if (_settingsService.Current.MinimizeToTrayOnClose != value)
                {
                    _settingsService.Current.MinimizeToTrayOnClose = value;
                    _settingsService.Save();
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowDynamicIsland
        {
            get { return _settingsService.Current.ShowDynamicIsland; }
            set
            {
                if (_settingsService.Current.ShowDynamicIsland != value)
                {
                    _settingsService.Current.ShowDynamicIsland = value;
                    _settingsService.Save();
                    OnPropertyChanged();
                    var handler = DynamicIslandVisibilityToggled;
                    if (handler != null) handler(value);
                }
            }
        }

        public bool DynamicIslandTopmost
        {
            get { return _settingsService.Current.DynamicIslandTopmost; }
            set
            {
                if (_settingsService.Current.DynamicIslandTopmost != value)
                {
                    _settingsService.Current.DynamicIslandTopmost = value;
                    _settingsService.Save();
                    OnPropertyChanged();
                    var handler = DynamicIslandTopmostChanged;
                    if (handler != null) handler(value);
                }
            }
        }

        public bool DynamicIslandAutoExpand
        {
            get { return _settingsService.Current.DynamicIslandAutoExpand; }
            set
            {
                if (_settingsService.Current.DynamicIslandAutoExpand != value)
                {
                    _settingsService.Current.DynamicIslandAutoExpand = value;
                    _settingsService.Save();
                    OnPropertyChanged();
                }
            }
        }

        public int MaxClipboardEntries
        {
            get { return _settingsService.Current.MaxClipboardEntries > 0 ? _settingsService.Current.MaxClipboardEntries : 500; }
            set
            {
                if (_settingsService.Current.MaxClipboardEntries != value)
                {
                    _settingsService.Current.MaxClipboardEntries = value;
                    _settingsService.Save();
                    _dbService.PruneToLimit(value);
                    OnPropertyChanged();
                    OnPropertyChanged("IsLimit100");
                    OnPropertyChanged("IsLimit250");
                    OnPropertyChanged("IsLimit500");
                    OnPropertyChanged("DatabaseEngineInfo");

                    var handler = ClipboardLimitChanged;
                    if (handler != null) handler(value);
                }
            }
        }

        public bool IsLimit100
        {
            get { return MaxClipboardEntries == 100; }
            set { if (value) MaxClipboardEntries = 100; }
        }

        public bool IsLimit250
        {
            get { return MaxClipboardEntries == 250; }
            set { if (value) MaxClipboardEntries = 250; }
        }

        public bool IsLimit500
        {
            get { return MaxClipboardEntries == 500; }
            set { if (value) MaxClipboardEntries = 500; }
        }

        private string _clearStatusMessage;
        public string ClearStatusMessage
        {
            get { return _clearStatusMessage; }
            set
            {
                if (SetProperty(ref _clearStatusMessage, value))
                {
                    OnPropertyChanged("HasClearStatus");
                }
            }
        }

        public bool HasClearStatus
        {
            get { return !string.IsNullOrEmpty(_clearStatusMessage); }
        }

        public string AppVersion
        {
            get { return "ClipClick v1.0.0"; }
        }

        public string TargetPlatform
        {
            get { return "Windows 10 1809+ (.NET Framework 4.8)"; }
        }

        public string AudioEngineInfo
        {
            get { return "NAudio 2.2.1 (Low Latency WaveOut)"; }
        }

        public string DatabaseEngineInfo
        {
            get { return string.Format("SQLite 3 (Batas Riwayat: {0} Entri)", MaxClipboardEntries); }
        }

        public RelayCommand ClearAllClipboardCommand { get; private set; }

        public event Action<bool> DynamicIslandVisibilityToggled;
        public event Action<bool> DynamicIslandTopmostChanged;
        public event Action ClipboardCleared;
        public event Action<int> ClipboardLimitChanged;

        public SettingsViewModel(SettingsService settingsService, DatabaseService dbService)
        {
            _settingsService = settingsService;
            _dbService = dbService;

            ClearAllClipboardCommand = new RelayCommand(ClearAllClipboard);
        }

        private void ClearAllClipboard()
        {
            _dbService.ClearAllUnpinned();
            ClearStatusMessage = "Riwayat clipboard (unpinned) berhasil dibersihkan!";
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            timer.Tick += (s, e) =>
            {
                ClearStatusMessage = null;
                timer.Stop();
            };
            timer.Start();

            var handler = ClipboardCleared;
            if (handler != null) handler();
        }
    }
}
