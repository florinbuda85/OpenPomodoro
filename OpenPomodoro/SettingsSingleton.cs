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
            File.WriteAllText("settings.json", json);

        }

        private void LoadSettings()
        {
            try
            {
                this.settingsHolder = JsonConvert.DeserializeObject<Settings>(File.ReadAllText("settings.json"));

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

        public int getSecondsUntilDesperateAlert()
        {
            return settingsHolder.SecondsUntilDesperateAlert;
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
    }
}
