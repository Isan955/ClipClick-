using System;
using System.IO;
using ClipClick.ViewModels;

namespace ClipClick.Models
{
    public class AudioTrack : ViewModelBase
    {
        private string _filePath;
        private string _title;
        private string _artist;
        private string _album;
        private TimeSpan _duration;
        private bool _isCurrent;

        public string FilePath
        {
            get { return _filePath; }
            set
            {
                if (SetProperty(ref _filePath, value))
                {
                    OnPropertyChanged("FileName");
                }
            }
        }

        public string Title
        {
            get { return string.IsNullOrEmpty(_title) ? FileName : _title; }
            set { SetProperty(ref _title, value); }
        }

        public string Artist
        {
            get { return string.IsNullOrEmpty(_artist) ? "Unknown Artist" : _artist; }
            set { SetProperty(ref _artist, value); }
        }

        public string Album
        {
            get { return string.IsNullOrEmpty(_album) ? "Unknown Album" : _album; }
            set { SetProperty(ref _album, value); }
        }

        public TimeSpan Duration
        {
            get { return _duration; }
            set
            {
                if (SetProperty(ref _duration, value))
                {
                    OnPropertyChanged("FormattedDuration");
                }
            }
        }

        public bool IsCurrent
        {
            get { return _isCurrent; }
            set { SetProperty(ref _isCurrent, value); }
        }

        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(_filePath)) return string.Empty;
                return Path.GetFileNameWithoutExtension(_filePath);
            }
        }

        public string FormattedDuration
        {
            get
            {
                if (_duration.TotalHours >= 1)
                {
                    return string.Format("{0:00}:{1:00}:{2:00}", (int)_duration.TotalHours, _duration.Minutes, _duration.Seconds);
                }
                return string.Format("{0:00}:{1:00}", _duration.Minutes, _duration.Seconds);
            }
        }
    }
}
