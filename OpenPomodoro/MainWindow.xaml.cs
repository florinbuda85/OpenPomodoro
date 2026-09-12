using MahApps.Metro;
using MahApps.Metro.Controls;
using System;
using System.Windows;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PomodoroDatabase;
using System.Xml;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using ToastNotifications.Messages;
using System.Collections.Generic;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OpenPomodoro
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        int currentWindowState;
        int previouWindowState;

        Timer stateTimer;

        DateTime stateStartedAt;
        DateTime lastTickMinute = DateTime.MinValue;
        bool desperateAlertStarted;
        int targetSeconds = 0;

        WasapiOut tickerAudioOutput;
        MixingSampleProvider tickerAudioMixer;
        readonly List<AudioFileReader> tickerReaders = new List<AudioFileReader>();
        readonly Random soundRandom = new Random();
        WasapiOut completionAudioOutput;
        AudioFileReader completionAudioReader;

        const string WORK_INPROGRESS = "img/tomato-icon-gray.png";
        const string WORK_COMPLETED = "img/tomato-icon.png";

        const string PAUSE_IN_PROGRES = "img/lemon-icon.png";
        const string PAUSE_COMPLETED = "img/circle.png";

        Notifier notifier = new Notifier(cfg =>
        {
            cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: Application.Current.MainWindow,
                corner: Corner.BottomRight,
                offsetX: 0,
                offsetY: 0);

            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                notificationLifetime: TimeSpan.FromSeconds(11),
                maximumNotificationCount: MaximumNotificationCount.FromCount(5));

            cfg.Dispatcher = Application.Current.Dispatcher;
        });

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            stateTimer = new Timer();
            stateTimer.Interval = 900;
            stateTimer.Elapsed += new ElapsedEventHandler(OnStateTimerElapsed);

            this.SetWindowState(WStates.DEFAULT);

            Pomodoros = new ObservableCollection<string>();
            LoadCompletedSessionsForToday();


            // start working
            this.SetWindowState(WStates.WORKING);
            this.Loaded += MainWindow_Loaded;


        }

        private void LoadCompletedSessionsForToday()
        {
            var completedPomodoros = DBSingleton.getInstance().GetCompletedPomodoros(DateTime.Today)
                .Select(pomodoro => new
                {
                    StartDate = pomodoro.StartDate,
                    IsPause = false,
                    IsLongPause = false
                });
            var completedPauses = DBSingleton.getInstance().GetCompletedPauses(DateTime.Today)
                .Select(pause => new
                {
                    StartDate = pause.StartDate == DateTime.MinValue ? pause.EndDate : pause.StartDate,
                    IsPause = true,
                    IsLongPause = pause.IsLong
                });

            foreach (var session in completedPomodoros.Concat(completedPauses).OrderBy(session => session.StartDate))
            {
                if (session.IsPause)
                {
                    AddCompletedPauseIcons(session.IsLongPause);
                }
                else
                {
                    Pomodoros.Add(WORK_COMPLETED);
                }
            }
        }

        private void AddCompletedPauseIcons(bool isLongPause)
        {
            int iconCount = isLongPause ? 3 : 1;
            for (int i = 0; i < iconCount; i++)
            {
                Pomodoros.Add(PAUSE_COMPLETED);
            }
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
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
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

        private void OnStateTimerElapsed(object source, ElapsedEventArgs e)
        {
            DateTime currentTime = DateTime.Now;
            int elapsedSeconds = Math.Max(0, (int)(currentTime - stateStartedAt).TotalSeconds);
            int state = currentWindowState;

            DateTime currentMinute = new DateTime(
                currentTime.Year,
                currentTime.Month,
                currentTime.Day,
                currentTime.Hour,
                currentTime.Minute,
                0);
            if (currentTime.Second == 0
                && currentMinute != lastTickMinute
                && SettingsSingleton.getInstance().isTickerEnabled())
            {
                lastTickMinute = currentMinute;

                if (state == WStates.WORKING)
                {
                    PlayTickerSound("tick_pomodoro");
                }
                else if (state == WStates.PAUSING || state == WStates.PAUSING_LONG)
                {
                    PlayTickerSound("tick_pause");
                }
                else if (state == WStates.ALERTING)
                {
                    PlayTickerSound("tick_alert");
                }
            }

            if (state == WStates.ALERTING)
            {
                UpdateAlert(elapsedSeconds);
                return;
            }

            if (state != WStates.WORKING && state != WStates.PAUSING && state != WStates.PAUSING_LONG)
            {
                return;
            }

            TimeSpan timePassed = TimeSpan.FromSeconds(elapsedSeconds);
            TimeSpan timeLeft = TimeSpan.FromSeconds(Math.Max(0, targetSeconds - elapsedSeconds));

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

                mainBar.Value = ((double)elapsedSeconds / targetSeconds) * 100;
            });
        }

        private void UpdateAlert(int attentionSeconds)
        {
            int desperateAlertThreshold = SettingsSingleton.getInstance().getSecondsUntilDesperateAlert();
            if (attentionSeconds < desperateAlertThreshold)
            {
                return;
            }

            this.Dispatcher.Invoke(() =>
            {
                if (!desperateAlertStarted)
                {
                    desperateAlertStarted = true;
                    MinimizeAllWindows();
                }

                this.Icon = new BitmapImage(new Uri(
                    attentionSeconds % 2 == 0
                        ? "pack://application:,,,/OpenPomodoro;component/img/attention-icon.png"
                        : "pack://application:,,,/OpenPomodoro;component/img/tomato-icon.png"));

                this.Background = new SolidColorBrush(
                    attentionSeconds % 10 == 0
                        ? Color.FromArgb(255, 0, 255, 0)
                        : Color.FromArgb(255, 233, 236, 255));
            });
        }


        private void ClearAlert()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.Icon = new BitmapImage(new Uri("pack://application:,,,/OpenPomodoro;component/img/tomato-icon.png"));
                this.Background = new SolidColorBrush(Color.FromArgb(255, 233, 236, 255));
            });
        }

        private static void MinimizeAllWindows()
        {
            Type shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType == null)
            {
                return;
            }

            object shell = null;
            try
            {
                shell = Activator.CreateInstance(shellType);
                shellType.InvokeMember(
                    "MinimizeAll",
                    BindingFlags.InvokeMethod,
                    null,
                    shell,
                    null);
            }
            catch (COMException)
            {
                // Alerting must continue even if Windows Shell cannot minimize the desktop.
            }
            finally
            {
                if (shell != null && Marshal.IsComObject(shell))
                {
                    Marshal.FinalReleaseComObject(shell);
                }
            }
        }

        private void SetWindowState(int state)
        {
            CleanMenu();

            previouWindowState = currentWindowState;
            currentWindowState = state;

            switch (state)
            {
                case WStates.DEFAULT:
                    stateTimer.Stop();
                    ClearAlert();
                    mainBar.Value = 0;
                    menuStartWork.Visibility = Visibility.Visible;
                    break;

                case WStates.WORKING:
                    ClearAlert();
                    ChangeTheme("red");
                    targetSeconds = SettingsSingleton.getInstance().getDurationWork(); ;
                    Pomodoros.Add(WORK_INPROGRESS);
                    menuCancelProgres.Visibility = Visibility.Visible;
                    menuForceCompleteProgres.Visibility = Visibility.Visible;
                    StartWorkSession();
                    break;

                case WStates.FINISHED_WORK:
                    Pomodoros.Remove(WORK_INPROGRESS);
                    Pomodoros.Add(WORK_COMPLETED);
                    PomodoroDatabase.DBSingleton.getInstance().CompletePomodoro();
                    PlayCompletionSound("pomodoro_end");
                    SetWindowState(WStates.STOP);
                    break;

                case WStates.STOP: // = CANCEL
                    stateTimer.Stop();
                    StopTickerAudioSession();
                    DBSingleton.getInstance().CancelPomodoro();
                    DBSingleton.getInstance().CancelPause();
                    Pomodoros.Remove(WORK_INPROGRESS);
                    Pomodoros.Remove(PAUSE_IN_PROGRES);
                    SetWindowState(WStates.ALERTING);
                    break;

                case WStates.PAUSING:
                case WStates.PAUSING_LONG:
                    ClearAlert();
                    ChangeTheme("green");
                    Pomodoros.Add(PAUSE_IN_PROGRES);
                    DBSingleton.getInstance().StartPause();
                    StartStateTimer();
                    menuCancelProgres.Visibility = Visibility.Visible;
                    menuForceCompleteProgres.Visibility = Visibility.Visible;
                    TryShowPauseAdvice();
                    break;

                case WStates.FINISHED_PAUSE:
                    Pomodoros.Remove(PAUSE_IN_PROGRES);
                    bool completedLongPause = previouWindowState == WStates.PAUSING_LONG;
                    DBSingleton.getInstance().RecordCompletedPause(completedLongPause);
                    AddCompletedPauseIcons(completedLongPause);
                    PlayCompletionSound("pause_end");
                    SetWindowState(WStates.STOP);
                    break;

                case WStates.ALERTING:
                    ChangeTheme("blue");

                    menuStartShortPause.Visibility = Visibility.Visible;
                    menuStartLongPause.Visibility = Visibility.Visible;
                    menuStartWork.Visibility = Visibility.Visible;

                    StartStateTimer();

                    break;

            }
        }

        private void ShowSuccessWhenWindowIsReady(string message)
        {
            if (IsLoaded)
            {
                notifier.ShowSuccess(message);
                return;
            }

            Dispatcher.BeginInvoke(
                new Action(() => notifier.ShowSuccess(message)),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void PlayCompletionSound(string soundFolder)
        {
            string[] soundFiles = GetSoundFiles(soundFolder);

            if (soundFiles.Length == 0)
            {
                return;
            }

            try
            {
                CloseCompletionAudio();

                completionAudioReader = new AudioFileReader(
                    soundFiles[soundRandom.Next(soundFiles.Length)]);
                completionAudioOutput = new WasapiOut(
                    AudioClientShareMode.Shared,
                    true,
                    100);
                completionAudioOutput.PlaybackStopped += CompletionAudioOutput_PlaybackStopped;
                completionAudioOutput.Init(completionAudioReader);
                completionAudioOutput.Play();
            }
            catch
            {
                CloseCompletionAudio();
            }
        }

        private void CompletionAudioOutput_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(CloseCompletionAudio));
        }

        private void CloseCompletionAudio()
        {
            if (completionAudioOutput != null)
            {
                completionAudioOutput.PlaybackStopped -= CompletionAudioOutput_PlaybackStopped;
                completionAudioOutput.Stop();
                completionAudioOutput.Dispose();
                completionAudioOutput = null;
            }

            completionAudioReader?.Dispose();
            completionAudioReader = null;
        }

        private void PlayTickerSound(string soundFolder)
        {
            string[] soundFiles = GetSoundFiles(soundFolder);
            if (soundFiles.Length == 0)
            {
                return;
            }

            try
            {
                StartTickerAudioSession();
                if (tickerAudioMixer == null)
                {
                    return;
                }

                tickerReaders.RemoveAll(reader =>
                {
                    if (reader.Position < reader.Length)
                    {
                        return false;
                    }
                    reader.Dispose();
                    return true;
                });

                string tickerPath = soundFiles[soundRandom.Next(soundFiles.Length)];
                AudioFileReader tickerReader = new AudioFileReader(tickerPath)
                {
                    Volume = SettingsSingleton.getInstance().getTickerVolume() / 100f
                };
                tickerReaders.Add(tickerReader);
                tickerAudioMixer.AddMixerInput(ConvertToTickerMixerFormat(tickerReader));
            }
            catch
            {
                StopTickerAudioSession();
            }
        }

        private static string[] GetSoundFiles(string soundFolder)
        {
            try
            {
                string folderPath = Path.Combine(@"D:\pers\sounds", soundFolder);
                if (!Directory.Exists(folderPath))
                {
                    return new string[0];
                }

                return Directory.GetFiles(folderPath)
                    .Where(path => Path.GetExtension(path)
                        .Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }
            catch
            {
                return new string[0];
            }
        }

        private ISampleProvider ConvertToTickerMixerFormat(ISampleProvider input)
        {
            ISampleProvider convertedInput = input;

            if (convertedInput.WaveFormat.Channels == 1)
            {
                convertedInput = new MonoToStereoSampleProvider(convertedInput);
            }
            else if (convertedInput.WaveFormat.Channels != tickerAudioMixer.WaveFormat.Channels)
            {
                throw new NotSupportedException(
                    $"Audio with {convertedInput.WaveFormat.Channels} channels is not supported.");
            }

            if (convertedInput.WaveFormat.SampleRate != tickerAudioMixer.WaveFormat.SampleRate)
            {
                convertedInput = new WdlResamplingSampleProvider(
                    convertedInput,
                    tickerAudioMixer.WaveFormat.SampleRate);
            }

            return convertedInput;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= MainWindow_Loaded;

            if (!SettingsSingleton.getInstance().isTickerEnabled())
            {
                return;
            }

            await Task.Delay(750);

            PlayTickerSound("tick_pomodoro");
        }

        private void StartWorkSession()
        {
            StartStateTimer();

            int? secondsBetweenPomodoros = DBSingleton.getInstance().StartPomodoro();
            if (secondsBetweenPomodoros.HasValue)
            {
                int minutesBetweenPomodoros = (int)Math.Round(
                    secondsBetweenPomodoros.Value / 60.0,
                    MidpointRounding.AwayFromZero);
                string minuteLabel = minutesBetweenPomodoros == 1 ? "minute" : "minutes";
                ShowSuccessWhenWindowIsReady($"{minutesBetweenPomodoros} {minuteLabel} from last finished pomodoro");
            }
        }

        private void StartStateTimer()
        {
            stateStartedAt = DateTime.Now;
            lastTickMinute = DateTime.MinValue;
            desperateAlertStarted = false;
            stateTimer.Start();
        }

        private void StartTickerAudioSession()
        {
            if (tickerAudioOutput != null)
            {
                return;
            }

            tickerAudioMixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
            {
                ReadFully = true
            };
            tickerAudioOutput = new WasapiOut(AudioClientShareMode.Shared, true, 100);
            tickerAudioOutput.Init(tickerAudioMixer);
            tickerAudioOutput.Play();
        }

        private void StopTickerAudioSession()
        {
            tickerAudioOutput?.Stop();
            tickerAudioOutput?.Dispose();
            tickerAudioOutput = null;
            tickerAudioMixer = null;
            tickerReaders.ForEach(reader => reader.Dispose());
            tickerReaders.Clear();
        }

        private void ChangeTheme(string newTheme)
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

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;

            SettingsView view = new SettingsView
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            view.ShowDialog();

            this.Topmost = true;
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        private void MenuChart_Click(object sender, RoutedEventArgs e)
        {

            Tuple<string, string> chartData = PomodoroDatabase.DBSingleton.getInstance().GetChartData(DateTime.Today);

            ChartGeneratior.ChartHelper.ShowChart(chartData);
        }

        private void MenuPauseAdvices_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;

            PauseAdvices view = new PauseAdvices
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            view.ShowDialog();

            this.Topmost = true;
        }

        private void MenuAddPauseReminder_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;

            AddPauseReminder view = new AddPauseReminder
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            view.ShowDialog();

            this.Topmost = true;
        }

        private void TryShowPauseAdvice()
        {
            try
            {
                if (SettingsSingleton.getInstance().shouldDisplayPauseAdvices())
                {
                    if (File.Exists("DefaultPauseAdvices.txt"))
                    {
                        var lines = File.ReadAllLines("DefaultPauseAdvices.txt");
                        if (lines != null && lines.Length > 0)
                        {
                            lines.Where(x => x.Length > 0).ToList()
                                .ForEach(x => DBSingleton.getInstance().InsertAdvice(x));
                        }
                        File.WriteAllText("DefaultPauseAdvices.txt", "");
                    }

                    PauseAdvice reminder = DBSingleton.getInstance().GetNextPauseReminder();
                    if (reminder == null)
                    {
                        MessageBox.Show(
                            this,
                            "Add pause reminders or deactivate the setting.",
                            "Pause reminders",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else if (reminder.IsOnce)
                    {
                        ReminderDialog dialog = new ReminderDialog(
                            reminder.Content,
                            true)
                        {
                            Owner = this
                        };
                        dialog.ShowDialog();
                        if (dialog.Accepted)
                        {
                            DBSingleton.getInstance().CompleteAdvice(reminder.id);
                        }
                    }
                    else
                    {
                        ReminderDialog dialog = new ReminderDialog(reminder.Content, false)
                        {
                            Owner = this
                        };
                        dialog.ShowDialog();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error on TryShowPauseAdvice: {e.Message}");
            }
        }

    }
}
