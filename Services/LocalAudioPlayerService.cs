using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using NAudio.Wave;
using ClipClick.Models;

namespace ClipClick.Services
{
    public class LocalAudioPlayerService : IDisposable
    {
        private WaveOutEvent _waveOut;
        private AudioFileReader _audioReader;
        private readonly DispatcherTimer _positionTimer;
        private float _volume = 0.8f;
        private bool _isMuted = false;
        private int _currentIndex = -1;

        public ObservableCollection<AudioTrack> Playlist { get; private set; }

        public AudioTrack CurrentTrack
        {
            get
            {
                if (_currentIndex >= 0 && _currentIndex < Playlist.Count)
                {
                    return Playlist[_currentIndex];
                }
                return null;
            }
        }

        public PlaybackState PlaybackState
        {
            get
            {
                return _waveOut != null ? _waveOut.PlaybackState : PlaybackState.Stopped;
            }
        }

        public float Volume
        {
            get { return _volume; }
            set
            {
                _volume = Math.Max(0.0f, Math.Min(1.0f, value));
                if (_audioReader != null && !_isMuted)
                {
                    _audioReader.Volume = _volume;
                }
                if (_volume > 0) _isMuted = false;
                var handler = VolumeChanged;
                if (handler != null) handler(_volume, _isMuted);
            }
        }

        public bool IsMuted
        {
            get { return _isMuted; }
            set
            {
                _isMuted = value;
                if (_audioReader != null)
                {
                    _audioReader.Volume = _isMuted ? 0.0f : _volume;
                }
                var handler = VolumeChanged;
                if (handler != null) handler(_volume, _isMuted);
            }
        }

        public TimeSpan CurrentPosition
        {
            get
            {
                return _audioReader != null ? _audioReader.CurrentTime : TimeSpan.Zero;
            }
        }

        public TimeSpan TotalDuration
        {
            get
            {
                return _audioReader != null ? _audioReader.TotalTime : TimeSpan.Zero;
            }
        }

        public event Action<PlaybackState> StateChanged;
        public event Action<AudioTrack> TrackChanged;
        public event Action<TimeSpan, TimeSpan> PositionChanged;
        public event Action<float, bool> VolumeChanged;

        public LocalAudioPlayerService()
        {
            Playlist = new ObservableCollection<AudioTrack>();

            _positionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _positionTimer.Tick += OnPositionTimerTick;
        }

        public void AddFiles(IEnumerable<string> filePaths)
        {
            foreach (var path in filePaths)
            {
                if (File.Exists(path) && IsSupportedAudio(path))
                {
                    var track = CreateTrack(path);
                    Playlist.Add(track);
                }
            }

            if (_currentIndex == -1 && Playlist.Count > 0)
            {
                _currentIndex = 0;
                Playlist[0].IsCurrent = true;
                var handler = TrackChanged;
                if (handler != null) handler(Playlist[0]);
            }
        }

        public void AddFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return;

            string[] extensions = new string[] { "*.mp3", "*.wav", "*.m4a", "*.aac", "*.flac", "*.wma" };
            var files = new List<string>();
            foreach (var ext in extensions)
            {
                try
                {
                    files.AddRange(Directory.GetFiles(folderPath, ext, SearchOption.AllDirectories));
                }
                catch
                {
                }
            }
            AddFiles(files);
        }

        public void RemoveTrack(AudioTrack track)
        {
            if (track == null) return;
            int index = Playlist.IndexOf(track);
            if (index == -1) return;

            if (index == _currentIndex)
            {
                Stop();
                Playlist.RemoveAt(index);
                if (Playlist.Count > 0)
                {
                    _currentIndex = Math.Min(index, Playlist.Count - 1);
                    Playlist[_currentIndex].IsCurrent = true;
                    var handler = TrackChanged;
                    if (handler != null) handler(Playlist[_currentIndex]);
                }
                else
                {
                    _currentIndex = -1;
                    var handler = TrackChanged;
                    if (handler != null) handler(null);
                }
            }
            else
            {
                Playlist.RemoveAt(index);
                if (index < _currentIndex)
                {
                    _currentIndex--;
                }
            }
        }

        public void ClearPlaylist()
        {
            Stop();
            Playlist.Clear();
            _currentIndex = -1;
            var handler = TrackChanged;
            if (handler != null) handler(null);
        }

        public void PlayTrack(AudioTrack track)
        {
            int index = Playlist.IndexOf(track);
            if (index >= 0)
            {
                PlayIndex(index);
            }
        }

        public void PlayIndex(int index)
        {
            if (index < 0 || index >= Playlist.Count) return;

            // Stop previous playback
            CleanupPlayback();

            // Reset flags
            for (int i = 0; i < Playlist.Count; i++)
            {
                Playlist[i].IsCurrent = (i == index);
            }
            _currentIndex = index;

            var track = Playlist[_currentIndex];

            try
            {
                _audioReader = new AudioFileReader(track.FilePath);
                _audioReader.Volume = _isMuted ? 0f : _volume;

                _waveOut = new WaveOutEvent();
                _waveOut.Init(_audioReader);
                _waveOut.PlaybackStopped += OnPlaybackStopped;
                _waveOut.Play();

                _positionTimer.Start();

                var trackHandler = TrackChanged;
                if (trackHandler != null) trackHandler(track);

                var stateHandler = StateChanged;
                if (stateHandler != null) stateHandler(PlaybackState.Playing);
            }
            catch (Exception)
            {
                // In case file is missing or corrupted, try next
                Next();
            }
        }

        public void Play()
        {
            if (_waveOut != null && _waveOut.PlaybackState == PlaybackState.Paused)
            {
                _waveOut.Play();
                _positionTimer.Start();
                var handler = StateChanged;
                if (handler != null) handler(PlaybackState.Playing);
            }
            else if (Playlist.Count > 0)
            {
                if (_currentIndex < 0) _currentIndex = 0;
                PlayIndex(_currentIndex);
            }
        }

        public void Pause()
        {
            if (_waveOut != null && _waveOut.PlaybackState == PlaybackState.Playing)
            {
                _waveOut.Pause();
                _positionTimer.Stop();
                var handler = StateChanged;
                if (handler != null) handler(PlaybackState.Paused);
            }
        }

        public void TogglePlayPause()
        {
            if (PlaybackState == PlaybackState.Playing)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        public void Stop()
        {
            if (_waveOut != null)
            {
                _waveOut.Stop();
            }
            CleanupPlayback();
            var handler = StateChanged;
            if (handler != null) handler(PlaybackState.Stopped);
        }

        public void Next()
        {
            if (Playlist.Count == 0) return;
            int nextIndex = _currentIndex + 1;
            if (nextIndex >= Playlist.Count)
            {
                nextIndex = 0; // Loop playlist
            }
            PlayIndex(nextIndex);
        }

        public void Previous()
        {
            if (Playlist.Count == 0) return;

            // If more than 3 seconds in, restart track
            if (CurrentPosition.TotalSeconds > 3)
            {
                Seek(TimeSpan.Zero);
                return;
            }

            int prevIndex = _currentIndex - 1;
            if (prevIndex < 0)
            {
                prevIndex = Playlist.Count - 1;
            }
            PlayIndex(prevIndex);
        }

        public void Seek(TimeSpan position)
        {
            if (_audioReader != null)
            {
                if (position < TimeSpan.Zero) position = TimeSpan.Zero;
                if (position > _audioReader.TotalTime) position = _audioReader.TotalTime;
                _audioReader.CurrentTime = position;

                var handler = PositionChanged;
                if (handler != null) handler(_audioReader.CurrentTime, _audioReader.TotalTime);
            }
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
        }

        private void OnPositionTimerTick(object sender, EventArgs e)
        {
            if (_audioReader != null && PlaybackState == PlaybackState.Playing)
            {
                var handler = PositionChanged;
                if (handler != null)
                {
                    handler(_audioReader.CurrentTime, _audioReader.TotalTime);
                }
            }
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            _positionTimer.Stop();

            // Check if track reached end naturally
            if (_audioReader != null && _audioReader.CurrentTime >= _audioReader.TotalTime - TimeSpan.FromSeconds(0.5))
            {
                Next();
            }
            else
            {
                var handler = StateChanged;
                if (handler != null) handler(PlaybackState.Stopped);
            }
        }

        private void CleanupPlayback()
        {
            _positionTimer.Stop();

            if (_waveOut != null)
            {
                _waveOut.PlaybackStopped -= OnPlaybackStopped;
                _waveOut.Dispose();
                _waveOut = null;
            }

            if (_audioReader != null)
            {
                _audioReader.Dispose();
                _audioReader = null;
            }
        }

        private AudioTrack CreateTrack(string filePath)
        {
            var track = new AudioTrack
            {
                FilePath = filePath
            };

            // Extract duration and metadata
            try
            {
                using (var reader = new AudioFileReader(filePath))
                {
                    track.Duration = reader.TotalTime;
                }
            }
            catch
            {
                track.Duration = TimeSpan.Zero;
            }

            // Parse artist and title from filename if in format "Artist - Title.mp3"
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            if (fileName.Contains(" - "))
            {
                var parts = fileName.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    track.Artist = parts[0].Trim();
                    track.Title = parts[1].Trim();
                }
                else
                {
                    track.Title = fileName;
                }
            }
            else
            {
                track.Title = fileName;
            }

            return track;
        }

        private bool IsSupportedAudio(string path)
        {
            string ext = Path.GetExtension(path);
            if (string.IsNullOrEmpty(ext)) return false;
            ext = ext.ToLowerInvariant();
            return ext == ".mp3" || ext == ".wav" || ext == ".m4a" || ext == ".aac" || ext == ".flac" || ext == ".wma";
        }

        public void Dispose()
        {
            CleanupPlayback();
        }
    }
}
