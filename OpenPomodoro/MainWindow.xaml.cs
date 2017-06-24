

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

        Timer getAttentionTimer;
        Timer workTimer;

        DateTime startTime;
        int targetSeconds = 0;

        string GRAY_POMODORO = "img/tomato-icon-gray.png";
        string NORMAL_POMODORO = "img/tomato-icon.png";

        string GRAY_LEMON = "img/lemon-icon-gray.png";
        string NORMAL_LEMON = "img/lemon-icon.png";

        int targetWorkSeconds = 25 * 60;






        public MainWindow()
        {
            InitializeComponent();

            getAttentionTimer = new Timer();
            getAttentionTimer.Interval = 200;
            //getAttentionTimer.Start();
            getAttentionTimer.Elapsed += new ElapsedEventHandler(OnAtentionTimerdEvent);


            this.DataContext = this;

            workTimer = new Timer();
            workTimer.Interval = 1000;
            workTimer.Elapsed += new ElapsedEventHandler(OnWorkTimerdEvent);

            this.SetWindowState(WStates.DEFAULT);


            Pomodoros = new ObservableCollection<string>();
            /*
            Pomodoros.Add(GRAY_POMODORO);
            Pomodoros.Add(NORMAL_POMODORO);
            Pomodoros.Add(GRAY_LEMON);
            Pomodoros.Add(NORMAL_LEMON);
            */

        }



        int i = 0;

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

            this.Dispatcher.Invoke(() =>
            {
                if (++i % 2 == 0)
                {
                    this.Icon = new BitmapImage(new Uri("pack://application:,,,/OpenPomodoro;component/img/attention-icon.png"));
                }
                else
                {
                    this.Icon = new BitmapImage(new Uri("pack://application:,,,/OpenPomodoro;component/img/tomato-icon.png"));
                }

                if (i % 10 == 0)
                {
                    this.Background = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                }
                else
                {
                    this.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                }
            });
        }

        private void OnWorkTimerdEvent(object source, ElapsedEventArgs e)
        {

            double seconds = (DateTime.Now - startTime).TotalSeconds;
            

            this.Dispatcher.Invoke(() =>
            {
                if (seconds >= targetSeconds)
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

                mainBar.Value = (seconds / targetSeconds) * 100;

            });
        }


        private void ClearAlert()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.Icon = new BitmapImage(new Uri("pack://application:,,,/OpenPomodoro;component/img/tomato-icon.png"));
                this.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            });
        }

        private void SetWindowState(int state)
        {
            CleanMenu();

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
                    Pomodoros.Add(GRAY_POMODORO);
                    startTime = DateTime.Now;
                    workTimer.Start();
                    menuCancelWork.Visibility = Visibility.Visible;
                    break;

                case WStates.FINISHED_WORK:
                    Pomodoros.Remove(GRAY_POMODORO);
                    Pomodoros.Add(NORMAL_POMODORO);

                    workTimer.Stop();
                    menuStartShortPause.Visibility = Visibility.Visible;
                    menuStartLongPause.Visibility = Visibility.Visible;
                    break;

                case WStates.PAUSING:
                    ClearAlert();
                    Pomodoros.Add(GRAY_LEMON);
                    startTime = DateTime.Now;
                    workTimer.Start();
                    //menuCancelWork.Visibility = Visibility.Visible;
                    break;

                case WStates.FINISHED_PAUSE:
                    Pomodoros.Remove(GRAY_LEMON);
                    Pomodoros.Add(NORMAL_LEMON);
                    workTimer.Stop();
                    menuStartWork.Visibility = Visibility.Visible;
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
            targetSeconds = targetWorkSeconds;
            this.SetWindowState(WStates.WORKING);
        }

        private void menuCancelWork_Click(object sender, RoutedEventArgs e)
        {
            //this.SetWindowState(WStates.WORKING);
        }

        private void menuStartShortPause_Click(object sender, RoutedEventArgs e)
        {
            targetSeconds = 60 * 5;
            this.SetWindowState(WStates.PAUSING);
        }
    }
}
