using System.Windows;

namespace KinectReframe
{
    public partial class MainWindow
    {
        // The original prototype named its root Grid RootVisual. The studio redesign
        // replaced that XAML structure, while the application-snapshot command still
        // expects a FrameworkElement representing the complete window contents.
        private FrameworkElement RootVisual
        {
            get { return Content as FrameworkElement; }
        }
    }
}
