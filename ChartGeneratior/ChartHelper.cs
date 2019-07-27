using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ChartGeneratior
{
    public class ChartHelper
    {

        private static readonly string chartLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PomodoroChart.html");

        public static void ShowChart(Tuple<string, string> chartData)
        {
            WriteFiles(chartLocation, chartData);

            OpenBrowser(chartLocation);
        }

        private static void WriteFiles(string targetPath, Tuple<string, string> chartData)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("PomodoroChart.html"));
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string content = reader.ReadToEnd();

                content = content.Replace("{calendarDates}", chartData.Item1);
                content = content.Replace("{pomodoroCounts}", chartData.Item2);
                File.WriteAllText(targetPath, content);
            }
        }

        private static void OpenBrowser(string chartLocation)
        {
            System.Diagnostics.Process.Start(chartLocation);
        }
        
    }
}
