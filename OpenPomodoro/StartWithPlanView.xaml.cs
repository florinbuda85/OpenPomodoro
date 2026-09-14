using MahApps.Metro.Controls;
using PomodoroDatabase;
using System.Windows;
using System.Windows.Input;
using System.Linq;

namespace OpenPomodoro
{
    public partial class StartWithPlanView : MetroWindow
    {
        public string PlanText { get; private set; }
        public int? SelectedPlanId { get; private set; }

        public StartWithPlanView()
        {
            InitializeComponent();
            PlanInput.ItemsSource = DBSingleton.getInstance().GetAllPomodoroPlans();
            Loaded += (sender, args) =>
            {
                PlanInput.Focus();
                Keyboard.Focus(PlanInput);
            };
        }

        public StartWithPlanView(string title, string initialText) : this()
        {
            Title = title;
            PlanInput.Text = initialText ?? string.Empty;
            PlanInput.SelectedItem = PlanInput.Items
                .OfType<PomodoroPlan>()
                .FirstOrDefault(plan => plan.Content == PlanInput.Text);
            ConfirmButton.Content = "Save";
        }

        private void StartPomodoro_Click(object sender, RoutedEventArgs e)
        {
            string plan = PlanInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(plan))
            {
                MessageBox.Show(
                    this,
                    "Enter a short plan for this Pomodoro.",
                    Title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                PlanInput.Focus();
                return;
            }

            PlanText = plan;
            PomodoroPlan selectedPlan = PlanInput.SelectedItem as PomodoroPlan;
            SelectedPlanId = selectedPlan != null && selectedPlan.Content == plan
                ? selectedPlan.id : (int?)null;
            DialogResult = true;
        }
    }
}
