

using MahApps.Metro.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        int previousWindowState;

        Timer getAttentionTimer;
        Timer workTimer;

        DateTime startTime;
        int targetSeconds = 0;

        string WORK_INPROGRESS = "img/tomato-icon-gray.png";
        string WORK_COMPLETED = "img/tomato-icon.png";

        string PAUSE_IN_PROGRES = "img/lemon-icon.png";
        string PAUSE_COMPLETED = "img/circle.png";



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



        int deadTimeSeconds = 0; //todo: get rid of this..

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
                        System.Media.SystemSounds.Beep.Play();
                    }
                    else
                    {
                        this.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
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
                    if (currentWindowState == WStates.PAUSING)
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
                this.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            });
        }

        private void SetWindowState(int state)
        {
            CleanMenu();

            previousWindowState = currentWindowState;
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
                    targetSeconds = SettingsSingleton.getInstance().getDurationWork(); ;
                    Pomodoros.Add(WORK_INPROGRESS);
                    startTime = DateTime.Now;
                    workTimer.Start();
                    menuCancelWork.Visibility = Visibility.Visible;
                    break;

                case WStates.FINISHED_WORK:
                    Pomodoros.Remove(WORK_INPROGRESS);
                    Pomodoros.Add(WORK_COMPLETED);
                    workTimer.Stop();
                    System.Media.SystemSounds.Asterisk.Play();
                    SetWindowState(WStates.ALERTING);
                    break;

                case WStates.PAUSING:
                    ClearAlert();
                    Pomodoros.Add(PAUSE_IN_PROGRES);
                    startTime = DateTime.Now;
                    workTimer.Start();
                    break;

                case WStates.FINISHED_PAUSE:
                    Pomodoros.Remove(PAUSE_IN_PROGRES);
                    Pomodoros.Add(PAUSE_COMPLETED);
                    workTimer.Stop();
                    System.Media.SystemSounds.Asterisk.Play();
                    SetWindowState(WStates.ALERTING);
                    break;

                case WStates.ALERTING:
                    if (previousWindowState == WStates.FINISHED_WORK)
                    {
                        menuStartShortPause.Visibility = Visibility.Visible;
                        menuStartLongPause.Visibility = Visibility.Visible;
                    }
                    if (previousWindowState == WStates.FINISHED_PAUSE)
                    {
                        menuStartWork.Visibility = Visibility.Visible;
                    }
                    deadTimeSeconds = 0;
                    getAttentionTimer.Start();

                    break;

            }
        }

        private void CleanMenu()
        {
            menuStartWork.Visibility = Visibility.Collapsed;
            menuStartLongPause.Visibility = Visibility.Collapsed;
            menuStartShortPause.Visibility = Visibility.Collapsed;
            menuCancelWork.Visibility = Visibility.Collapsed;

            menuSettings.Visibility = Visibility.Visible;
        }

        private void menuStartWork_Click(object sender, RoutedEventArgs e)
        {
            this.SetWindowState(WStates.WORKING);
        }

        private void menuCancelWork_Click(object sender, RoutedEventArgs e)
        {
            //this.SetWindowState(WStates.WORKING);
        }

        private void menuStartShortPause_Click(object sender, RoutedEventArgs e)
        {
            targetSeconds = SettingsSingleton.getInstance().getDurationShortPause();
            this.SetWindowState(WStates.PAUSING);
        }
    }
}
