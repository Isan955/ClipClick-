using System;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace ClipClick.Services
{
    public class AppSettings
    {
        public bool StartWithWindows { get; set; }
        public bool MinimizeToTrayOnClose { get; set; }
        public bool ShowDynamicIsland { get; set; }
        public bool DynamicIslandTopmost { get; set; }
        public bool DynamicIslandAutoExpand { get; set; }
        public double IslandLeft { get; set; }
        public double IslandTop { get; set; }
        public int MaxClipboardEntries { get; set; }

        public AppSettings()
        {
            StartWithWindows = false;
            MinimizeToTrayOnClose = true;
            ShowDynamicIsland = true;
            DynamicIslandTopmost = true;
            DynamicIslandAutoExpand = false;
            IslandLeft = -1;
            IslandTop = -1;
            MaxClipboardEntries = 500;
        }
    }

    public class SettingsService
    {
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegistryName = "ClipClick";
        private readonly string _settingsFilePath;

        public AppSettings Current { get; private set; }

        public SettingsService()
        {
            string folder = DatabaseService.GetStorageDirectory();
            _settingsFilePath = Path.Combine(folder, "settings.xml");

            Load();
        }

        public void Load()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(AppSettings));
                    using (var reader = new StreamReader(_settingsFilePath))
                    {
                        Current = (AppSettings)serializer.Deserialize(reader);
                    }
                }
                catch
                {
                    Current = new AppSettings();
                }
            }
            else
            {
                Current = new AppSettings();
            }

            // Sync startup registry status
            Current.StartWithWindows = IsStartupWithWindowsEnabled();
        }

        public void Save()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(AppSettings));
                using (var writer = new StreamWriter(_settingsFilePath))
                {
                    serializer.Serialize(writer, Current);
                }
            }
            catch
            {
            }
        }

        public void SetStartWithWindows(bool enable)
        {
            Current.StartWithWindows = enable;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                            key.SetValue(AppRegistryName, string.Format("\"{0}\" --minimized", exePath));
                        }
                        else
                        {
                            key.DeleteValue(AppRegistryName, false);
                        }
                    }
                }
            }
            catch
            {
            }
            Save();
        }

        public bool IsStartupWithWindowsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false))
                {
                    if (key != null)
                    {
                        var val = key.GetValue(AppRegistryName);
                        return val != null;
                    }
                }
            }
            catch
            {
            }
            return false;
        }
    }
}
