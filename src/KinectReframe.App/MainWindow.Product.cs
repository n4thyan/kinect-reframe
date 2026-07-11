using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using KinectReframe.Services;

namespace KinectReframe
{
    public partial class MainWindow
    {
        private AppSettings loadedSettings;
        private bool productShellReady;
        private bool fullscreenPreview;
        private WindowStyle previousWindowStyle;
        private ResizeMode previousResizeMode;
        private WindowState previousWindowState;
        private GridLength[] previousRootRows;
        private GridLength[] previousWorkspaceColumns;
        private Grid workspaceGrid;
        private Border toastBorder;
        private TextBlock toastText;
        private DispatcherTimer toastTimer;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            LoadDesktopSettings();
            ConfigureWindowsShell();
            Closing += ProductWindow_Closing;
            ContentRendered += ProductWindow_ContentRendered;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.Key == Key.Escape && fullscreenPreview)
            {
                ToggleFullscreenPreview();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F11)
            {
                ToggleFullscreenPreview();
                e.Handled = true;
                return;
            }

            ModifierKeys modifiers = Keyboard.Modifiers;
            if (modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.D1:
                    case Key.NumPad1:
                        SelectPreviewMode(StudioPreviewMode.Camera);
                        e.Handled = true;
                        return;
                    case Key.D2:
                    case Key.NumPad2:
                        SelectPreviewMode(StudioPreviewMode.Depth);
                        e.Handled = true;
                        return;
                    case Key.D3:
                    case Key.NumPad3:
                        SelectPreviewMode(StudioPreviewMode.PointCloud);
                        e.Handled = true;
                        return;
                    case Key.D4:
                    case Key.NumPad4:
                        SelectPreviewMode(StudioPreviewMode.Split);
                        e.Handled = true;
                        return;
                    case Key.R:
                        CameraVideoButton_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        return;
                    case Key.S:
                        CameraSnapshotButton_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        return;
                    case Key.O:
                        OpenOutputFolderButton_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        return;
                }
            }

            if (modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                if (e.Key == Key.R)
                {
                    RenderVideoButton_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.S)
                {
                    SnapshotButton_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    return;
                }
            }

            if (modifiers != ModifierKeys.None)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Space:
                    FreezeToggle.IsChecked = FreezeToggle.IsChecked != true;
                    e.Handled = true;
                    break;
                case Key.G:
                    GridToggle.IsChecked = GridToggle.IsChecked != true;
                    e.Handled = true;
                    break;
                case Key.M:
                    MirrorToggle.IsChecked = MirrorToggle.IsChecked != true;
                    e.Handled = true;
                    break;
                case Key.S:
                    SkeletonToggle.IsChecked = SkeletonToggle.IsChecked != true;
                    e.Handled = true;
                    break;
                case Key.F1:
                    ShowAboutDialog();
                    e.Handled = true;
                    break;
            }
        }

        private void ProductWindow_ContentRendered(object sender, EventArgs e)
        {
            if (productShellReady)
            {
                return;
            }

            productShellReady = true;
            Dispatcher.BeginInvoke(new Action(ApplyRestoredWorkspace), DispatcherPriority.ContextIdle);
            InstallHelpMenu();
            ConfigureToolTips();
            CreateToastLayer();
        }

        private void ConfigureWindowsShell()
        {
            TaskbarItemInfo = new TaskbarItemInfo
            {
                ProgressState = TaskbarItemProgressState.None,
                ProgressValue = 0
            };
        }

        private void LoadDesktopSettings()
        {
            loadedSettings = AppSettingsStore.Load();

            if (loadedSettings.WindowWidth >= MinWidth && loadedSettings.WindowHeight >= MinHeight)
            {
                Width = Math.Min(loadedSettings.WindowWidth, SystemParameters.VirtualScreenWidth);
                Height = Math.Min(loadedSettings.WindowHeight, SystemParameters.VirtualScreenHeight);
            }

            if (IsVisibleOnVirtualScreen(loadedSettings.WindowLeft, loadedSettings.WindowTop, Width, Height))
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = loadedSettings.WindowLeft;
                Top = loadedSettings.WindowTop;
            }

            MirrorToggle.IsChecked = loadedSettings.Mirror;
            SeatedModeToggle.IsChecked = loadedSettings.SeatedTracking;
            BodyOnlyToggle.IsChecked = loadedSettings.BodyOnly;
            BrightnessSlider.Value = Clamp(loadedSettings.Brightness, BrightnessSlider.Minimum, BrightnessSlider.Maximum);
            ContrastSlider.Value = Clamp(loadedSettings.Contrast, ContrastSlider.Minimum, ContrastSlider.Maximum);
            SmoothingSlider.Value = Clamp(loadedSettings.Smoothing, SmoothingSlider.Minimum, SmoothingSlider.Maximum);
            MotionThresholdSlider.Value = Clamp(loadedSettings.MotionThreshold, MotionThresholdSlider.Minimum, MotionThresholdSlider.Maximum);
            MotionPersistenceSlider.Value = Clamp(loadedSettings.MotionPersistence, MotionPersistenceSlider.Minimum, MotionPersistenceSlider.Maximum);
            PointCloudDetailSlider.Value = Clamp(loadedSettings.PointCloudDetail, PointCloudDetailSlider.Minimum, PointCloudDetailSlider.Maximum);
            PointCloudPointSizeSlider.Value = Clamp(loadedSettings.PointCloudPointSize, PointCloudPointSizeSlider.Minimum, PointCloudPointSizeSlider.Maximum);
            PointCloudShadingToggle.IsChecked = loadedSettings.PointCloudShading;
            OutputScaleSlider.Value = Clamp(loadedSettings.OutputScale, OutputScaleSlider.Minimum, OutputScaleSlider.Maximum);
            VideoFpsSlider.Value = Clamp(loadedSettings.VideoFps, VideoFpsSlider.Minimum, VideoFpsSlider.Maximum);
            VideoQualitySlider.Value = Clamp(loadedSettings.VideoQuality, VideoQualitySlider.Minimum, VideoQualitySlider.Maximum);

            if (loadedSettings.SceneIndex >= 0 && loadedSettings.SceneIndex < SceneList.Items.Count)
            {
                SceneList.SelectedIndex = loadedSettings.SceneIndex;
            }

            if (loadedSettings.WindowMaximized)
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void ApplyRestoredWorkspace()
        {
            if (loadedSettings == null)
            {
                return;
            }

            SelectPreviewMode(ParsePreviewMode(loadedSettings.PreviewMode));
            UpdateAdjustmentLabels();
            UpdateTitleForScene();
            StatusBarMessageText.Text = "Workspace restored";
        }

        private void ProductWindow_Closing(object sender, CancelEventArgs e)
        {
            SaveDesktopSettings();
        }

        private void SaveDesktopSettings()
        {
            try
            {
                Rect bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
                AppSettings settings = new AppSettings
                {
                    WindowLeft = bounds.Left,
                    WindowTop = bounds.Top,
                    WindowWidth = bounds.Width,
                    WindowHeight = bounds.Height,
                    WindowMaximized = WindowState == WindowState.Maximized,
                    SceneIndex = SceneList == null ? 0 : SceneList.SelectedIndex,
                    PreviewMode = currentPreviewMode.ToString(),
                    Mirror = MirrorToggle.IsChecked == true,
                    SeatedTracking = SeatedModeToggle.IsChecked == true,
                    BodyOnly = BodyOnlyToggle.IsChecked == true,
                    Brightness = BrightnessSlider.Value,
                    Contrast = ContrastSlider.Value,
                    Smoothing = SmoothingSlider.Value,
                    MotionThreshold = MotionThresholdSlider.Value,
                    MotionPersistence = MotionPersistenceSlider.Value,
                    PointCloudDetail = PointCloudDetailSlider.Value,
                    PointCloudPointSize = PointCloudPointSizeSlider.Value,
                    PointCloudShading = PointCloudShadingToggle.IsChecked == true,
                    OutputScale = OutputScaleSlider.Value,
                    VideoFps = VideoFpsSlider.Value,
                    VideoQuality = VideoQualitySlider.Value
                };

                AppSettingsStore.Save(settings);
            }
            catch
            {
                // Settings must never prevent the application from closing.
            }
        }

        private void InstallHelpMenu()
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

            MenuItem help = new MenuItem { Header = "_Help" };
            MenuItem shortcuts = new MenuItem { Header = "Keyboard shortcuts" };
            shortcuts.Click += delegate { ShowShortcutDialog(); };
            MenuItem settingsFolder = new MenuItem { Header = "Open settings folder" };
            settingsFolder.Click += delegate
            {
                Directory.CreateDirectory(AppSettingsStore.SettingsDirectory);
                Process.Start("explorer.exe", AppSettingsStore.SettingsDirectory);
            };
            MenuItem about = new MenuItem { Header = "About Kinect Reframe" };
            about.Click += delegate { ShowAboutDialog(); };

            help.Items.Add(shortcuts);
            help.Items.Add(settingsFolder);
            help.Items.Add(new Separator());
            help.Items.Add(about);
            menu.Items.Add(help);
        }

        private void ConfigureToolTips()
        {
            CameraPreviewModeButton.ToolTip = "Camera preview (Ctrl+1)";
            DepthPreviewModeButton.ToolTip = "Depth preview (Ctrl+2)";
            PointCloudPreviewModeButton.ToolTip = "3D preview (Ctrl+3)";
            SplitPreviewModeButton.ToolTip = "Split preview (Ctrl+4)";
            CameraVideoButton.ToolTip = "Start or stop camera video (Ctrl+R)";
            RenderVideoButton.ToolTip = "Start or stop render video (Ctrl+Shift+R)";
            SkeletonToggle.ToolTip = "Show enhanced skeleton (S)";
            GridToggle.ToolTip = "Show framing grid (G)";
            MirrorToggle.ToolTip = "Mirror camera composition (M)";
            FreezeToggle.ToolTip = "Freeze the current frame (Space)";
            SceneList.ToolTip = "Select a prepared workspace";
        }

        private void CreateToastLayer()
        {
            Grid root = Content as Grid;
            if (root == null || toastBorder != null)
            {
                return;
            }

            toastText = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 360
            };

            toastBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(245, 38, 41, 47)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(81, 88, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(12, 9, 12, 9),
                Margin = new Thickness(0, 88, 18, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed,
                Child = toastText,
                IsHitTestVisible = false
            };

            Grid.SetRowSpan(toastBorder, root.RowDefinitions.Count);
            Panel.SetZIndex(toastBorder, 1000);
            root.Children.Add(toastBorder);

            toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.2) };
            toastTimer.Tick += delegate
            {
                toastTimer.Stop();
                toastBorder.Visibility = Visibility.Collapsed;
            };
        }

        internal void ShowToast(string message)
        {
            if (toastBorder == null || toastText == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            toastText.Text = message;
            toastBorder.Visibility = Visibility.Visible;
            toastTimer.Stop();
            toastTimer.Start();
        }

        internal void UpdateTaskbarRecordingState(bool recording)
        {
            if (TaskbarItemInfo == null)
            {
                return;
            }

            TaskbarItemInfo.ProgressState = recording
                ? TaskbarItemProgressState.Indeterminate
                : TaskbarItemProgressState.None;
        }

        internal void UpdateTitleForScene()
        {
            ListBoxItem item = SceneList == null ? null : SceneList.SelectedItem as ListBoxItem;
            string sceneName = item == null ? null : item.Content as string;
            Title = string.IsNullOrWhiteSpace(sceneName)
                ? "Kinect Reframe"
                : "Kinect Reframe — " + sceneName;
        }

        private void ToggleFullscreenPreview()
        {
            Grid root = Content as Grid;
            if (root == null || root.RowDefinitions.Count < 5)
            {
                return;
            }

            if (!fullscreenPreview)
            {
                previousWindowStyle = WindowStyle;
                previousResizeMode = ResizeMode;
                previousWindowState = WindowState;
                previousRootRows = new GridLength[root.RowDefinitions.Count];
                for (int i = 0; i < root.RowDefinitions.Count; i++)
                {
                    previousRootRows[i] = root.RowDefinitions[i].Height;
                }

                workspaceGrid = FindWorkspaceGrid(root);
                if (workspaceGrid != null)
                {
                    previousWorkspaceColumns = new GridLength[workspaceGrid.ColumnDefinitions.Count];
                    for (int i = 0; i < workspaceGrid.ColumnDefinitions.Count; i++)
                    {
                        previousWorkspaceColumns[i] = workspaceGrid.ColumnDefinitions[i].Width;
                    }

                    workspaceGrid.ColumnDefinitions[0].Width = new GridLength(0);
                    workspaceGrid.ColumnDefinitions[1].Width = new GridLength(0);
                    workspaceGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
                    workspaceGrid.ColumnDefinitions[3].Width = new GridLength(0);
                    workspaceGrid.ColumnDefinitions[4].Width = new GridLength(0);
                    workspaceGrid.Margin = new Thickness(0);
                }

                root.RowDefinitions[0].Height = new GridLength(0);
                root.RowDefinitions[1].Height = new GridLength(0);
                root.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
                root.RowDefinitions[3].Height = new GridLength(0);
                root.RowDefinitions[4].Height = new GridLength(0);

                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;
                fullscreenPreview = true;
                ShowToast("Fullscreen preview • press F11 or Esc to exit");
                return;
            }

            for (int i = 0; i < previousRootRows.Length; i++)
            {
                root.RowDefinitions[i].Height = previousRootRows[i];
            }

            if (workspaceGrid != null && previousWorkspaceColumns != null)
            {
                for (int i = 0; i < previousWorkspaceColumns.Length; i++)
                {
                    workspaceGrid.ColumnDefinitions[i].Width = previousWorkspaceColumns[i];
                }

                workspaceGrid.Margin = new Thickness(8);
            }

            WindowStyle = previousWindowStyle;
            ResizeMode = previousResizeMode;
            WindowState = previousWindowState;
            fullscreenPreview = false;
        }

        private static Grid FindWorkspaceGrid(Grid root)
        {
            foreach (UIElement child in root.Children)
            {
                Grid candidate = child as Grid;
                if (candidate != null && Grid.GetRow(candidate) == 2 && candidate.ColumnDefinitions.Count == 5)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void ShowShortcutDialog()
        {
            MessageBox.Show(
                this,
                "Ctrl+1   Camera preview\n" +
                "Ctrl+2   Depth preview\n" +
                "Ctrl+3   3D preview\n" +
                "Ctrl+4   Split preview\n\n" +
                "Ctrl+R         Record camera video\n" +
                "Ctrl+Shift+R   Record render video\n" +
                "Ctrl+S         Save camera frame\n" +
                "Ctrl+Shift+S   Save application snapshot\n" +
                "Ctrl+O         Open output folder\n\n" +
                "S   Skeleton overlay\n" +
                "G   Framing grid\n" +
                "M   Mirror\n" +
                "Space   Freeze frame\n" +
                "F11     Fullscreen preview\n" +
                "Esc     Exit fullscreen",
                "Kinect Reframe keyboard shortcuts",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ShowAboutDialog()
        {
            MessageBox.Show(
                this,
                "Kinect Reframe\n\n" +
                "Xbox 360 Kinect tracking, depth visualisation and real-time 3D rendering studio.\n\n" +
                "Kinect for Windows SDK 1.8 • x86 • .NET Framework 4.8",
                "About Kinect Reframe",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static bool IsVisibleOnVirtualScreen(double left, double top, double width, double height)
        {
            if (double.IsNaN(left) || double.IsNaN(top) || double.IsInfinity(left) || double.IsInfinity(top))
            {
                return false;
            }

            Rect windowRect = new Rect(left, top, Math.Max(100, width), Math.Max(100, height));
            Rect screenRect = new Rect(
                SystemParameters.VirtualScreenLeft,
                SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth,
                SystemParameters.VirtualScreenHeight);
            return windowRect.IntersectsWith(screenRect);
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return minimum;
            }

            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}
