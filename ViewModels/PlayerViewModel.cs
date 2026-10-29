using System;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using ClipClick.Models;
using ClipClick.Services;
using NAudio.Wave;

namespace ClipClick.ViewModels
{
    public class PlayerViewModel : ViewModelBase
    {
        private readonly LocalAudioPlayerService _localPlayer;
        private readonly ExternalMediaManagerService _externalMedia;

        private bool _isExternalMode = false;
        private string _trackTitle = "No track playing";
        private string _trackArtist = "Add songs to playlist";
        private string _trackSource = "Local Player";
        private bool _isPlaying = false;
        private double _positionSeconds = 0;
        private double _durationSeconds = 1;
        private string _timeString = "00:00 / 00:00";
        private float _volume = 80;
        private bool _isMuted = false;
        private bool _isUpdatingPositionFromPlayer = false;
        private bool _isSeeking = false;

        public bool IsLocalMode
        {
            get { return !_isExternalMode; }
            set
            {
                if (value && _isExternalMode)
                {
                    IsExternalMode = false;
                }
            }
        }

        public bool IsExternalMode
        {
            get { return _isExternalMode; }
            set
            {
                if (SetProperty(ref _isExternalMode, value))
                {
                    OnPropertyChanged("IsLocalMode");
                    UpdateCurrentDisplay();
                }
            }
        }

        public bool IsPlaylistEmpty
        {
            get { return Playlist == null || Playlist.Count == 0; }
        }

        public bool HasPlaylistItems
        {
            get { return !IsPlaylistEmpty; }
        }

        public string TrackTitle
        {
            get { return _trackTitle; }
            set { SetProperty(ref _trackTitle, value); }
        }

        public string TrackArtist
        {
            get { return _trackArtist; }
            set { SetProperty(ref _trackArtist, value); }
        }

        public string TrackSource
        {
            get { return _trackSource; }
            set { SetProperty(ref _trackSource, value); }
        }

        public bool IsPlaying
        {
            get { return _isPlaying; }
            set { SetProperty(ref _isPlaying, value); }
        }

        public double PositionSeconds
        {
            get { return _positionSeconds; }
            set
            {
                if (Math.Abs(_positionSeconds - value) > 0.05)
                {
                    _positionSeconds = value;
                    OnPropertyChanged("PositionSeconds");

                    if (!_isUpdatingPositionFromPlayer && !_isSeeking && !_isExternalMode)
                    {
                        _localPlayer.Seek(TimeSpan.FromSeconds(value));
                    }
                }
            }
        }

        public double DurationSeconds
        {
            get { return _durationSeconds; }
            set { SetProperty(ref _durationSeconds, value); }
        }

        public string TimeString
        {
            get { return _timeString; }
            set { SetProperty(ref _timeString, value); }
        }

        public float Volume
        {
            get { return _volume; }
            set
            {
                if (SetProperty(ref _volume, value))
                {
                    _localPlayer.Volume = value / 100.0f;
                }
            }
        }

        public bool IsMuted
        {
            get { return _isMuted; }
            set
            {
                if (SetProperty(ref _isMuted, value))
                {
                    _localPlayer.IsMuted = value;
                }
            }
        }

        public ObservableCollection<AudioTrack> Playlist
        {
            get { return _localPlayer.Playlist; }
        }

        public RelayCommand TogglePlayPauseCommand { get; private set; }
        public RelayCommand PreviousCommand { get; private set; }
        public RelayCommand NextCommand { get; private set; }
        public RelayCommand StopCommand { get; private set; }
        public RelayCommand PlayTrackCommand { get; private set; }
        public RelayCommand RemoveTrackCommand { get; private set; }
        public RelayCommand ClearPlaylistCommand { get; private set; }
        public RelayCommand AddFilesCommand { get; private set; }
        public RelayCommand AddFolderCommand { get; private set; }
        public RelayCommand ToggleMuteCommand { get; private set; }
        public RelayCommand SetLocalModeCommand { get; private set; }
        public RelayCommand SetExternalModeCommand { get; private set; }

        public PlayerViewModel(LocalAudioPlayerService localPlayer, ExternalMediaManagerService externalMedia)
        {
            _localPlayer = localPlayer;
            _externalMedia = externalMedia;

            TogglePlayPauseCommand = new RelayCommand(TogglePlayPause);
            PreviousCommand = new RelayCommand(Previous);
            NextCommand = new RelayCommand(Next);
            StopCommand = new RelayCommand(Stop);
            PlayTrackCommand = new RelayCommand(p => PlayTrack(p as AudioTrack));
            RemoveTrackCommand = new RelayCommand(p => _localPlayer.RemoveTrack(p as AudioTrack));
            ClearPlaylistCommand = new RelayCommand(_localPlayer.ClearPlaylist);
            AddFilesCommand = new RelayCommand(AddFiles);
            AddFolderCommand = new RelayCommand(AddFolder);
            ToggleMuteCommand = new RelayCommand(_localPlayer.ToggleMute);
            SetLocalModeCommand = new RelayCommand(() => IsExternalMode = false);
            SetExternalModeCommand = new RelayCommand(() => IsExternalMode = true);

            _localPlayer.Playlist.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged("IsPlaylistEmpty");
                OnPropertyChanged("HasPlaylistItems");
            };

            // Hook local player events
            _localPlayer.StateChanged += OnLocalStateChanged;
            _localPlayer.TrackChanged += OnLocalTrackChanged;
            _localPlayer.PositionChanged += OnLocalPositionChanged;
            _localPlayer.VolumeChanged += OnLocalVolumeChanged;

            // Hook external media events
            if (_externalMedia != null)
            {
                _externalMedia.MediaStateChanged += OnExternalMediaChanged;
            }

            _volume = _localPlayer.Volume * 100.0f;
            _isMuted = _localPlayer.IsMuted;

            UpdateCurrentDisplay();
        }

        public void StartSeeking()
        {
            _isSeeking = true;
        }

        public void StopSeeking(double targetSeconds)
        {
            _isSeeking = false;
            _localPlayer.Seek(TimeSpan.FromSeconds(targetSeconds));
        }

        private void TogglePlayPause()
        {
            if (_isExternalMode)
            {
                if (_externalMedia != null)
                {
                    var task = _externalMedia.TogglePlayPauseAsync();
                }
            }
            else
            {
                _localPlayer.TogglePlayPause();
            }
        }

        private void Previous()
        {
            if (_isExternalMode)
            {
                if (_externalMedia != null)
                {
                    var task = _externalMedia.SkipPreviousAsync();
                }
            }
            else
            {
                _localPlayer.Previous();
            }
        }

        private void Next()
        {
            if (_isExternalMode)
            {
                if (_externalMedia != null)
                {
                    var task = _externalMedia.SkipNextAsync();
                }
            }
            else
            {
                _localPlayer.Next();
            }
        }

        private void Stop()
        {
            if (!_isExternalMode)
            {
                _localPlayer.Stop();
            }
        }

        private void PlayTrack(AudioTrack track)
        {
            if (track != null)
            {
                IsExternalMode = false;
                _localPlayer.PlayTrack(track);
            }
        }

        private void AddFiles()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select Audio Files",
                Multiselect = true,
                Filter = "Audio Files (*.mp3;*.wav;*.flac;*.m4a;*.aac;*.wma)|*.mp3;*.wav;*.flac;*.m4a;*.aac;*.wma|All Files (*.*)|*.*"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _localPlayer.AddFiles(dlg.FileNames);
            }
        }

        private void AddFolder()
        {
            var dlg = new FolderBrowserDialog
            {
                Description = "Select Folder Containing Music",
                ShowNewFolderButton = false
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _localPlayer.AddFolder(dlg.SelectedPath);
            }
        }

        private void OnLocalStateChanged(PlaybackState state)
        {
            if (!_isExternalMode)
            {
                IsPlaying = (state == PlaybackState.Playing);
            }
        }

        private void OnLocalTrackChanged(AudioTrack track)
        {
            if (!_isExternalMode)
            {
                UpdateCurrentDisplay();
            }
        }

        private void OnLocalPositionChanged(TimeSpan current, TimeSpan total)
        {
            if (!_isExternalMode)
            {
                _isUpdatingPositionFromPlayer = true;
                try
                {
                    _positionSeconds = current.TotalSeconds;
                    _durationSeconds = total.TotalSeconds > 0 ? total.TotalSeconds : 1;
                    OnPropertyChanged("PositionSeconds");
                    OnPropertyChanged("DurationSeconds");

                    TimeString = string.Format("{0:00}:{1:00} / {2:00}:{3:00}",
                        current.Minutes, current.Seconds, total.Minutes, total.Seconds);
                }
                finally
                {
                    _isUpdatingPositionFromPlayer = false;
                }
            }
        }

        private void OnLocalVolumeChanged(float vol, bool muted)
        {
            _volume = vol * 100.0f;
            _isMuted = muted;
            OnPropertyChanged("Volume");
            OnPropertyChanged("IsMuted");
        }

        private void OnExternalMediaChanged(ExternalMediaState state)
        {
            if (_isExternalMode)
            {
                UpdateCurrentDisplay();
            }
            // Auto switch to external if external starts playing while local is stopped
            else if (state.IsPlaying && _localPlayer.PlaybackState != PlaybackState.Playing)
            {
                IsExternalMode = true;
            }
        }

        private void UpdateCurrentDisplay()
        {
            if (_isExternalMode)
            {
                var state = _externalMedia != null ? _externalMedia.CurrentState : new ExternalMediaState();
                TrackTitle = state.Title;
                TrackArtist = state.Artist;
                TrackSource = state.SourceApp;
                IsPlaying = state.IsPlaying;
                TimeString = "External Media (Live)";
                PositionSeconds = 0;
                DurationSeconds = 1;
            }
            else
            {
                var current = _localPlayer.CurrentTrack;
                if (current != null)
                {
                    TrackTitle = current.Title;
                    TrackArtist = current.Artist;
                    TrackSource = "Local Music Player";
                }
                else
                {
                    TrackTitle = "No song selected";
                    TrackArtist = "Add songs to start playing";
                    TrackSource = "Local Music Player";
                    TimeString = "00:00 / 00:00";
                    PositionSeconds = 0;
                    DurationSeconds = 1;
                }
                IsPlaying = (_localPlayer.PlaybackState == PlaybackState.Playing);
            }
        }
    }
}
