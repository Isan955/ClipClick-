using System;
using ClipClick.Services;

namespace ClipClick.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private int _selectedNavIndex = 0;

        public ClipboardViewModel ClipboardVM { get; private set; }
        public PlayerViewModel PlayerVM { get; private set; }
        public IslandViewModel IslandVM { get; private set; }
        public SettingsViewModel SettingsVM { get; private set; }

        public int SelectedNavIndex
        {
            get { return _selectedNavIndex; }
            set
            {
                if (SetProperty(ref _selectedNavIndex, value))
                {
                    OnPropertyChanged("IsClipboardTabSelected");
                    OnPropertyChanged("IsMediaTabSelected");
                    OnPropertyChanged("IsSettingsTabSelected");
                }
            }
        }

        public bool IsClipboardTabSelected
        {
            get { return _selectedNavIndex == 0; }
            set { if (value) SelectedNavIndex = 0; }
        }

        public bool IsMediaTabSelected
        {
            get { return _selectedNavIndex == 1; }
            set { if (value) SelectedNavIndex = 1; }
        }

        public bool IsSettingsTabSelected
        {
            get { return _selectedNavIndex == 2; }
            set { if (value) SelectedNavIndex = 2; }
        }

        public RelayCommand NavigateCommand { get; private set; }
        public RelayCommand ToggleIslandCommand { get; private set; }

        public MainViewModel()
            : this(null, null, null, null)
        {
        }

        public MainViewModel(
            ClipboardViewModel clipboardVM,
            PlayerViewModel playerVM,
            IslandViewModel islandVM,
            SettingsViewModel settingsVM)
        {
            ClipboardVM = clipboardVM;
            PlayerVM = playerVM;
            IslandVM = islandVM;
            SettingsVM = settingsVM;

            NavigateCommand = new RelayCommand(p =>
            {
                if (p != null)
                {
                    int index;
                    if (int.TryParse(p.ToString(), out index))
                    {
                        SelectedNavIndex = index;
                    }
                }
            });

            ToggleIslandCommand = new RelayCommand(() =>
            {
                if (SettingsVM != null)
                {
                    SettingsVM.ShowDynamicIsland = !SettingsVM.ShowDynamicIsland;
                }
            });

            if (SettingsVM != null)
            {
                SettingsVM.ClipboardCleared += () =>
                {
                    if (ClipboardVM != null) ClipboardVM.LoadItems();
                };

                SettingsVM.ClipboardLimitChanged += (limit) =>
                {
                    if (ClipboardVM != null) ClipboardVM.LoadItems();
                };
            }
        }
    }
}
