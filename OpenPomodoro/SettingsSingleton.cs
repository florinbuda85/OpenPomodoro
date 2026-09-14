using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenPomodoro
{

    public class SettingsSingleton
    {
        private static readonly string UserSettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "OpenPomodoroSettings.json");
        private static readonly string BundledSettingsFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "settings.json");

        private SettingsSingleton() { }

        private static SettingsSingleton _instance = null;

        private Settings settingsHolder;

        public static SettingsSingleton getInstance()
        {
            if (_instance == null)
            {
                _instance = new SettingsSingleton();

                _instance.LoadSettings();
            }
            return _instance;
        }

        public void SaveSettings()
        {
            TimeSpan start;
            TimeSpan end;
            if (!settingsHolder.TryGetDayTimelineHours(out start, out end))
            {
                throw new ArgumentException("Enter timeline times as HH:mm, with the end later than the start (e.g. 09:00 to 18:00).");
            }
            string json = JsonConvert.SerializeObject(settingsHolder);
            File.WriteAllText(UserSettingsFilePath, json);

        }

        private void LoadSettings()
        {
            try
            {
                string settingsPath = File.Exists(UserSettingsFilePath)
                    ? UserSettingsFilePath
                    : BundledSettingsFilePath;
                this.settingsHolder = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsPath));
                if (this.settingsHolder == null)
                {
                    throw new InvalidDataException("Settings file is empty.");
                }

                // Migrate the first run's bundled settings to the user-owned file.
                if (!File.Exists(UserSettingsFilePath))
                {
                    SaveSettings();
                }

            } catch (Exception)
            {
                settingsHolder = new Settings();
                settingsHolder.DurationShortPause = 5 * 60;
                settingsHolder.DurationLongPause = 15 * 60;
                settingsHolder.DurationWork = 25 * 60;

                settingsHolder.SecondsUntilDesperateAlert = 300;
                SaveSettings();
            }
        }

        public int getDurationShortPause()
        {
            return settingsHolder.DurationShortPause;
        }

        public int GetDurationLongPause()
        {
            return settingsHolder.DurationLongPause;
        }

        public int getDurationWork()
        {
            return settingsHolder.DurationWork;
        }

        public bool shouldDisplayPauseAdvices()
        {
            return settingsHolder.DisplayPauseAdvices;
        }

        public int getSecondsUntilDesperateAlert()
        {
            return settingsHolder.SecondsUntilDesperateAlert;
        }

        public bool isTickerEnabled()
        {
            return settingsHolder.TickerEnabled;
        }

        public int getTickerVolume()
        {
            return Math.Max(0, Math.Min(100, settingsHolder.TickerVolume));
        }

        public Settings GetSettings()
        {
            return settingsHolder;
        }
    }

    public class Settings
    {
        public int DurationShortPause { get; set; }
        public int DurationLongPause { get; set; }
        public int DurationWork { get; set; }

        public int SecondsUntilDesperateAlert { get; set; }

        public bool DisplayPauseAdvices { get; set; }

        public bool TickerEnabled { get; set; } = true;

        public int TickerVolume { get; set; } = 70;

        public string DayTimelineStart { get; set; } = "09:00";
        public string DayTimelineEnd { get; set; } = "18:00";

        public bool TryGetDayTimelineHours(out TimeSpan start, out TimeSpan end)
        {
            bool validStart = TimeSpan.TryParseExact(DayTimelineStart, @"hh\:mm", CultureInfo.InvariantCulture, out start);
            bool validEnd = TimeSpan.TryParseExact(DayTimelineEnd, @"hh\:mm", CultureInfo.InvariantCulture, out end);
            return validStart && validEnd && start < end;
        }
    }
}
