using System;
using System.IO;
using System.Windows;
using ClipClick.Services;
using ClipClick.ViewModels;
using ClipClick.Views;

namespace ClipClick
{
    public partial class App : Application
    {
        private DatabaseService _dbService;
        private SettingsService _settingsService;
        private ClipboardMonitorService _clipboardMonitor;
        private LocalAudioPlayerService _localPlayer;
        private ExternalMediaManagerService _externalMedia;

        private DynamicIslandWindow _islandWindow;
        private MainWindow _mainWindow;

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                LogException("UnhandledException", args.ExceptionObject as Exception);
            };
            DispatcherUnhandledException += (s, args) =>
            {
                LogException("DispatcherUnhandledException", args.Exception);
                args.Handled = true;
            };
        }

        private static void LogException(string title, Exception ex)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                File.AppendAllText(logPath, string.Format("[{0}] {1}: {2}\r\n{3}\r\n\r\n",
                    DateTime.Now, title, ex != null ? ex.Message : "Unknown", ex != null ? ex.StackTrace : ""));
            }
            catch
            {
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // Initialize Services
                _settingsService = new SettingsService();
                _dbService = new DatabaseService(_settingsService.Current.MaxClipboardEntries);
                _clipboardMonitor = new ClipboardMonitorService();
                _localPlayer = new LocalAudioPlayerService();
                _externalMedia = new ExternalMediaManagerService(Dispatcher);

                // Initialize ViewModels
                var clipboardVM = new ClipboardViewModel(_dbService, _clipboardMonitor, Dispatcher);
                var playerVM = new PlayerViewModel(_localPlayer, _externalMedia);
                var islandVM = new IslandViewModel(_localPlayer, _externalMedia);
                var settingsVM = new SettingsViewModel(_settingsService, _dbService);

                var mainVM = new MainViewModel(clipboardVM, playerVM, islandVM, settingsVM);

                // Create Dynamic Island Window
                _islandWindow = new DynamicIslandWindow(islandVM, _settingsService);

                // Create Main Window
                _mainWindow = new MainWindow(mainVM, _clipboardMonitor, _settingsService, _islandWindow);

                // Check if launched with --minimized flag (e.g. Windows startup)
                bool startMinimized = false;
                if (e.Args != null)
                {
                    foreach (var arg in e.Args)
                    {
                        if (string.Equals(arg, "--minimized", StringComparison.OrdinalIgnoreCase))
                        {
                            startMinimized = true;
                            break;
                        }
                    }
                }

                if (!startMinimized)
                {
                    _mainWindow.Show();
                }
            }
            catch (Exception ex)
            {
                LogException("OnStartupException", ex);
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (_localPlayer != null)
                {
                    _localPlayer.Dispose();
                }

                if (_clipboardMonitor != null)
                {
                    _clipboardMonitor.Dispose();
                }

                if (_settingsService != null)
                {
                    _settingsService.Save();
                }
            }
            catch
            {
            }

            base.OnExit(e);
        }
    }
}
