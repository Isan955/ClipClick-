using System;
using ClipClick.Models;
using ClipClick.Services;
using NAudio.Wave;

namespace ClipClick.ViewModels
{
    public class IslandViewModel : ViewModelBase
    {
        private readonly LocalAudioPlayerService _localPlayer;
        private readonly ExternalMediaManagerService _externalMedia;

        private bool _isExpanded = false;
        private string _title = "ClipClick";
        private string _artist = "Ready to play";
        private string _source = "Local";
        private bool _isPlaying = false;
        private double _progress = 0.0;
        private string _timeString = "00:00 / 00:00";
        private bool _isHovered = false;

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { SetProperty(ref _isExpanded, value); }
        }

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public string Artist
        {
            get { return _artist; }
            set { SetProperty(ref _artist, value); }
        }

        public string Source
        {
            get { return _source; }
            set { SetProperty(ref _source, value); }
        }

        public bool IsPlaying
        {
            get { return _isPlaying; }
            set { SetProperty(ref _isPlaying, value); }
        }

        public double Progress
        {
            get { return _progress; }
            set { SetProperty(ref _progress, value); }
        }

        public string TimeString
        {
            get { return _timeString; }
            set { SetProperty(ref _timeString, value); }
        }

        public bool IsHovered
        {
            get { return _isHovered; }
            set { SetProperty(ref _isHovered, value); }
        }

        public RelayCommand TogglePlayPauseCommand { get; private set; }
        public RelayCommand PreviousCommand { get; private set; }
        public RelayCommand NextCommand { get; private set; }
        public RelayCommand ToggleExpandCommand { get; private set; }

        public IslandViewModel(LocalAudioPlayerService localPlayer, ExternalMediaManagerService externalMedia)
        {
            _localPlayer = localPlayer;
            _externalMedia = externalMedia;

            TogglePlayPauseCommand = new RelayCommand(TogglePlayPause);
            PreviousCommand = new RelayCommand(Previous);
            NextCommand = new RelayCommand(Next);
            ToggleExpandCommand = new RelayCommand(() => IsExpanded = !IsExpanded);

            // Hook local player
            _localPlayer.StateChanged += OnLocalStateChanged;
            _localPlayer.TrackChanged += OnLocalTrackChanged;
            _localPlayer.PositionChanged += OnLocalPositionChanged;

            // Hook external media
            if (_externalMedia != null)
            {
                _externalMedia.MediaStateChanged += OnExternalMediaChanged;
            }

            SyncDisplay();
        }

        private bool IsExternalActive()
        {
            if (_externalMedia == null) return false;
            // If external is playing and local is not, prefer external
            if (_externalMedia.CurrentState.IsPlaying && _localPlayer.PlaybackState != PlaybackState.Playing)
            {
                return true;
            }
            // If local has no tracks and external has a session
            if (_localPlayer.CurrentTrack == null && _externalMedia.CurrentState.HasActiveSession)
            {
                return true;
            }
            return false;
        }

        private void TogglePlayPause()
        {
            if (IsExternalActive())
            {
                var task = _externalMedia.TogglePlayPauseAsync();
            }
            else
            {
                _localPlayer.TogglePlayPause();
            }
        }

        private void Previous()
        {
            if (IsExternalActive())
            {
                var task = _externalMedia.SkipPreviousAsync();
            }
            else
            {
                _localPlayer.Previous();
            }
        }

        private void Next()
        {
            if (IsExternalActive())
            {
                var task = _externalMedia.SkipNextAsync();
            }
            else
            {
                _localPlayer.Next();
            }
        }

        private void OnLocalStateChanged(PlaybackState state)
        {
            SyncDisplay();
        }

        private void OnLocalTrackChanged(AudioTrack track)
        {
            SyncDisplay();
        }

        private void OnLocalPositionChanged(TimeSpan current, TimeSpan total)
        {
            if (!IsExternalActive())
            {
                if (total.TotalSeconds > 0)
                {
                    Progress = Math.Min(1.0, current.TotalSeconds / total.TotalSeconds);
                    TimeString = string.Format("{0:00}:{1:00} / {2:00}:{3:00}",
                        current.Minutes, current.Seconds, total.Minutes, total.Seconds);
                }
                else
                {
                    Progress = 0.0;
                    TimeString = "00:00 / 00:00";
                }
            }
        }

        private void OnExternalMediaChanged(ExternalMediaState state)
        {
            SyncDisplay();
        }

        public void SyncDisplay()
        {
            if (IsExternalActive())
            {
                var ext = _externalMedia.CurrentState;
                Title = ext.Title;
                Artist = ext.Artist;
                Source = ext.SourceApp;
                IsPlaying = ext.IsPlaying;
                TimeString = "External Media";
                Progress = 0.5; // Neutral activity indicator
            }
            else
            {
                var track = _localPlayer.CurrentTrack;
                if (track != null)
                {
                    Title = track.Title;
                    Artist = track.Artist;
                    Source = "Local";
                }
                else
                {
                    Title = "ClipClick Island";
                    Artist = "No track playing";
                    Source = "ClipClick";
                    TimeString = "00:00 / 00:00";
                    Progress = 0.0;
                }
                IsPlaying = (_localPlayer.PlaybackState == PlaybackState.Playing);
            }
        }
    }
}
