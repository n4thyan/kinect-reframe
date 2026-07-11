using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using KinectReframe.Services;

namespace KinectReframe
{
    public partial class MainWindow
    {
        private bool shellEventsAttached;

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            if (shellEventsAttached)
            {
                return;
            }

            shellEventsAttached = true;
            AttachShellEvents();
            LoadCustomScenes();
            InstallSceneContextMenu();
            Closing += SaveCustomScenesOnClosing;
            UpdateTitleForScene();
        }

        private void AttachShellEvents()
        {
            SceneList.SelectionChanged += ShellSceneList_SelectionChanged;

            SkeletonToggle.Click += SourceToggle_Click;
            RawSkeletonToggle.Click += SourceToggle_Click;
            MotionHeatToggle.Click += SourceToggle_Click;
            DepthHeatToggle.Click += SourceToggle_Click;
            GridToggle.Click += SourceToggle_Click;
            MirrorToggle.Click += SourceToggle_Click;
            FreezeToggle.Click += SourceToggle_Click;
            SeatedModeToggle.Click += SourceToggle_Click;
            BodyOnlyToggle.Click += SourceToggle_Click;
            PointCloudShadingToggle.Click += SourceToggle_Click;

            CameraVideoButton.Click += RecordingButton_PostClick;
            RenderVideoButton.Click += RecordingButton_PostClick;
            RecordButton.Click += TrackingRecordButton_PostClick;

            CameraPreviewModeButton.Click += PreviewButton_PostClick;
            DepthPreviewModeButton.Click += PreviewButton_PostClick;
            PointCloudPreviewModeButton.Click += PreviewButton_PostClick;
            SplitPreviewModeButton.Click += PreviewButton_PostClick;
        }

        private void ShellSceneList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBoxItem selected = SceneList.SelectedItem as ListBoxItem;
            if (selected == null)
            {
                return;
            }

            CustomSceneSettings custom = selected.Tag as CustomSceneSettings;
            if (custom != null)
            {
                ApplyCustomScene(custom);
            }

            UpdateTitleForScene();
            ShowToast("Scene: " + Convert.ToString(selected.Content));
            FocusInspectorForScene(selected.Tag);
        }

        private void PreviewButton_PostClick(object sender, RoutedEventArgs e)
        {
            StatusBarMessageText.Text = "Preview: " + StatusPreviewModeText.Text;
        }

        private void RecordingButton_PostClick(object sender, RoutedEventArgs e)
        {
            UpdateTaskbarRecordingState(videoRecorder.IsRecording);

            if (videoRecorder.IsRecording)
            {
                ShowToast("Video recording started • " + activeVideoWidth + " × " + activeVideoHeight);
            }
            else
            {
                ShowToast(TrackingStatusText.Text);
            }
        }

        private void TrackingRecordButton_PostClick(object sender, RoutedEventArgs e)
        {
            ShowToast(recorder.IsRecording ? "Tracking-data capture started" : TrackingStatusText.Text);
        }

        private void SourceToggle_Click(object sender, RoutedEventArgs e)
        {
            string section = GetInspectorSection(sender as FrameworkElement);
            if (!string.IsNullOrWhiteSpace(section))
            {
                FocusPropertySection(section);
                StatusBarMessageText.Text = "Properties: " + section;
            }
        }

        private static string GetInspectorSection(FrameworkElement element)
        {
            if (element == null)
            {
                return null;
            }

            string name = element.Name;
            if (name == "SkeletonToggle" || name == "RawSkeletonToggle" || name == "SeatedModeToggle")
            {
                return "Tracking";
            }

            if (name == "MotionHeatToggle" || name == "DepthHeatToggle")
            {
                return "Heatmaps";
            }

            if (name == "BodyOnlyToggle" || name == "PointCloudShadingToggle")
            {
                return "3D Render";
            }

            return "Camera";
        }

        private void FocusInspectorForScene(object sceneTag)
        {
            string builtIn = sceneTag as string;
            if (builtIn == "Skeleton" || builtIn == "TrackingDebug")
            {
                FocusPropertySection("Tracking");
            }
            else if (builtIn == "MotionHeat" || builtIn == "DepthHeat")
            {
                FocusPropertySection("Heatmaps");
            }
            else if (builtIn == "DepthHologram" || builtIn == "PointCloud" || builtIn == "Split")
            {
                FocusPropertySection("3D Render");
            }
            else
            {
                FocusPropertySection("Camera");
            }
        }

        private void FocusPropertySection(string header)
        {
            List<Expander> expanders = new List<Expander>();
            CollectExpanders(this, expanders);

            foreach (Expander expander in expanders)
            {
                string currentHeader = Convert.ToString(expander.Header);
                expander.IsExpanded = string.Equals(currentHeader, header, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static void CollectExpanders(DependencyObject parent, IList<Expander> results)
        {
            if (parent == null)
            {
                return;
            }

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int index = 0; index < count; index++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, index);
                Expander expander = child as Expander;
                if (expander != null)
                {
                    results.Add(expander);
                }

                CollectExpanders(child, results);
            }
        }

        private void InstallSceneContextMenu()
        {
            ContextMenu menu = new ContextMenu();

            MenuItem save = new MenuItem { Header = "Save current layout as scene" };
            save.Click += delegate { SaveCurrentAsCustomScene(); };

            MenuItem duplicate = new MenuItem { Header = "Duplicate selected scene" };
            duplicate.Click += delegate { DuplicateSelectedScene(); };

            MenuItem rename = new MenuItem { Header = "Rename custom scene" };
            rename.Click += delegate { RenameSelectedScene(); };

            MenuItem delete = new MenuItem { Header = "Delete custom scene" };
            delete.Click += delegate { DeleteSelectedScene(); };

            menu.Items.Add(save);
            menu.Items.Add(duplicate);
            menu.Items.Add(new Separator());
            menu.Items.Add(rename);
            menu.Items.Add(delete);
            SceneList.ContextMenu = menu;
        }

        private void LoadCustomScenes()
        {
            if (loadedSettings == null || loadedSettings.CustomScenes == null)
            {
                return;
            }

            foreach (CustomSceneSettings scene in loadedSettings.CustomScenes)
            {
                if (scene == null || string.IsNullOrWhiteSpace(scene.Name))
                {
                    continue;
                }

                SceneList.Items.Add(CreateCustomSceneItem(scene));
            }
        }

        private void SaveCurrentAsCustomScene()
        {
            string name = PromptForText("Save scene", "Scene name", "Custom Scene");
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            CustomSceneSettings scene = CaptureCurrentScene(name.Trim());
            ListBoxItem item = CreateCustomSceneItem(scene);
            SceneList.Items.Add(item);
            SceneList.SelectedItem = item;
            ShowToast("Saved scene: " + scene.Name);
        }

        private void DuplicateSelectedScene()
        {
            ListBoxItem selected = SceneList.SelectedItem as ListBoxItem;
            if (selected == null)
            {
                return;
            }

            string baseName = Convert.ToString(selected.Content);
            string name = PromptForText("Duplicate scene", "Scene name", "Copy of " + baseName);
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            CustomSceneSettings scene = CaptureCurrentScene(name.Trim());
            ListBoxItem item = CreateCustomSceneItem(scene);
            SceneList.Items.Add(item);
            SceneList.SelectedItem = item;
        }

        private void RenameSelectedScene()
        {
            ListBoxItem selected = SceneList.SelectedItem as ListBoxItem;
            CustomSceneSettings scene = selected == null ? null : selected.Tag as CustomSceneSettings;
            if (scene == null)
            {
                ShowToast("Built-in scenes cannot be renamed");
                return;
            }

            string name = PromptForText("Rename scene", "Scene name", scene.Name);
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            scene.Name = name.Trim();
            selected.Content = scene.Name;
            UpdateTitleForScene();
        }

        private void DeleteSelectedScene()
        {
            ListBoxItem selected = SceneList.SelectedItem as ListBoxItem;
            if (selected == null || !(selected.Tag is CustomSceneSettings))
            {
                ShowToast("Built-in scenes cannot be deleted");
                return;
            }

            string name = Convert.ToString(selected.Content);
            int index = SceneList.SelectedIndex;
            SceneList.Items.Remove(selected);
            SceneList.SelectedIndex = Math.Max(0, Math.Min(index - 1, SceneList.Items.Count - 1));
            ShowToast("Deleted scene: " + name);
        }

        private ListBoxItem CreateCustomSceneItem(CustomSceneSettings scene)
        {
            return new ListBoxItem
            {
                Content = scene.Name,
                Tag = scene,
                ToolTip = "Custom scene • right-click to rename or delete"
            };
        }

        private CustomSceneSettings CaptureCurrentScene(string name)
        {
            return new CustomSceneSettings
            {
                Name = name,
                PreviewMode = currentPreviewMode.ToString(),
                RendererTab = RendererTabs.SelectedIndex,
                Skeleton = SkeletonToggle.IsChecked == true,
                RawSkeleton = RawSkeletonToggle.IsChecked == true,
                MotionHeat = MotionHeatToggle.IsChecked == true,
                DepthHeat = DepthHeatToggle.IsChecked == true,
                Grid = GridToggle.IsChecked == true,
                Mirror = MirrorToggle.IsChecked == true,
                SeatedTracking = SeatedModeToggle.IsChecked == true,
                BodyOnly = BodyOnlyToggle.IsChecked == true,
                PointCloudShading = PointCloudShadingToggle.IsChecked == true,
                Brightness = BrightnessSlider.Value,
                Contrast = ContrastSlider.Value,
                Smoothing = SmoothingSlider.Value,
                MotionThreshold = MotionThresholdSlider.Value,
                MotionPersistence = MotionPersistenceSlider.Value,
                PointCloudDetail = PointCloudDetailSlider.Value,
                PointCloudPointSize = PointCloudPointSizeSlider.Value,
                OutputScale = OutputScaleSlider.Value,
                VideoFps = VideoFpsSlider.Value,
                VideoQuality = VideoQualitySlider.Value
            };
        }

        private void ApplyCustomScene(CustomSceneSettings scene)
        {
            if (scene == null)
            {
                return;
            }

            SkeletonToggle.IsChecked = scene.Skeleton;
            RawSkeletonToggle.IsChecked = scene.Skeleton && scene.RawSkeleton;
            MotionHeatToggle.IsChecked = scene.MotionHeat;
            DepthHeatToggle.IsChecked = scene.DepthHeat;
            GridToggle.IsChecked = scene.Grid;
            MirrorToggle.IsChecked = scene.Mirror;
            SeatedModeToggle.IsChecked = scene.SeatedTracking;
            BodyOnlyToggle.IsChecked = scene.BodyOnly;
            PointCloudShadingToggle.IsChecked = scene.PointCloudShading;
            BrightnessSlider.Value = Clamp(scene.Brightness, BrightnessSlider.Minimum, BrightnessSlider.Maximum);
            ContrastSlider.Value = Clamp(scene.Contrast, ContrastSlider.Minimum, ContrastSlider.Maximum);
            SmoothingSlider.Value = Clamp(scene.Smoothing, SmoothingSlider.Minimum, SmoothingSlider.Maximum);
            MotionThresholdSlider.Value = Clamp(scene.MotionThreshold, MotionThresholdSlider.Minimum, MotionThresholdSlider.Maximum);
            MotionPersistenceSlider.Value = Clamp(scene.MotionPersistence, MotionPersistenceSlider.Minimum, MotionPersistenceSlider.Maximum);
            PointCloudDetailSlider.Value = Clamp(scene.PointCloudDetail, PointCloudDetailSlider.Minimum, PointCloudDetailSlider.Maximum);
            PointCloudPointSizeSlider.Value = Clamp(scene.PointCloudPointSize, PointCloudPointSizeSlider.Minimum, PointCloudPointSizeSlider.Maximum);
            OutputScaleSlider.Value = Clamp(scene.OutputScale, OutputScaleSlider.Minimum, OutputScaleSlider.Maximum);
            VideoFpsSlider.Value = Clamp(scene.VideoFps, VideoFpsSlider.Minimum, VideoFpsSlider.Maximum);
            VideoQualitySlider.Value = Clamp(scene.VideoQuality, VideoQualitySlider.Minimum, VideoQualitySlider.Maximum);
            RendererTabs.SelectedIndex = scene.RendererTab == 0 ? 0 : 1;
            SelectPreviewMode(ParsePreviewMode(scene.PreviewMode));
            ApplyVisualSettings();
            UpdateAdjustmentLabels();
        }

        private void SaveCustomScenesOnClosing(object sender, CancelEventArgs e)
        {
            try
            {
                AppSettings settings = AppSettingsStore.Load();
                settings.CustomScenes.Clear();

                foreach (object itemObject in SceneList.Items)
                {
                    ListBoxItem item = itemObject as ListBoxItem;
                    CustomSceneSettings scene = item == null ? null : item.Tag as CustomSceneSettings;
                    if (scene != null)
                    {
                        settings.CustomScenes.Add(scene);
                    }
                }

                settings.SceneIndex = SceneList.SelectedIndex;
                AppSettingsStore.Save(settings);
            }
            catch
            {
                // Scene persistence should never block application shutdown.
            }
        }

        private string PromptForText(string title, string label, string initialValue)
        {
            Window dialog = new Window
            {
                Title = title,
                Owner = this,
                Width = 390,
                Height = 165,
                MinWidth = 340,
                MinHeight = 150,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = (Brush)FindResource("PanelBrush"),
                ShowInTaskbar = false
            };

            Grid layout = new Grid { Margin = new Thickness(14) };
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock labelBlock = new TextBlock { Text = label, Foreground = (Brush)FindResource("MutedTextBrush") };
            TextBox input = new TextBox
            {
                Text = initialValue ?? string.Empty,
                Padding = new Thickness(8, 6, 8, 6),
                Background = (Brush)FindResource("ControlBrush"),
                Foreground = Brushes.White,
                BorderBrush = (Brush)FindResource("PanelBorderBrush"),
                BorderThickness = new Thickness(1)
            };
            Grid.SetRow(input, 2);

            StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Button cancel = new Button { Content = "Cancel", MinWidth = 80 };
            Button save = new Button { Content = "Save", MinWidth = 80, Style = (Style)FindResource("PrimaryButtonStyle"), Margin = new Thickness(0) };
            cancel.Click += delegate { dialog.DialogResult = false; };
            save.Click += delegate { dialog.DialogResult = true; };
            buttons.Children.Add(cancel);
            buttons.Children.Add(save);
            Grid.SetRow(buttons, 4);

            layout.Children.Add(labelBlock);
            layout.Children.Add(input);
            layout.Children.Add(buttons);
            dialog.Content = layout;
            dialog.Loaded += delegate
            {
                input.Focus();
                input.SelectAll();
            };

            bool? result = dialog.ShowDialog();
            return result == true ? input.Text : null;
        }
    }
}
