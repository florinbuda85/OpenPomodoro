using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Windows;
using System.Windows.Input;

namespace OpenPomodoro.ViewModel
{
    public class SettingsViewModel : ViewModelBase
    {
        public SettingsViewModel()
        {
            SettingsHolder = SettingsSingleton.getInstance().GetSettings();
        }

        #region Property SettingsHolder
        private Settings _settingsHolder;
        public Settings SettingsHolder
        {
            get
            {
                return _settingsHolder;
            }
            set
            {
                _settingsHolder = value;
                RaisePropertyChanged("SettingsHolder");
            }
        }
        #endregion

        #region ICommand DoSaveSettings
        private ICommand _doSaveSettings;
        public ICommand DoSaveSettings
        {
            get
            {
                if (_doSaveSettings == null)
                {
                    _doSaveSettings = new RelayCommand(DoSaveSettingsExecute); // 
                }
                return _doSaveSettings;
            }
        }
        private void DoSaveSettingsExecute()
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            try
            {
                SettingsSingleton.getInstance().SaveSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception found on DoSaveSettingsExecute :" + ex.Message);
            }
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
        }
        #endregion

    }
}