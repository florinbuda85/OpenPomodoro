

using MahApps.Metro;
using MahApps.Metro.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenPomodoro
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        int currentWindowState;
        int previouWindowState;

        Timer getAttentionTimer;
        Timer workTimer;

        DateTime startTime;
        int targetSeconds = 0;
        int deadTimeSeconds = 0; //todo: get rid of this..

        const string WORK_INPROGRESS = "img/tomato-icon-gray.png";
        const string WORK_COMPLETED = "img/tomato-icon.png";

        const string PAUSE_IN_PROGRES = "img/lemon-icon.png";
        const string PAUSE_COMPLETED = "img/circle.png";



        public MainWindow()
        {
            InitializeComponent();

            getAttentionTimer = new Timer();
            getAttentionTimer.Interval = 200;
            //getAttentionTimer.Start();
            getAttentionTimer.Elapsed += new ElapsedEventHandler(OnAtentionTimerdEvent);


            this.DataContext = this;

            workTimer = new Timer();
            workTimer.Interval = 500;
            workTimer.Elapsed += new ElapsedEventHandler(OnWorkTimerdEvent);

            this.SetWindowState(WStates.DEFAULT);

            Pomodoros = new ObservableCollection<string>();


            // start working
            this.SetWindowState(WStates.WORKING);


        }
        #region Property TextTimePassed
        private String _textTimePassed;
        public String TextTimePassed
        {
            get
            {
                if (_textTimePassed == null)
                {
                    _textTimePassed = "--:--";
                }

                return _textTimePassed;
            }
            set
            {
                _textTimePassed = value;
                OnPropertyChanged("TextTimePassed");
            }
        }
        #endregion

        #region Property TextTimeLeft
        private String _textTimeLeft;
        public String TextTimeLeft
        {
            get
            {
                if (_textTimeLeft == null)
                {
                    _textTimeLeft = "--:--";
                }
                return _textTimeLeft;
            }
            set
            {
                _textTimeLeft = value;
                OnPropertyChanged("TextTimeLeft");
            }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (!string.IsNullOrEmpty(propertyName))
            {
                if (this.PropertyChanged != null)
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #region ObservableCollection Pomodoros
        private ObservableCollection<String> _pomodoros;
        public ObservableCollection<String> Pomodoros
        {
            get
            {
                return _pomodoros;
            }
            set
            {
                _pomodoros = value;
                OnPropertyChanged("Pomodoros");
            }
        }
        #endregion



        private void OnAtentionTimerdEvent(object source, ElapsedEventArgs e)
        {
            if (++deadTimeSeconds >= SettingsSingleton.getInstance().getSecondsUntilDesperateAlert())
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (deadTimeSeconds % 2 == 0)
                    {
                        this.Icon = new BitmapImage(new Uri("pack://application:,,,/OpenPomodoro;component/img/attention-icon.png"));
                    }
                    else
                    {
                        this.Icon = new BitmapImage(new Uri("pack://application:,,,/OpenPomodoro;component/img/tomato-icon.png"));
                    }

                    if (deadTimeSeconds % 7 == 0)
                    {
                        this.Background = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    }
                    else
                    {
                        this.Background = new SolidColorBrush(Color.FromArgb(255, 233, 236, 255));
                    }

                    if (deadTimeSeconds % 40 == 0)
                    {
                        System.Media.SystemSounds.Beep.Play();
                    }
                });
            }
        }

        private void OnWorkTimerdEvent(object source, ElapsedEventArgs e)
        {

            double elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;


            TimeSpan timePassed = TimeSpan.FromSeconds(elapsedSeconds);
            TimeSpan timeLeft = TimeSpan.FromSeconds(targetSeconds - elapsedSeconds);


            this.Dispatcher.Invoke(() =>
            {

                if (elapsedSeconds >= targetSeconds)
                {
                    if (currentWindowState == WStates.WORKING)
                    {
                        SetWindowState(WStates.FINISHED_WORK);
                    }
                    if (currentWindowState == WStates.PAUSING || currentWindowState == WStates.PAUSING_LONG)
                    {
                        SetWindowState(WStates.FINISHED_PAUSE);
                    }
                }

                TextTimePassed = timePassed.ToString(@"mm\:ss");
                TextTimeLeft = timeLeft.ToString(@"mm\:ss");

                mainBar.Value = (elapsedSeconds / targetSeconds) * 100;

            });
        }


        private void ClearAlert()
        {
            getAttentionTimer.Stop();
            this.Dispatcher.Invoke(() =>
            {
                this.Icon = new BitmapImage(new Uri("pack://application:,,,/OpenPomodoro;component/img/tomato-icon.png"));
                this.Background = new SolidColorBrush(Color.FromArgb(255, 233, 236, 255));
            });
        }

        private void SetWindowState(int state)
        {
            CleanMenu();

            previouWindowState = currentWindowState;
            currentWindowState = state;

            switch (state)
            {
                case WStates.DEFAULT:
                    ClearAlert();
                    mainBar.Value = 0;
                    menuStartWork.Visibility = Visibility.Visible;
                    break;

                case WStates.WORKING:
                    ClearAlert();
                    changeTheme("red");
                    targetSeconds = SettingsSingleton.getInstance().getDurationWork(); ;
                    Pomodoros.Add(WORK_INPROGRESS);
                    startTime = DateTime.Now;
                    workTimer.Start();
                    menuCancelProgres.Visibility = Visibility.Visible;
                    menuForceCompleteProgres.Visibility = Visibility.Visible;
                    PomodoroDatabase.DBSingleton.getInstance().StartPomodoro();
                    break;

                case WStates.FINISHED_WORK:
                    Pomodoros.Remove(WORK_INPROGRESS);
                    Pomodoros.Add(WORK_COMPLETED);
                    SetWindowState(WStates.STOP);
                    PomodoroDatabase.DBSingleton.getInstance().CompletePomodoro();
                    break;

                case WStates.STOP: // = CANCEL
                    workTimer.Stop();
                    Pomodoros.Remove(WORK_INPROGRESS);
                    Pomodoros.Remove(PAUSE_IN_PROGRES);
                    System.Media.SystemSounds.Asterisk.Play();
                    System.Media.SystemSounds.Asterisk.Play();
                    SetWindowState(WStates.ALERTING);
                    break;

                case WStates.PAUSING:
                case WStates.PAUSING_LONG:
                    ClearAlert();
                    changeTheme("green");
                    Pomodoros.Add(PAUSE_IN_PROGRES);
                    startTime = DateTime.Now;
                    workTimer.Start();
                    menuCancelProgres.Visibility = Visibility.Visible;
                    menuForceCompleteProgres.Visibility = Visibility.Visible;
                    break;

                case WStates.FINISHED_PAUSE:
                    Pomodoros.Remove(PAUSE_IN_PROGRES);
                    if (previouWindowState == WStates.PAUSING_LONG)
                    {
                        Pomodoros.Add(PAUSE_COMPLETED);
                        Pomodoros.Add(PAUSE_COMPLETED);
                        Pomodoros.Add(PAUSE_COMPLETED);
                    }
                    else
                    {
                        Pomodoros.Add(PAUSE_COMPLETED);
                    }
                    SetWindowState(WStates.STOP);
                    break;

                case WStates.ALERTING:
                    changeTheme("blue");

                    menuStartShortPause.Visibility = Visibility.Visible;
                    menuStartLongPause.Visibility = Visibility.Visible;
                    menuStartWork.Visibility = Visibility.Visible;

                    deadTimeSeconds = 0;
                    getAttentionTimer.Start();

                    break;

            }
        }

        private void changeTheme(string newTheme)
        {
            var theme = ThemeManager.DetectAppStyle(Application.Current);
            var accent = ThemeManager.GetAccent(newTheme);
            ThemeManager.ChangeAppStyle(Application.Current, accent, theme.Item1);
        }

        private void CleanMenu()
        {
            menuStartWork.Visibility = Visibility.Collapsed;
            menuStartLongPause.Visibility = Visibility.Collapsed;
            menuStartShortPause.Visibility = Visibility.Collapsed;
            menuCancelProgres.Visibility = Visibility.Collapsed;
            menuForceCompleteProgres.Visibility = Visibility.Collapsed;

            menuSettings.Visibility = Visibility.Visible;
        }

        private void menuStartWork_Click(object sender, RoutedEventArgs e)
        {
            this.SetWindowState(WStates.WORKING);
        }

        private void menuCancelProgress_Click(object sender, RoutedEventArgs e)
        {
            this.SetWindowState(WStates.STOP);
        }

        private void menuStartShortPause_Click(object sender, RoutedEventArgs e)
        {
            targetSeconds = SettingsSingleton.getInstance().getDurationShortPause();
            this.SetWindowState(WStates.PAUSING);
        }

        private void menuStartLongPause_Click(object sender, RoutedEventArgs e)
        {
            targetSeconds = SettingsSingleton.getInstance().GetDurationLongPause();
            this.SetWindowState(WStates.PAUSING_LONG);
        }

        private void menuForceCompleteProgres_Click(object sender, RoutedEventArgs e)
        {
            if (currentWindowState == WStates.WORKING)
            {
                SetWindowState(WStates.FINISHED_WORK);
            }
            if (currentWindowState == WStates.PAUSING || currentWindowState == WStates.PAUSING_LONG)
            {
                SetWindowState(WStates.FINISHED_PAUSE);
            }
        }
    }
}
