using System;
using ClipClick.ViewModels;

namespace ClipClick.Models
{
    public class ClipboardItem : ViewModelBase
    {
        private int _id;
        private string _content;
        private DateTime _createdAt;
        private bool _isPinned;
        private int _charCount;
        private bool _isJustCopied;

        public int Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        public string Content
        {
            get { return _content; }
            set
            {
                if (SetProperty(ref _content, value))
                {
                    OnPropertyChanged("PreviewText");
                    OnPropertyChanged("CharCount");
                }
            }
        }

        public DateTime CreatedAt
        {
            get { return _createdAt; }
            set
            {
                if (SetProperty(ref _createdAt, value))
                {
                    OnPropertyChanged("FormattedTime");
                }
            }
        }

        public bool IsPinned
        {
            get { return _isPinned; }
            set { SetProperty(ref _isPinned, value); }
        }

        public int CharCount
        {
            get { return _charCount > 0 ? _charCount : (_content != null ? _content.Length : 0); }
            set { SetProperty(ref _charCount, value); }
        }

        public bool IsJustCopied
        {
            get { return _isJustCopied; }
            set { SetProperty(ref _isJustCopied, value); }
        }

        public string PreviewText
        {
            get
            {
                if (string.IsNullOrEmpty(_content)) return string.Empty;
                string trimmed = _content.Trim();
                if (trimmed.Length > 240)
                {
                    return trimmed.Substring(0, 240) + "...";
                }
                return trimmed;
            }
        }

        public string FormattedTime
        {
            get
            {
                TimeSpan diff = DateTime.Now - _createdAt;
                if (diff.TotalSeconds < 60) return "Just now";
                if (diff.TotalMinutes < 60) return string.Format("{0}m ago", (int)diff.TotalMinutes);
                if (diff.TotalHours < 24) return string.Format("{0}h ago", (int)diff.TotalHours);
                if (diff.TotalDays < 2) return "Yesterday";
                if (diff.TotalDays < 7) return string.Format("{0}d ago", (int)diff.TotalDays);
                return _createdAt.ToString("MMM dd, yyyy");
            }
        }
    }
}
