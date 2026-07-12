using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Kinect;

namespace KinectReframe
{
    public partial class MainWindow
    {
        private Slider cameraZoomSlider;
        private Slider cameraPanXSlider;
        private Slider cameraPanYSlider;
        private TextBlock cameraZoomValueText;
        private TextBlock cameraPanXValueText;
        private TextBlock cameraPanYValueText;
        private ToggleButton autoFrameToggle;
        private TextBlock autoFrameStatusText;

        private Slider sensorTiltSlider;
        private TextBlock sensorTiltValueText;
        private TextBlock accelerometerText;
        private DispatcherTimer sensorTelemetryTimer;
        private DispatcherTimer tiltDebounceTimer;
        private bool suppressTiltChanges;

        private ToggleButton microphoneToggle;
        private ProgressBar microphoneLevelMeter;
        private TextBlock microphoneLevelText;
        private TextBlock microphoneDirectionText;
        private Thread microphoneThread;
        private Stream microphoneStream;
        private volatile bool microphoneRunning;

        private double automaticPanX;
        private double automaticPanY;
        private bool webcamFeaturesReady;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Loaded += WebcamWindow_Loaded;
            Closing += WebcamWindow_Closing;
        }

        private void WebcamWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (webcamFeaturesReady)
            {
                return;
            }

            webcamFeaturesReady = true;
            RebuildPropertyInspector();
            InstallSensorAndAudioPanels();
            NeutralizeExpanderHeaders();
            HookFramingEvents();
            StartSensorTelemetry();
            ApplyCameraFraming();

            if (sensor != null)
            {
                sensor.AllFramesReady += WebcamSensor_AllFramesReady;
            }
        }

        private void WebcamWindow_Closing(object sender, CancelEventArgs e)
        {
            if (sensor != null)
            {
                sensor.AllFramesReady -= WebcamSensor_AllFramesReady;
            }

            if (sensorTelemetryTimer != null)
            {
                sensorTelemetryTimer.Stop();
            }

            if (tiltDebounceTimer != null)
            {
                tiltDebounceTimer.Stop();
            }

            StopMicrophoneMonitor();
        }

        private void RebuildPropertyInspector()
        {
            Expander camera = FindPropertyExpander("Camera");
            Expander tracking = FindPropertyExpander("Tracking");
            Expander heatmaps = FindPropertyExpander("Heatmaps");
            Expander render = FindPropertyExpander("3D Render");
            Expander output = FindPropertyExpander("Output");

            if (camera != null)
            {
                StackPanel panel = PreparePropertyPanel(camera);
                panel.Children.Add(CreateSectionLabel("Framing"));

                autoFrameToggle = CreateInspectorToggle("Auto-frame tracked person", false);
                panel.Children.Add(autoFrameToggle);
                autoFrameStatusText = CreateHintText("Manual framing");
                panel.Children.Add(autoFrameStatusText);

                cameraZoomSlider = CreateSlider(1.0, 3.0, 1.0, 0.05, false);
                cameraZoomValueText = CreateValueText("1.00×");
                panel.Children.Add(CreateSliderRow("Digital zoom", cameraZoomSlider, cameraZoomValueText));

                cameraPanXSlider = CreateSlider(-220.0, 220.0, 0.0, 5.0, false);
                cameraPanXValueText = CreateValueText("0 px");
                panel.Children.Add(CreateSliderRow("Horizontal pan", cameraPanXSlider, cameraPanXValueText));

                cameraPanYSlider = CreateSlider(-160.0, 160.0, 0.0, 5.0, false);
                cameraPanYValueText = CreateValueText("0 px");
                panel.Children.Add(CreateSliderRow("Vertical pan", cameraPanYSlider, cameraPanYValueText));

                Grid framingButtons = CreateTwoButtonRow(
                    "Head & shoulders",
                    delegate
                    {
                        cameraZoomSlider.Value = 1.35;
                        autoFrameToggle.IsChecked = true;
                    },
                    "Reset framing",
                    delegate
                    {
                        autoFrameToggle.IsChecked = false;
                        cameraZoomSlider.Value = 1.0;
                        cameraPanXSlider.Value = 0.0;
                        cameraPanYSlider.Value = 0.0;
                        automaticPanX = 0.0;
                        automaticPanY = 0.0;
                        ApplyCameraFraming();
                    });
                panel.Children.Add(framingButtons);

                panel.Children.Add(CreateSectionLabel("Image"));
                panel.Children.Add(MirrorToggle);
                panel.Children.Add(FreezeToggle);
                panel.Children.Add(FocusCameraToggle);
                panel.Children.Add(CreateSliderRow("Brightness", BrightnessSlider, BrightnessValueText));
                panel.Children.Add(CreateSliderRow("Contrast", ContrastSlider, ContrastValueText));
                panel.Children.Add(CreateInspectorButton("Reset camera", ResetCameraButton_Click));
            }

            if (tracking != null)
            {
                StackPanel panel = PreparePropertyPanel(tracking);
                panel.Children.Add(SeatedModeToggle);
                panel.Children.Add(CreateSliderRow("Smoothing", SmoothingSlider, SmoothingValueText));
                panel.Children.Add(CreateInspectorButton("Reset tracking filters", ResetFiltersButton_Click));
            }

            if (heatmaps != null)
            {
                StackPanel panel = PreparePropertyPanel(heatmaps);
                panel.Children.Add(CreateSliderRow("Motion threshold", MotionThresholdSlider, MotionThresholdValueText));
                panel.Children.Add(CreateSliderRow("Trail persistence", MotionPersistenceSlider, MotionPersistenceValueText));
                panel.Children.Add(CreateInspectorButton("Clear motion heat", ClearHeatmapButton_Click));
            }

            if (render != null)
            {
                StackPanel panel = PreparePropertyPanel(render);
                panel.Children.Add(BodyOnlyToggle);
                panel.Children.Add(PointCloudShadingToggle);
                panel.Children.Add(CreateSliderRow("Detail", PointCloudDetailSlider, PointCloudDetailValueText));
                panel.Children.Add(CreateSliderRow("Point size", PointCloudPointSizeSlider, PointCloudPointSizeValueText));
                panel.Children.Add(CreateInspectorButton("Reset 3D view", ResetPointCloudViewButton_Click));
            }

            if (output != null)
            {
                StackPanel panel = PreparePropertyPanel(output);
                panel.Children.Add(CreateSliderRow("Output size", OutputScaleSlider, OutputResolutionText));
                panel.Children.Add(CreateSliderRow("Video FPS", VideoFpsSlider, VideoFpsValueText));
                panel.Children.Add(CreateSliderRow("JPEG quality", VideoQualitySlider, VideoQualityValueText));
                panel.Children.Add(CreateHintText("MJPEG AVI • video only • scaling cannot add sensor detail"));
            }
        }

        private void InstallSensorAndAudioPanels()
        {
            Expander camera = FindPropertyExpander("Camera");
            StackPanel root = camera == null ? null : camera.Parent as StackPanel;
            if (root == null)
            {
                return;
            }

            int insertIndex = root.Children.IndexOf(camera) + 1;

            Expander sensorPanel = new Expander
            {
                Header = "Kinect Sensor",
                IsExpanded = true
            };
            StackPanel sensorContent = new StackPanel { Margin = new Thickness(8) };
            sensorPanel.Content = sensorContent;

            sensorTiltSlider = CreateSlider(-27.0, 27.0, 0.0, 1.0, true);
            sensorTiltValueText = CreateValueText("0°");
            sensorContent.Children.Add(CreateSliderRow("Motor tilt", sensorTiltSlider, sensorTiltValueText));
            sensorContent.Children.Add(CreateThreeButtonRow(
                "−5°",
                delegate { SetTiltRelative(-5); },
                "Level",
                delegate { SetTiltAbsolute(0); },
                "+5°",
                delegate { SetTiltRelative(5); }));
            accelerometerText = CreateHintText("Accelerometer: waiting for sensor");
            sensorContent.Children.Add(accelerometerText);
            sensorContent.Children.Add(CreateHintText("Motor range is limited to −27° through +27°. Changes are debounced to protect the mechanism."));

            Expander audioPanel = new Expander
            {
                Header = "Microphone Array",
                IsExpanded = false
            };
            StackPanel audioContent = new StackPanel { Margin = new Thickness(8) };
            audioPanel.Content = audioContent;

            microphoneToggle = CreateInspectorToggle("Enable microphone monitor", false);
            microphoneToggle.Checked += MicrophoneToggle_Changed;
            microphoneToggle.Unchecked += MicrophoneToggle_Changed;
            audioContent.Children.Add(microphoneToggle);

            microphoneLevelMeter = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Height = 10,
                Margin = new Thickness(0, 8, 0, 5),
                Background = new SolidColorBrush(Color.FromRgb(31, 34, 40)),
                Foreground = new SolidColorBrush(Color.FromRgb(76, 141, 255))
            };
            audioContent.Children.Add(microphoneLevelMeter);

            microphoneLevelText = CreateHintText("Level: off");
            microphoneDirectionText = CreateHintText("Sound direction: unavailable");
            audioContent.Children.Add(microphoneLevelText);
            audioContent.Children.Add(microphoneDirectionText);
            audioContent.Children.Add(CreateHintText("Uses the Kinect microphone array locally. Audio is monitored for level and direction only; AVI recording remains video-only."));

            root.Children.Insert(insertIndex, sensorPanel);
            root.Children.Insert(insertIndex + 1, audioPanel);
        }

        private void HookFramingEvents()
        {
            cameraZoomSlider.ValueChanged += FramingSlider_ValueChanged;
            cameraPanXSlider.ValueChanged += FramingSlider_ValueChanged;
            cameraPanYSlider.ValueChanged += FramingSlider_ValueChanged;
            autoFrameToggle.Checked += AutoFrameToggle_Changed;
            autoFrameToggle.Unchecked += AutoFrameToggle_Changed;
            MirrorToggle.Checked += MirrorFraming_Changed;
            MirrorToggle.Unchecked += MirrorFraming_Changed;
        }

        private void FramingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateFramingLabels();
            ApplyCameraFraming();
        }

        private void AutoFrameToggle_Changed(object sender, RoutedEventArgs e)
        {
            bool automatic = autoFrameToggle.IsChecked == true;
            cameraPanXSlider.IsEnabled = !automatic;
            cameraPanYSlider.IsEnabled = !automatic;
            autoFrameStatusText.Text = automatic ? "Following the tracked upper body" : "Manual framing";

            if (!automatic)
            {
                automaticPanX = 0.0;
                automaticPanY = 0.0;
            }

            ApplyCameraFraming();
        }

        private void MirrorFraming_Changed(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(ApplyCameraFraming), DispatcherPriority.Background);
        }

        private void WebcamSensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (autoFrameToggle == null || autoFrameToggle.IsChecked != true || skeletons == null || sensor == null)
            {
                return;
            }

            Skeleton tracked = skeletons.FirstOrDefault(item => item.TrackingState == SkeletonTrackingState.Tracked);
            if (tracked == null)
            {
                automaticPanX *= 0.94;
                automaticPanY *= 0.94;
                ApplyCameraFraming();
                return;
            }

            Joint anchor = tracked.Joints[JointType.ShoulderCenter];
            if (anchor.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            ColorImagePoint point = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(
                anchor.Position,
                ColorImageFormat.RgbResolution640x480Fps30);

            double visibleX = MirrorToggle.IsChecked == true ? 640.0 - point.X : point.X;
            double desiredPanX = Clamp(320.0 - visibleX, -220.0, 220.0);
            double desiredPanY = Clamp(255.0 - point.Y, -150.0, 150.0);

            automaticPanX += (desiredPanX - automaticPanX) * 0.10;
            automaticPanY += (desiredPanY - automaticPanY) * 0.10;
            ApplyCameraFraming();
        }

        private void ApplyCameraFraming()
        {
            if (CameraSurface == null || cameraZoomSlider == null)
            {
                return;
            }

            double zoom = cameraZoomSlider.Value;
            double panX = autoFrameToggle != null && autoFrameToggle.IsChecked == true
                ? automaticPanX
                : cameraPanXSlider.Value;
            double panY = autoFrameToggle != null && autoFrameToggle.IsChecked == true
                ? automaticPanY
                : cameraPanYSlider.Value;

            TransformGroup transforms = new TransformGroup();
            transforms.Children.Add(new ScaleTransform(
                MirrorToggle.IsChecked == true ? -zoom : zoom,
                zoom,
                320.0,
                240.0));
            transforms.Children.Add(new TranslateTransform(panX, panY));
            CameraSurface.RenderTransform = transforms;
            UpdateFramingLabels();
        }

        private void UpdateFramingLabels()
        {
            if (cameraZoomValueText != null)
            {
                cameraZoomValueText.Text = cameraZoomSlider.Value.ToString("0.00") + "×";
            }

            if (cameraPanXValueText != null)
            {
                cameraPanXValueText.Text = Math.Round(cameraPanXSlider.Value).ToString("0") + " px";
            }

            if (cameraPanYValueText != null)
            {
                cameraPanYValueText.Text = Math.Round(cameraPanYSlider.Value).ToString("0") + " px";
            }
        }

        private void StartSensorTelemetry()
        {
            sensorTelemetryTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            sensorTelemetryTimer.Tick += SensorTelemetryTimer_Tick;
            sensorTelemetryTimer.Start();

            tiltDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(450)
            };
            tiltDebounceTimer.Tick += TiltDebounceTimer_Tick;
            sensorTiltSlider.ValueChanged += SensorTiltSlider_ValueChanged;

            RefreshSensorTelemetry();
        }

        private void SensorTelemetryTimer_Tick(object sender, EventArgs e)
        {
            RefreshSensorTelemetry();
        }

        private void RefreshSensorTelemetry()
        {
            if (sensor == null || !sensor.IsRunning)
            {
                if (accelerometerText != null)
                {
                    accelerometerText.Text = "Accelerometer: sensor unavailable";
                }

                return;
            }

            try
            {
                suppressTiltChanges = true;
                sensorTiltSlider.Value = sensor.ElevationAngle;
                sensorTiltValueText.Text = sensor.ElevationAngle.ToString("0") + "°";
                suppressTiltChanges = false;

                Vector4 acceleration = sensor.AccelerometerGetCurrentReading();
                accelerometerText.Text = string.Format(
                    "Accelerometer  X {0:0.00}   Y {1:0.00}   Z {2:0.00}",
                    acceleration.X,
                    acceleration.Y,
                    acceleration.Z);

                if (microphoneToggle != null && microphoneToggle.IsChecked == true)
                {
                    KinectAudioSource audio = sensor.AudioSource;
                    microphoneDirectionText.Text = string.Format(
                        "Sound {0:0}°  •  confidence {1:P0}  •  beam {2:0}°",
                        audio.SoundSourceAngle,
                        audio.SoundSourceAngleConfidence,
                        audio.BeamAngle);
                }
            }
            catch (InvalidOperationException exception)
            {
                accelerometerText.Text = "Sensor telemetry unavailable: " + exception.Message;
            }
            finally
            {
                suppressTiltChanges = false;
            }
        }

        private void SensorTiltSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (suppressTiltChanges || tiltDebounceTimer == null)
            {
                return;
            }

            sensorTiltValueText.Text = Math.Round(sensorTiltSlider.Value).ToString("0") + "°";
            tiltDebounceTimer.Stop();
            tiltDebounceTimer.Start();
        }

        private void TiltDebounceTimer_Tick(object sender, EventArgs e)
        {
            tiltDebounceTimer.Stop();
            SetTiltAbsolute((int)Math.Round(sensorTiltSlider.Value));
        }

        private void SetTiltRelative(int change)
        {
            int current = sensor == null ? 0 : sensor.ElevationAngle;
            SetTiltAbsolute(current + change);
        }

        private void SetTiltAbsolute(int angle)
        {
            angle = Math.Max(-27, Math.Min(27, angle));

            if (sensor == null || !sensor.IsRunning)
            {
                StatusBarMessageText.Text = "Kinect motor is unavailable";
                return;
            }

            try
            {
                sensor.ElevationAngle = angle;
                suppressTiltChanges = true;
                sensorTiltSlider.Value = angle;
                sensorTiltValueText.Text = angle.ToString("0") + "°";
                suppressTiltChanges = false;
                StatusBarMessageText.Text = "Kinect tilt set to " + angle + "°";
            }
            catch (InvalidOperationException exception)
            {
                StatusBarMessageText.Text = "Could not move Kinect: " + exception.Message;
            }
            finally
            {
                suppressTiltChanges = false;
            }
        }

        private void MicrophoneToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (microphoneToggle.IsChecked == true)
            {
                StartMicrophoneMonitor();
            }
            else
            {
                StopMicrophoneMonitor();
            }
        }

        private void StartMicrophoneMonitor()
        {
            if (microphoneRunning)
            {
                return;
            }

            if (sensor == null || !sensor.IsRunning)
            {
                microphoneToggle.IsChecked = false;
                StatusBarMessageText.Text = "Kinect microphone array is unavailable";
                return;
            }

            try
            {
                microphoneStream = sensor.AudioSource.Start();
                microphoneRunning = true;
                microphoneLevelText.Text = "Level: listening";

                microphoneThread = new Thread(MicrophoneReadLoop)
                {
                    IsBackground = true,
                    Name = "Kinect microphone meter"
                };
                microphoneThread.Start();
                StatusBarMessageText.Text = "Kinect microphone monitor enabled";
            }
            catch (Exception exception)
            {
                microphoneRunning = false;
                microphoneToggle.IsChecked = false;
                microphoneLevelText.Text = "Level: unavailable";
                StatusBarMessageText.Text = "Could not start microphone: " + exception.Message;
            }
        }

        private void StopMicrophoneMonitor()
        {
            microphoneRunning = false;

            try
            {
                if (microphoneStream != null)
                {
                    microphoneStream.Dispose();
                    microphoneStream = null;
                }
            }
            catch
            {
            }

            try
            {
                if (sensor != null)
                {
                    sensor.AudioSource.Stop();
                }
            }
            catch
            {
            }

            if (microphoneThread != null && microphoneThread.IsAlive)
            {
                microphoneThread.Join(500);
            }

            microphoneThread = null;

            if (microphoneLevelMeter != null)
            {
                microphoneLevelMeter.Value = 0;
            }

            if (microphoneLevelText != null)
            {
                microphoneLevelText.Text = "Level: off";
            }

            if (microphoneDirectionText != null)
            {
                microphoneDirectionText.Text = "Sound direction: unavailable";
            }
        }

        private void MicrophoneReadLoop()
        {
            byte[] buffer = new byte[4096];
            DateTime lastUpdate = DateTime.MinValue;

            while (microphoneRunning && microphoneStream != null)
            {
                try
                {
                    int bytesRead = microphoneStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 1)
                    {
                        continue;
                    }

                    double sum = 0.0;
                    int samples = bytesRead / 2;
                    for (int index = 0; index + 1 < bytesRead; index += 2)
                    {
                        short sample = (short)(buffer[index] | (buffer[index + 1] << 8));
                        double normalized = sample / 32768.0;
                        sum += normalized * normalized;
                    }

                    double rms = Math.Sqrt(sum / Math.Max(1, samples));
                    double decibels = rms <= 0.000001 ? -60.0 : 20.0 * Math.Log10(rms);
                    double meter = Clamp((decibels + 60.0) / 60.0 * 100.0, 0.0, 100.0);

                    if ((DateTime.UtcNow - lastUpdate).TotalMilliseconds >= 90)
                    {
                        lastUpdate = DateTime.UtcNow;
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            if (microphoneLevelMeter != null)
                            {
                                microphoneLevelMeter.Value = meter;
                                microphoneLevelText.Text = "Level: " + decibels.ToString("0.0") + " dBFS";
                            }
                        }));
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (IOException)
                {
                    break;
                }
            }
        }

        private void NeutralizeExpanderHeaders()
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                List<Expander> expanders = new List<Expander>();
                CollectExpanders(this, expanders);
                foreach (Expander expander in expanders)
                {
                    expander.ApplyTemplate();
                    ToggleButton header = FindVisualChild<ToggleButton>(expander);
                    if (header != null)
                    {
                        header.Template = CreateFlatHeaderTemplate();
                        header.Background = new SolidColorBrush(Color.FromRgb(37, 40, 46));
                        header.BorderBrush = new SolidColorBrush(Color.FromRgb(55, 59, 67));
                        header.Foreground = new SolidColorBrush(Color.FromRgb(225, 227, 231));
                    }
                }
            }), DispatcherPriority.Loaded);
        }

        private static ControlTemplate CreateFlatHeaderTemplate()
        {
            ControlTemplate template = new ControlTemplate(typeof(ToggleButton));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "HeaderBorder";
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetValue(Border.PaddingProperty, new Thickness(9, 7, 9, 7));

            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentControl.ContentProperty));
            presenter.SetValue(ContentPresenter.ContentTemplateProperty, new TemplateBindingExtension(ContentControl.ContentTemplateProperty));
            presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);
            template.VisualTree = border;

            Trigger hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(47, 51, 59)), "HeaderBorder"));
            template.Triggers.Add(hover);

            Trigger selected = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
            selected.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(41, 45, 52)), "HeaderBorder"));
            selected.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(69, 75, 86)), "HeaderBorder"));
            template.Triggers.Add(selected);
            return template;
        }

        private Expander FindPropertyExpander(string header)
        {
            List<Expander> expanders = new List<Expander>();
            CollectExpanders(this, expanders);
            return expanders.FirstOrDefault(item => string.Equals(Convert.ToString(item.Header), header, StringComparison.OrdinalIgnoreCase));
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
            {
                return null;
            }

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int index = 0; index < count; index++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, index);
                T match = child as T;
                if (match != null)
                {
                    return match;
                }

                match = FindVisualChild<T>(child);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static StackPanel PreparePropertyPanel(Expander expander)
        {
            StackPanel panel = expander.Content as StackPanel;
            if (panel == null)
            {
                panel = new StackPanel();
                expander.Content = panel;
            }

            panel.Children.Clear();
            panel.Margin = new Thickness(8);
            return panel;
        }

        private static TextBlock CreateSectionLabel(string text)
        {
            return new TextBlock
            {
                Text = text.ToUpperInvariant(),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(126, 132, 143)),
                Margin = new Thickness(0, 4, 0, 7)
            };
        }

        private static TextBlock CreateValueText(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(Color.FromRgb(215, 218, 223)),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
        }

        private static TextBlock CreateHintText(string text)
        {
            return new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(125, 130, 139)),
                Margin = new Thickness(0, 3, 0, 7)
            };
        }

        private static Slider CreateSlider(double minimum, double maximum, double value, double tick, bool snap)
        {
            return new Slider
            {
                Minimum = minimum,
                Maximum = maximum,
                Value = value,
                TickFrequency = tick,
                IsSnapToTickEnabled = snap,
                Margin = new Thickness(0, 2, 0, 8)
            };
        }

        private static Grid CreateSliderRow(string label, Slider slider, TextBlock value)
        {
            RemoveFromParent(slider);
            RemoveFromParent(value);
            value.Margin = new Thickness(8, 0, 0, 0);

            Grid row = new Grid { Margin = new Thickness(0, 2, 0, 3) };
            row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock labelText = new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(Color.FromRgb(166, 170, 178)),
                VerticalAlignment = VerticalAlignment.Center
            };
            row.Children.Add(labelText);
            Grid.SetColumn(value, 1);
            row.Children.Add(value);
            Grid.SetRow(slider, 1);
            Grid.SetColumnSpan(slider, 2);
            row.Children.Add(slider);
            return row;
        }

        private static ToggleButton CreateInspectorToggle(string text, bool value)
        {
            return new ToggleButton
            {
                Content = text,
                IsChecked = value,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 0, 5)
            };
        }

        private static Button CreateInspectorButton(string text, RoutedEventHandler handler)
        {
            Button button = new Button
            {
                Content = text,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 4, 0, 0)
            };
            button.Click += handler;
            return button;
        }

        private static Grid CreateTwoButtonRow(string firstText, RoutedEventHandler firstHandler, string secondText, RoutedEventHandler secondHandler)
        {
            Grid grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Button first = CreateInspectorButton(firstText, firstHandler);
            first.Margin = new Thickness(0);
            Button second = CreateInspectorButton(secondText, secondHandler);
            second.Margin = new Thickness(0);
            Grid.SetColumn(second, 2);
            grid.Children.Add(first);
            grid.Children.Add(second);
            return grid;
        }

        private static Grid CreateThreeButtonRow(
            string firstText,
            RoutedEventHandler firstHandler,
            string secondText,
            RoutedEventHandler secondHandler,
            string thirdText,
            RoutedEventHandler thirdHandler)
        {
            Grid grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            for (int index = 0; index < 5; index++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = index == 1 || index == 3 ? new GridLength(5) : new GridLength(1, GridUnitType.Star)
                });
            }

            Button first = CreateInspectorButton(firstText, firstHandler);
            Button second = CreateInspectorButton(secondText, secondHandler);
            Button third = CreateInspectorButton(thirdText, thirdHandler);
            first.Margin = second.Margin = third.Margin = new Thickness(0);
            Grid.SetColumn(second, 2);
            Grid.SetColumn(third, 4);
            grid.Children.Add(first);
            grid.Children.Add(second);
            grid.Children.Add(third);
            return grid;
        }

        private static void RemoveFromParent(UIElement element)
        {
            if (element == null)
            {
                return;
            }

            Panel panel = VisualTreeHelper.GetParent(element) as Panel;
            if (panel != null)
            {
                panel.Children.Remove(element);
                return;
            }

            ContentControl content = VisualTreeHelper.GetParent(element) as ContentControl;
            if (content != null && ReferenceEquals(content.Content, element))
            {
                content.Content = null;
            }
        }
    }
}
