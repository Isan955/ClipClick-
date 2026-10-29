using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Windows.Media.Control;

namespace ClipClick.Services
{
    public class ExternalMediaState
    {
        public bool IsAvailable { get; set; }
        public bool HasActiveSession { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string SourceApp { get; set; }
        public bool IsPlaying { get; set; }

        public ExternalMediaState()
        {
            Title = "No external media";
            Artist = "Spotify, Chrome, Edge, etc.";
            SourceApp = "External Media";
            IsPlaying = false;
            IsAvailable = false;
            HasActiveSession = false;
        }
    }

    public class ExternalMediaManagerService
    {
        private GlobalSystemMediaTransportControlsSessionManager _sessionManager;
        private GlobalSystemMediaTransportControlsSession _currentSession;
        private readonly Dispatcher _dispatcher;
        private readonly DispatcherTimer _pollTimer;
        private bool _isBusyUpdating = false;

        public ExternalMediaState CurrentState { get; private set; }
        public bool IsSupported { get; private set; }

        public event Action<ExternalMediaState> MediaStateChanged;

        public ExternalMediaManagerService(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            CurrentState = new ExternalMediaState();

            _pollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(900)
            };
            _pollTimer.Tick += OnPollTimerTick;

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                var op = GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                _sessionManager = await WindowsRuntimeSystemExtensions.AsTask(op);

                if (_sessionManager != null)
                {
                    IsSupported = true;
                    CurrentState.IsAvailable = true;

                    await RefreshCurrentMediaAsync();
                    _pollTimer.Start();
                }
                else
                {
                    SetUnavailable("GSMTC API not available on this system.");
                }
            }
            catch (Exception ex)
            {
                SetUnavailable(ex.Message);
            }
        }

        private void SetUnavailable(string reason)
        {
            IsSupported = false;
            CurrentState.IsAvailable = false;
            CurrentState.HasActiveSession = false;
            CurrentState.Title = "External Media Unavailable";
            CurrentState.Artist = reason;
            NotifyStateChanged();
        }

        private async void OnPollTimerTick(object sender, EventArgs e)
        {
            if (_isBusyUpdating || _sessionManager == null) return;
            await RefreshCurrentMediaAsync();
        }

        public async Task RefreshCurrentMediaAsync()
        {
            if (_sessionManager == null || _isBusyUpdating) return;

            _isBusyUpdating = true;
            try
            {
                _currentSession = _sessionManager.GetCurrentSession();

                if (_currentSession != null)
                {
                    var op = _currentSession.TryGetMediaPropertiesAsync();
                    var props = await WindowsRuntimeSystemExtensions.AsTask(op);

                    string appName = _currentSession.SourceAppUserModelId ?? "External Player";
                    if (appName.IndexOf("Spotify", StringComparison.OrdinalIgnoreCase) >= 0) appName = "Spotify";
                    else if (appName.IndexOf("Chrome", StringComparison.OrdinalIgnoreCase) >= 0) appName = "Chrome";
                    else if (appName.IndexOf("Edge", StringComparison.OrdinalIgnoreCase) >= 0) appName = "Edge";
                    else if (appName.IndexOf("Firefox", StringComparison.OrdinalIgnoreCase) >= 0) appName = "Firefox";

                    string title = props != null && !string.IsNullOrWhiteSpace(props.Title) ? props.Title : "Unknown Title";
                    string artist = props != null && !string.IsNullOrWhiteSpace(props.Artist) ? props.Artist : "Unknown Artist";

                    bool isPlaying = false;
                    var info = _currentSession.GetPlaybackInfo();
                    if (info != null)
                    {
                        isPlaying = (info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing);
                    }

                    // Only notify if something changed to minimize UI rerenders
                    if (!CurrentState.HasActiveSession ||
                        CurrentState.Title != title ||
                        CurrentState.Artist != artist ||
                        CurrentState.IsPlaying != isPlaying ||
                        CurrentState.SourceApp != appName)
                    {
                        CurrentState.HasActiveSession = true;
                        CurrentState.Title = title;
                        CurrentState.Artist = artist;
                        CurrentState.SourceApp = appName;
                        CurrentState.IsPlaying = isPlaying;
                        NotifyStateChanged();
                    }
                }
                else
                {
                    if (CurrentState.HasActiveSession)
                    {
                        CurrentState.HasActiveSession = false;
                        CurrentState.IsPlaying = false;
                        CurrentState.Title = "No Active Media";
                        CurrentState.Artist = "Play music in Spotify, YouTube, etc.";
                        CurrentState.SourceApp = "External Media";
                        NotifyStateChanged();
                    }
                }
            }
            catch
            {
            }
            finally
            {
                _isBusyUpdating = false;
            }
        }

        public async Task<bool> TogglePlayPauseAsync()
        {
            if (_currentSession == null) return false;
            try
            {
                var op = _currentSession.TryTogglePlayPauseAsync();
                bool res = await WindowsRuntimeSystemExtensions.AsTask(op);
                await RefreshCurrentMediaAsync();
                return res;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SkipPreviousAsync()
        {
            if (_currentSession == null) return false;
            try
            {
                var op = _currentSession.TrySkipPreviousAsync();
                bool res = await WindowsRuntimeSystemExtensions.AsTask(op);
                await RefreshCurrentMediaAsync();
                return res;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SkipNextAsync()
        {
            if (_currentSession == null) return false;
            try
            {
                var op = _currentSession.TrySkipNextAsync();
                bool res = await WindowsRuntimeSystemExtensions.AsTask(op);
                await RefreshCurrentMediaAsync();
                return res;
            }
            catch
            {
                return false;
            }
        }

        private void NotifyStateChanged()
        {
            _dispatcher.BeginInvoke(new Action(() =>
            {
                var handler = MediaStateChanged;
                if (handler != null)
                {
                    handler(CurrentState);
                }
            }));
        }
    }
}
