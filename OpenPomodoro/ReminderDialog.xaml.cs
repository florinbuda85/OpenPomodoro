using MahApps.Metro.Controls;
using System.Windows;

namespace OpenPomodoro
{
    public partial class ReminderDialog : MetroWindow
    {
        public bool Accepted { get; private set; }

        public ReminderDialog(string content, bool isOnce)
        {
            InitializeComponent();
            ReminderContent.Text = "Reminder: " + content;
            Title = isOnce ? "One-time pause reminder" : "Pause reminder";
            YesButton.Visibility = isOnce ? Visibility.Visible : Visibility.Collapsed;
            NoButton.Visibility = isOnce ? Visibility.Visible : Visibility.Collapsed;
            OkButton.Visibility = isOnce ? Visibility.Collapsed : Visibility.Visible;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Accepted = true;
            DialogResult = true;
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Accepted = true;
            DialogResult = true;
        }
    }
}
