using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KinectReframe
{
    public partial class MainWindow
    {
        private Window projectorWindow;
        private bool projectorHooksInstalled;
        private WindowStyle projectorPreviousStyle;
        private ResizeMode projectorPreviousResizeMode;
        private WindowState projectorPreviousState;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            ContentRendered += Projector_ContentRendered;
            PreviewKeyDown += MainWindow_ProjectorShortcut;
        }

        private void Projector_ContentRendered(object sender, EventArgs e)
        {
            if (projectorHooksInstalled)
            {
                return;
            }

            projectorHooksInstalled = true;
            InstallProjectorMenuItem();
        }

        private void MainWindow_ProjectorShortcut(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F9 && Keyboard.Modifiers == ModifierKeys.None)
            {
                OpenProjectorPreview();
                e.Handled = true;
            }
        }

        private void InstallProjectorMenuItem()
        {
            Grid root = Content as Grid;
            if (root == null)
            {
                return;
            }

            Menu menu = null;
            foreach (UIElement child in root.Children)
            {
                menu = child as Menu;
                if (menu != null)
                {
                    break;
                }
            }

            if (menu == null)
            {
                return;
            }

            foreach (object itemObject in menu.Items)
            {
                MenuItem item = itemObject as MenuItem;
                if (item == null || Convert.ToString(item.Header) != "_View")
                {
                    continue;
                }

                item.Items.Add(new Separator());
                MenuItem fullscreen = new MenuItem { Header = "Fullscreen preview\tF11" };
                fullscreen.Click += delegate { ToggleFullscreenPreview(); };
                MenuItem projector = new MenuItem { Header = "Open projector preview\tF9" };
                projector.Click += delegate { OpenProjectorPreview(); };
                item.Items.Add(fullscreen);
                item.Items.Add(projector);
                break;
            }
        }

        private void OpenProjectorPreview()
        {
            if (projectorWindow != null)
            {
                if (projectorWindow.WindowState == WindowState.Minimized)
                {
                    projectorWindow.WindowState = WindowState.Normal;
                }

                projectorWindow.Activate();
                return;
            }

            VisualBrush previewBrush = new VisualBrush(MainContentGrid)
            {
                Stretch = Stretch.Uniform,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };

            Rectangle preview = new Rectangle
            {
                Fill = previewBrush,
                Stretch = Stretch.Uniform,
                SnapsToDevicePixels = true
            };

            Grid layout = new Grid { Background = Brushes.Black };
            layout.Children.Add(preview);

            Border hint = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(210, 24, 26, 31)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(67, 72, 82)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(9, 5, 9, 5),
                Margin = new Thickness(12),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Child = new TextBlock
                {
                    Text = "F11 fullscreen  •  Esc close  •  double-click fullscreen",
                    Foreground = new SolidColorBrush(Color.FromRgb(190, 194, 201)),
                    FontSize = 11
                }
            };
            layout.Children.Add(hint);

            projectorWindow = new Window
            {
                Title = "Kinect Reframe — Projector Preview",
                Width = 1100,
                Height = 700,
                MinWidth = 640,
                MinHeight = 400,
                Background = Brushes.Black,
                Content = layout,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            projectorWindow.KeyDown += ProjectorWindow_KeyDown;
            projectorWindow.MouseDoubleClick += ProjectorWindow_MouseDoubleClick;
            projectorWindow.Closed += delegate
            {
                projectorWindow = null;
                StatusBarMessageText.Text = "Projector preview closed";
            };
            projectorWindow.Show();
            StatusBarMessageText.Text = "Projector preview open";
            ShowToast("Projector preview opened • F11 toggles fullscreen");
        }

        private void ProjectorWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                projectorWindow.Close();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F11)
            {
                ToggleProjectorFullscreen();
                e.Handled = true;
            }
        }

        private void ProjectorWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ToggleProjectorFullscreen();
            e.Handled = true;
        }

        private void ToggleProjectorFullscreen()
        {
            if (projectorWindow == null)
            {
                return;
            }

            if (projectorWindow.WindowStyle != WindowStyle.None)
            {
                projectorPreviousStyle = projectorWindow.WindowStyle;
                projectorPreviousResizeMode = projectorWindow.ResizeMode;
                projectorPreviousState = projectorWindow.WindowState;
                projectorWindow.WindowStyle = WindowStyle.None;
                projectorWindow.ResizeMode = ResizeMode.NoResize;
                projectorWindow.WindowState = WindowState.Maximized;
                return;
            }

            projectorWindow.WindowStyle = projectorPreviousStyle;
            projectorWindow.ResizeMode = projectorPreviousResizeMode;
            projectorWindow.WindowState = projectorPreviousState;
        }
    }
}
