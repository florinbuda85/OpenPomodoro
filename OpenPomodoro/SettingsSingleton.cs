using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenPomodoro
{

    public class SettingsSingleton
    {
        private static readonly string SettingsFilePath = Path.Combine(
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
            string json = JsonConvert.SerializeObject(settingsHolder);
            File.WriteAllText(SettingsFilePath, json);

        }

        private void LoadSettings()
        {
            try
            {
                this.settingsHolder = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SettingsFilePath));

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
    }
}
