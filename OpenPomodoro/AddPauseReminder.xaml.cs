using MahApps.Metro.Controls;
using PomodoroDatabase;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenPomodoro
{
    public partial class AddPauseReminder : MetroWindow
    {
        public AddPauseReminder()
        {
            InitializeComponent();
            Loaded += (sender, args) =>
            {
                ReminderText.Focus();
                Keyboard.Focus(ReminderText);
            };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ReminderText.Text))
            {
                MessageBox.Show("Enter a pause reminder.", "Add Pause Reminder");
                return;
            }

            ComboBoxItem selectedType = ReminderType.SelectedItem as ComboBoxItem;
            string type = selectedType?.Content?.ToString() ?? "Permanent";
            DBSingleton.getInstance().InsertAdvice(ReminderText.Text, type);
            DialogResult = true;
        }
    }
}
