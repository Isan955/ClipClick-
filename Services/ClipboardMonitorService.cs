using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ClipClick.Services
{
    public class ClipboardMonitorService : IDisposable
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        public event Action<string> ClipboardTextChanged;

        private HwndSource _hwndSource;
        private IntPtr _windowHandle;
        private bool _isListening;
        private string _lastCapturedText;
        private DateTime _lastCapturedTime = DateTime.MinValue;
        private bool _suppressNextCapture;

        public void Start(Window window)
        {
            if (_isListening) return;

            try
            {
                var helper = new WindowInteropHelper(window);
                _windowHandle = helper.EnsureHandle();
                AttachListener();
            }
            catch
            {
                if (window != null)
                {
                    window.SourceInitialized += (s, e) =>
                    {
                        var helper = new WindowInteropHelper(window);
                        _windowHandle = helper.Handle;
                        AttachListener();
                    };
                }
            }
        }

        private void AttachListener()
        {
            if (_windowHandle == IntPtr.Zero || _isListening) return;

            _hwndSource = HwndSource.FromHwnd(_windowHandle);
            if (_hwndSource != null)
            {
                _hwndSource.AddHook(WndProc);
                AddClipboardFormatListener(_windowHandle);
                _isListening = true;
            }
        }

        public void Stop()
        {
            if (!_isListening) return;

            if (_windowHandle != IntPtr.Zero)
            {
                RemoveClipboardFormatListener(_windowHandle);
            }

            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource = null;
            }

            _isListening = false;
        }

        public void SetClipboardTextSilently(string text)
        {
            try
            {
                _suppressNextCapture = true;
                _lastCapturedText = text;
                _lastCapturedTime = DateTime.UtcNow;
                Clipboard.SetText(text);
            }
            catch
            {
                _suppressNextCapture = false;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                OnClipboardChanged();
            }
            return IntPtr.Zero;
        }

        private void OnClipboardChanged()
        {
            try
            {
                if (_suppressNextCapture)
                {
                    _suppressNextCapture = false;
                    return;
                }

                string text = null;
                // Retry reading clipboard up to 8 times with a small delay to handle another process temporarily locking the clipboard
                for (int i = 0; i < 8; i++)
                {
                    try
                    {
                        if (Clipboard.ContainsText())
                        {
                            text = Clipboard.GetText();
                            break;
                        }
                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(30);
                    }
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    DateTime now = DateTime.UtcNow;
                    // Ignore rapid duplicate Windows notifications (within 500ms for exact same text)
                    if (string.Equals(text, _lastCapturedText, StringComparison.Ordinal) && (now - _lastCapturedTime).TotalMilliseconds < 500)
                    {
                        return;
                    }

                    _lastCapturedText = text;
                    _lastCapturedTime = now;

                    var handler = ClipboardTextChanged;
                    if (handler != null)
                    {
                        handler(text);
                    }
                }
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
