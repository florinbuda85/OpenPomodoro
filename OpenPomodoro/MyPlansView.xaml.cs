using MahApps.Metro.Controls;
using PomodoroDatabase;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenPomodoro
{
    public partial class MyPlansView : MetroWindow
    {
        public PomodoroPlan PlanToStart { get; private set; }
        public event EventHandler PlansChanged;

        public MyPlansView()
        {
            InitializeComponent();
            try
            {
                RefreshPlans();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "Could not load plans.\n\n" + exception.Message,
                    "My plans", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RefreshPlans()
        {
            int? selectedId = (PlansGrid.SelectedItem as PomodoroPlan)?.id;
            var plans = DBSingleton.getInstance().GetAllPomodoroPlans();
            PlansGrid.ItemsSource = plans;
            PlansGrid.SelectedItem = plans.FirstOrDefault(plan => plan.id == selectedId);
        }

        private PomodoroPlan GetSelectedPlan(object sender)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                var contextMenu = menuItem.Parent as ContextMenu;
                var row = contextMenu?.PlacementTarget as DataGridRow;
                return row?.DataContext as PomodoroPlan;
            }
            return PlansGrid.SelectedItem as PomodoroPlan;
        }

        private void AddPlan_Click(object sender, RoutedEventArgs e)
        {
            EditPlan(null);
        }

        private void EditPlan_Click(object sender, RoutedEventArgs e)
        {
            var plan = GetSelectedPlan(sender);
            if (plan != null)
            {
                EditPlan(plan);
            }
        }

        private void EditPlan(PomodoroPlan plan)
        {
            var editor = new StartWithPlanView(plan == null ? "Add plan" : "Edit plan", plan?.Content)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (editor.ShowDialog() != true)
            {
                return;
            }

            var database = DBSingleton.getInstance();
            if (plan == null)
            {
                database.CreatePomodoroPlan(editor.PlanText);
            }
            else
            {
                database.UpdatePomodoroPlan(plan.id, editor.PlanText);
            }
            try
            {
                RefreshPlans();
                PlansChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "Could not save the plan.\n\n" + exception.Message,
                    "My plans", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeletePlan_Click(object sender, RoutedEventArgs e)
        {
            var plan = GetSelectedPlan(sender);
            if (plan == null || MessageBox.Show(this,
                $"Delete this plan?\n\n{plan.Content}\n\nPomodoro sessions will remain in your history.",
                "Delete plan", MessageBoxButton.YesNo, MessageBoxImage.Question,
                MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                DBSingleton.getInstance().DeletePomodoroPlan(plan.id);
                RefreshPlans();
                PlansChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "Could not delete the plan.\n\n" + exception.Message,
                    "My plans", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartPlan_Click(object sender, RoutedEventArgs e)
        {
            var plan = GetSelectedPlan(sender);
            if (plan != null)
            {
                PlanToStart = plan;
                DialogResult = true;
            }
        }

        private void PlanRow_RightClick(object sender, MouseButtonEventArgs e)
        {
            var row = (DataGridRow)sender;
            row.IsSelected = true;
            row.Focus();
        }

        private void PlansGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EditButton.IsEnabled = DeleteButton.IsEnabled = PlansGrid.SelectedItem != null;
        }
    }
}
