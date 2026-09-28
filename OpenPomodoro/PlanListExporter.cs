using PomodoroDatabase;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenPomodoro
{
    internal static class PlanListExporter
    {
        private const string PlanListFileName = "planlist.txt";

        public static void ExportDay(DateTime date)
        {
            Settings settings = SettingsSingleton.getInstance().GetSettings();
            if (!settings.PlanExportEnabled)
            {
                return;
            }

            string dayFolder = GetDayFolder(date);
            Directory.CreateDirectory(dayFolder);

            DBSingleton database = DBSingleton.getInstance();
            string[] lines = database.GetCompletedPomodoros(date)
                .Select(pomodoro =>
                {
                    PomodoroPlan plan = pomodoro.PlanId.HasValue
                        ? database.GetPomodoroPlan(pomodoro.PlanId.Value)
                        : null;
                    string planText = plan?.Content ?? "[deleted plan]";
                    planText = planText.Replace('\r', ' ').Replace('\n', ' ');
                    return $"<{planText}, {pomodoro.StartDate:HH:mm:ss}, {pomodoro.EndDate:HH:mm:ss}>";
                })
                .ToArray();

            File.WriteAllLines(
                Path.Combine(dayFolder, PlanListFileName),
                lines,
                new UTF8Encoding(false));
        }

        public static void OpenCurrentDayFolder()
        {
            string dayFolder = GetDayFolder(DateTime.Today);
            Directory.CreateDirectory(dayFolder);
            Process.Start(new ProcessStartInfo
            {
                FileName = dayFolder,
                UseShellExecute = true
            });
        }

        private static string GetDayFolder(DateTime date)
        {
            string rootFolder = SettingsSingleton.getInstance().GetSettings().PlanExportRoot;
            if (string.IsNullOrWhiteSpace(rootFolder))
            {
                rootFolder = AppDomain.CurrentDomain.BaseDirectory;
            }
            else
            {
                rootFolder = Environment.ExpandEnvironmentVariables(rootFolder.Trim());
                if (!Path.IsPathRooted(rootFolder))
                {
                    rootFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rootFolder);
                }
            }

            return Path.Combine(
                Path.GetFullPath(rootFolder),
                date.ToString("MM"),
                date.ToString("dd"));
        }
    }
}
