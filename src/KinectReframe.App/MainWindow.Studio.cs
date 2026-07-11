using System;
using System.Windows;
using System.Windows.Controls;

namespace KinectReframe
{
    public partial class MainWindow
    {
        private bool studioEventsHooked;
        private bool suppressPreviewModeEvents;
        private StudioPreviewMode currentPreviewMode = StudioPreviewMode.Camera;

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (studioEventsHooked)
            {
                return;
            }

            studioEventsHooked = true;
            HookStudioEvents();

            if (SceneList != null && SceneList.SelectedItem is ListBoxItem selectedScene)
            {
                ApplyScenePreset(selectedScene.Tag as string);
            }
            else
            {
                SelectPreviewMode(StudioPreviewMode.Camera);
            }
        }

        private void HookStudioEvents()
        {
            FocusCameraToggle.Checked += FocusCameraToggle_StudioChanged;
            FocusCameraToggle.Unchecked += FocusCameraToggle_StudioChanged;

            SkeletonToggle.Checked += StudioVisualSetting_Changed;
            SkeletonToggle.Unchecked += StudioVisualSetting_Changed;
            GridToggle.Checked += StudioVisualSetting_Changed;
            GridToggle.Unchecked += StudioVisualSetting_Changed;
            MirrorToggle.Checked += StudioVisualSetting_Changed;
            MirrorToggle.Unchecked += StudioVisualSetting_Changed;
            FreezeToggle.Checked += StudioVisualSetting_Changed;
            FreezeToggle.Unchecked += StudioVisualSetting_Changed;
        }

        private void StudioVisualSetting_Changed(object sender, RoutedEventArgs e)
        {
            if (!uiReady || suppressPreviewModeEvents)
            {
                return;
            }

            ApplyPreviewLayout();
        }

        private void FocusCameraToggle_StudioChanged(object sender, RoutedEventArgs e)
        {
            if (!uiReady || suppressPreviewModeEvents)
            {
                return;
            }

            if (FocusCameraToggle.IsChecked == true)
            {
                SelectPreviewMode(StudioPreviewMode.Camera);
            }
            else
            {
                ApplyPreviewLayout();
            }
        }

        private void PreviewModeButton_Checked(object sender, RoutedEventArgs e)
        {
            // ToggleButton.Checked can fire while InitializeComponent is still constructing
            // the XAML tree. At that point later named controls are still null, so defer all
            // preview work until Window_Loaded has marked the interface ready.
            if (!uiReady || suppressPreviewModeEvents || !(sender is FrameworkElement element))
            {
                return;
            }

            SelectPreviewMode(ParsePreviewMode(element.Tag as string));
        }

        private void PreviewModeMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                SelectPreviewMode(ParsePreviewMode(item.Tag as string));
            }
        }

        private void SceneList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!uiReady || !(SceneList.SelectedItem is ListBoxItem selectedScene))
            {
                return;
            }

            ApplyScenePreset(selectedScene.Tag as string);
        }

        private void ApplyScenePreset(string preset)
        {
            if (string.IsNullOrWhiteSpace(preset))
            {
                return;
            }

            switch (preset)
            {
                case "CleanCamera":
                    SetCameraLayers(false, false, false, false, false);
                    SelectPreviewMode(StudioPreviewMode.Camera);
                    break;

                case "Skeleton":
                    SetCameraLayers(true, false, false, false, false);
                    SelectPreviewMode(StudioPreviewMode.Camera);
                    break;

                case "MotionHeat":
                    SetCameraLayers(false, false, true, false, false);
                    SelectPreviewMode(StudioPreviewMode.Camera);
                    break;

                case "DepthHeat":
                    SetCameraLayers(false, false, false, true, false);
                    SelectPreviewMode(StudioPreviewMode.Camera);
                    break;

                case "DepthHologram":
                    SetCameraLayers(false, false, false, false, false);
                    BodyOnlyToggle.IsChecked = true;
                    SelectPreviewMode(StudioPreviewMode.Depth);
                    break;

                case "PointCloud":
                    SetCameraLayers(false, false, false, false, false);
                    BodyOnlyToggle.IsChecked = true;
                    SelectPreviewMode(StudioPreviewMode.PointCloud);
                    break;

                case "Split":
                    SetCameraLayers(false, false, false, false, false);
                    BodyOnlyToggle.IsChecked = true;
                    RendererTabs.SelectedIndex = 1;
                    SelectPreviewMode(StudioPreviewMode.Split);
                    break;

                case "TrackingDebug":
                    SetCameraLayers(true, true, false, false, false);
                    BodyOnlyToggle.IsChecked = true;
                    RendererTabs.SelectedIndex = 0;
                    SelectPreviewMode(StudioPreviewMode.Split);
                    break;
            }

            StatusBarMessageText.Text = "Scene: " + GetSceneDisplayName(preset);
        }

        private void SetCameraLayers(bool skeleton, bool rawSkeleton, bool motionHeat, bool depthHeat, bool grid)
        {
            SkeletonToggle.IsChecked = skeleton;
            RawSkeletonToggle.IsChecked = skeleton && rawSkeleton;
            MotionHeatToggle.IsChecked = motionHeat;
            DepthHeatToggle.IsChecked = depthHeat;
            GridToggle.IsChecked = grid;
        }

        private void SelectPreviewMode(StudioPreviewMode mode)
        {
            currentPreviewMode = mode;

            // This method can also be reached by settings restoration. Keep it safe if the
            // visual tree is not complete yet and let OnContentRendered apply the layout.
            if (CameraPreviewModeButton == null ||
                DepthPreviewModeButton == null ||
                PointCloudPreviewModeButton == null ||
                SplitPreviewModeButton == null ||
                RendererTabs == null ||
                FocusCameraToggle == null)
            {
                return;
            }

            suppressPreviewModeEvents = true;

            CameraPreviewModeButton.IsChecked = mode == StudioPreviewMode.Camera;
            DepthPreviewModeButton.IsChecked = mode == StudioPreviewMode.Depth;
            PointCloudPreviewModeButton.IsChecked = mode == StudioPreviewMode.PointCloud;
            SplitPreviewModeButton.IsChecked = mode == StudioPreviewMode.Split;

            if (mode == StudioPreviewMode.Depth)
            {
                RendererTabs.SelectedIndex = 0;
            }
            else if (mode == StudioPreviewMode.PointCloud)
            {
                RendererTabs.SelectedIndex = 1;
            }

            bool previousUiReady = uiReady;
            uiReady = false;
            FocusCameraToggle.IsChecked = mode == StudioPreviewMode.Camera;
            uiReady = previousUiReady;

            suppressPreviewModeEvents = false;
            ApplyPreviewLayout();
        }

        private void ApplyPreviewLayout()
        {
            if (CameraViewport == null ||
                RightPanelBorder == null ||
                CameraPreviewColumn == null ||
                RightPanelSpacerColumn == null ||
                RightPanelColumn == null ||
                PreviewDescriptionText == null ||
                StatusPreviewModeText == null ||
                StatusBarMessageText == null)
            {
                return;
            }

            switch (currentPreviewMode)
            {
                case StudioPreviewMode.Camera:
                    CameraViewport.Visibility = Visibility.Visible;
                    CameraPreviewColumn.Width = new GridLength(1, GridUnitType.Star);
                    RightPanelBorder.Visibility = Visibility.Collapsed;
                    RightPanelSpacerColumn.Width = new GridLength(0);
                    RightPanelColumn.Width = new GridLength(0);
                    PreviewDescriptionText.Text = "Camera composition";
                    StatusPreviewModeText.Text = "Camera";
                    break;

                case StudioPreviewMode.Depth:
                    CameraViewport.Visibility = Visibility.Collapsed;
                    CameraPreviewColumn.Width = new GridLength(0);
                    RightPanelBorder.Visibility = Visibility.Visible;
                    RightPanelSpacerColumn.Width = new GridLength(0);
                    RightPanelColumn.Width = new GridLength(1, GridUnitType.Star);
                    RendererTabs.SelectedIndex = 0;
                    PreviewDescriptionText.Text = "Depth hologram";
                    StatusPreviewModeText.Text = "Depth";
                    break;

                case StudioPreviewMode.PointCloud:
                    CameraViewport.Visibility = Visibility.Collapsed;
                    CameraPreviewColumn.Width = new GridLength(0);
                    RightPanelBorder.Visibility = Visibility.Visible;
                    RightPanelSpacerColumn.Width = new GridLength(0);
                    RightPanelColumn.Width = new GridLength(1, GridUnitType.Star);
                    RendererTabs.SelectedIndex = 1;
                    PreviewDescriptionText.Text = "Interactive point cloud";
                    StatusPreviewModeText.Text = "3D";
                    break;

                case StudioPreviewMode.Split:
                    CameraViewport.Visibility = Visibility.Visible;
                    CameraPreviewColumn.Width = new GridLength(3, GridUnitType.Star);
                    RightPanelBorder.Visibility = Visibility.Visible;
                    RightPanelSpacerColumn.Width = new GridLength(8);
                    RightPanelColumn.Width = new GridLength(2, GridUnitType.Star);
                    PreviewDescriptionText.Text = RendererTabs.SelectedIndex == 0
                        ? "Camera and depth"
                        : "Camera and 3D";
                    StatusPreviewModeText.Text = "Split";
                    break;
            }

            StatusBarMessageText.Text = "Preview: " + StatusPreviewModeText.Text;
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static StudioPreviewMode ParsePreviewMode(string value)
        {
            switch (value)
            {
                case "Depth":
                    return StudioPreviewMode.Depth;
                case "PointCloud":
                    return StudioPreviewMode.PointCloud;
                case "Split":
                    return StudioPreviewMode.Split;
                default:
                    return StudioPreviewMode.Camera;
            }
        }

        private static string GetSceneDisplayName(string preset)
        {
            switch (preset)
            {
                case "CleanCamera": return "Clean Camera";
                case "Skeleton": return "Skeleton Tracking";
                case "MotionHeat": return "Motion Heatmap";
                case "DepthHeat": return "Depth Heatmap";
                case "DepthHologram": return "Depth Hologram";
                case "PointCloud": return "3D Point Cloud";
                case "Split": return "Camera + 3D";
                case "TrackingDebug": return "Tracking Debug";
                default: return preset;
            }
        }

        private enum StudioPreviewMode
        {
            Camera,
            Depth,
            PointCloud,
            Split
        }
    }
}
