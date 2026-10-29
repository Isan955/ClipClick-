using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ClipClick.Services;
using ClipClick.ViewModels;

namespace ClipClick.Views
{
    public partial class DynamicIslandWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly IslandViewModel _islandVM;

        public DynamicIslandWindow(IslandViewModel islandVM, SettingsService settingsService)
        {
            InitializeComponent();
            _islandVM = islandVM;
            _settingsService = settingsService;
            DataContext = _islandVM;

            _islandVM.PropertyChanged += OnIslandVMPropertyChanged;

            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Restore window position or default to top center
            if (_settingsService.Current.IslandLeft >= 0 && _settingsService.Current.IslandTop >= 0)
            {
                Left = _settingsService.Current.IslandLeft;
                Top = _settingsService.Current.IslandTop;
            }
            else
            {
                // Default: top-center of primary screen with 24px padding from top
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                Left = (screenWidth - Width) / 2;
                Top = 24;
            }

            Topmost = _settingsService.Current.DynamicIslandTopmost;
            UpdateExpandState(_islandVM.IsExpanded);
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            // Save position
            _settingsService.Current.IslandLeft = Left;
            _settingsService.Current.IslandTop = Top;
            _settingsService.Save();
        }

        private void OnIslandVMPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsExpanded")
            {
                UpdateExpandState(_islandVM.IsExpanded);
            }
        }

        private void UpdateExpandState(bool expanded)
        {
            if (expanded)
            {
                CompactGrid.Visibility = Visibility.Collapsed;
                ExpandedGrid.Visibility = Visibility.Visible;
                Height = 115;
                Width = 400;
                PillBorder.CornerRadius = new CornerRadius(16);
            }
            else
            {
                CompactGrid.Visibility = Visibility.Visible;
                ExpandedGrid.Visibility = Visibility.Collapsed;
                Height = 48;
                Width = 380;
                PillBorder.CornerRadius = new CornerRadius(20);
            }
        }

        private void Pill_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
                _settingsService.Current.IslandLeft = Left;
                _settingsService.Current.IslandTop = Top;
                _settingsService.Save();
            }
        }

        private void Pill_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_settingsService.Current.DynamicIslandAutoExpand && !_islandVM.IsExpanded)
            {
                _islandVM.IsExpanded = true;
            }
        }

        private void Pill_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_settingsService.Current.DynamicIslandAutoExpand && _islandVM.IsExpanded)
            {
                _islandVM.IsExpanded = false;
            }
        }
    }
}
