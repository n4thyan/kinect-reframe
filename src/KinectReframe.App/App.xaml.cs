using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace KinectReframe
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            string path = WriteCrashLog(e.Exception, "UI thread");
            MessageBox.Show(
                "Kinect Reframe encountered an unexpected error and could not continue this action.\n\n" +
                e.Exception.Message + "\n\n" +
                "A diagnostic log was written to:\n" + path,
                "Kinect Reframe",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = e.ExceptionObject as Exception;
            WriteCrashLog(exception ?? new Exception(Convert.ToString(e.ExceptionObject)), "Background thread");
        }

        private static string WriteCrashLog(Exception exception, string source)
        {
            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KinectReframe",
                    "Logs");
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, "crash-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");

                StringBuilder log = new StringBuilder();
                log.AppendLine("Kinect Reframe crash report");
                log.AppendLine("Time: " + DateTime.Now.ToString("O"));
                log.AppendLine("Source: " + source);
                log.AppendLine("OS: " + Environment.OSVersion);
                log.AppendLine("CLR: " + Environment.Version);
                log.AppendLine("64-bit OS: " + Environment.Is64BitOperatingSystem);
                log.AppendLine("64-bit process: " + Environment.Is64BitProcess);
                log.AppendLine();
                log.AppendLine(exception == null ? "No exception details available." : exception.ToString());
                File.WriteAllText(path, log.ToString(), Encoding.UTF8);
                return path;
            }
            catch
            {
                return "The diagnostic log could not be written.";
            }
        }
    }
}
