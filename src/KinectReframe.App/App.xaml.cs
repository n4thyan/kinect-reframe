using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

            Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(delegate
                {
                    if (MainWindow != null)
                    {
                        MainWindow.Icon = CreateWindowIcon();
                    }
                }));
        }

        internal static ImageSource CreateWindowIcon()
        {
            const int size = 64;
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                context.DrawRoundedRectangle(
                    new SolidColorBrush(Color.FromRgb(32, 34, 40)),
                    new Pen(new SolidColorBrush(Color.FromRgb(76, 141, 255)), 3),
                    new Rect(2, 2, 60, 60),
                    10,
                    10);

                context.DrawRoundedRectangle(
                    new SolidColorBrush(Color.FromRgb(10, 12, 16)),
                    new Pen(new SolidColorBrush(Color.FromRgb(99, 105, 116)), 1.5),
                    new Rect(10, 22, 44, 20),
                    6,
                    6);

                context.DrawEllipse(
                    new SolidColorBrush(Color.FromRgb(32, 59, 96)),
                    new Pen(new SolidColorBrush(Color.FromRgb(76, 141, 255)), 2),
                    new Point(32, 32),
                    8,
                    8);
                context.DrawEllipse(
                    new SolidColorBrush(Color.FromRgb(104, 166, 255)),
                    null,
                    new Point(32, 32),
                    3,
                    3);

                SolidColorBrush sensorBrush = new SolidColorBrush(Color.FromRgb(56, 201, 154));
                context.DrawEllipse(sensorBrush, null, new Point(16, 32), 2.5, 2.5);
                context.DrawEllipse(sensorBrush, null, new Point(22, 32), 2.5, 2.5);
                context.DrawEllipse(sensorBrush, null, new Point(42, 32), 2.5, 2.5);
                context.DrawEllipse(sensorBrush, null, new Point(48, 32), 2.5, 2.5);

                context.DrawRectangle(
                    new SolidColorBrush(Color.FromRgb(76, 141, 255)),
                    null,
                    new Rect(14, 15, 36, 2));
            }

            RenderTargetBitmap bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
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
