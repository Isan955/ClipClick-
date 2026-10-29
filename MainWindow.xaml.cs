using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using ClipClick.Services;
using ClipClick.ViewModels;
using ClipClick.Views;

namespace ClipClick
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _mainVM;
        private readonly ClipboardMonitorService _clipboardMonitor;
        private readonly SettingsService _settingsService;
        private readonly DynamicIslandWindow _islandWindow;
        private NotifyIcon _notifyIcon;
        private bool _isRealExit = false;

        public MainWindow(
            MainViewModel mainVM,
            ClipboardMonitorService clipboardMonitor,
            SettingsService settingsService,
            DynamicIslandWindow islandWindow)
        {
            InitializeComponent();
            _mainVM = mainVM;
            _clipboardMonitor = clipboardMonitor;
            _settingsService = settingsService;
            _islandWindow = islandWindow;

            DataContext = _mainVM;

            Loaded += OnLoaded;
            Closing += OnClosing;

            _mainVM.SettingsVM.DynamicIslandVisibilityToggled += OnIslandVisibilityToggled;
            _mainVM.SettingsVM.DynamicIslandTopmostChanged += OnIslandTopmostChanged;

            InitializeSystemTray();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Start Win32 clipboard monitoring
            _clipboardMonitor.Start(this);

            // Show Dynamic Island if enabled
            if (_settingsService.Current.ShowDynamicIsland)
            {
                _islandWindow.Show();
            }
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            if (!_isRealExit && _settingsService.Current.MinimizeToTrayOnClose)
            {
                e.Cancel = true;
                Hide();
                if (_notifyIcon != null)
                {
                    _notifyIcon.ShowBalloonTip(1500, "ClipClick", "ClipClick is minimized to system tray.", ToolTipIcon.Info);
                }
            }
            else
            {
                // Real exit
                _clipboardMonitor.Stop();
                _islandWindow.Close();
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
            }
        }

        private void OnIslandVisibilityToggled(bool isVisible)
        {
            if (isVisible)
            {
                _islandWindow.Show();
            }
            else
            {
                _islandWindow.Hide();
            }
        }

        private void OnIslandTopmostChanged(bool isTopmost)
        {
            _islandWindow.Topmost = isTopmost;
        }

        #region Custom TitleBar Controls

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
            }
            else
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void ToggleMaximize()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeIcon.Data = (System.Windows.Media.Geometry)FindResource("IconMaximize");
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaximizeIcon.Data = (System.Windows.Media.Geometry)FindResource("IconRestore");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region System Tray Integration

        private void InitializeSystemTray()
        {
            try
            {
                _notifyIcon = new NotifyIcon
                {
                    Text = "ClipClick - Clipboard & Media Hub",
                    Icon = CreateAppTrayIcon(),
                    Visible = true
                };

                _notifyIcon.DoubleClick += (s, e) =>
                {
                    Show();
                    WindowState = WindowState.Normal;
                    Activate();
                };

                var contextMenu = new ContextMenuStrip();

                var showItem = new ToolStripMenuItem("Open ClipClick", null, (s, e) =>
                {
                    Show();
                    WindowState = WindowState.Normal;
                    Activate();
                });
                showItem.Font = new Font(showItem.Font, System.Drawing.FontStyle.Bold);

                var islandItem = new ToolStripMenuItem("Toggle Dynamic Island", null, (s, e) =>
                {
                    _mainVM.SettingsVM.ShowDynamicIsland = !_mainVM.SettingsVM.ShowDynamicIsland;
                });

                var clearItem = new ToolStripMenuItem("Clear Clipboard History", null, (s, e) =>
                {
                    _mainVM.ClipboardVM.ClearAllCommand.Execute(null);
                });

                var exitItem = new ToolStripMenuItem("Exit ClipClick", null, (s, e) =>
                {
                    _isRealExit = true;
                    System.Windows.Application.Current.Shutdown();
                });

                contextMenu.Items.Add(showItem);
                contextMenu.Items.Add(islandItem);
                contextMenu.Items.Add(clearItem);
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add(exitItem);

                _notifyIcon.ContextMenuStrip = contextMenu;
            }
            catch
            {
            }
        }

        private Icon CreateAppTrayIcon()
        {
            // Dynamically create a sleek 16x16 / 32x32 dark icon with #B6FF00 accent dot
            using (var bmp = new Bitmap(32, 32))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);

                    // Dark capsule background
                    using (var brush = new SolidBrush(Color.FromArgb(14, 14, 14)))
                    {
                        g.FillEllipse(brush, 2, 2, 28, 28);
                    }

                    // Neon border
                    using (var pen = new Pen(Color.FromArgb(182, 255, 0), 2.5f))
                    {
                        g.DrawEllipse(pen, 3, 3, 26, 26);
                    }

                    // Inner neon geometric dot
                    using (var brush = new SolidBrush(Color.FromArgb(182, 255, 0)))
                    {
                        g.FillEllipse(brush, 11, 11, 10, 10);
                    }
                }
                return System.Drawing.Icon.FromHandle(bmp.GetHicon());
            }
        }

        #endregion
    }
}
