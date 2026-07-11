using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using KinectReframe.Rendering;
using KinectReframe.Services;
using KinectReframe.Tracking;

namespace KinectReframe
{
    public partial class MainWindow : Window
    {
        private readonly JointSmoother jointSmoother = new JointSmoother();
        private readonly SessionRecorder recorder = new SessionRecorder();
        private readonly Stopwatch fpsClock = Stopwatch.StartNew();

        private KinectSensor sensor;
        private WriteableBitmap colorBitmap;
        private WriteableBitmap bodyBitmap;
        private WriteableBitmap pointCloudBitmap;
        private WriteableBitmap motionHeatmapBitmap;
        private WriteableBitmap depthHeatmapBitmap;
        private byte[] colorPixels;
        private byte[] adjustedColorPixels;
        private DepthImagePixel[] depthPixels;
        private SkeletonPoint[] pointCloudPoints;
        private Skeleton[] skeletons;
        private BodyDepthRenderer bodyRenderer;
        private PointCloudRenderer pointCloudRenderer;
        private HeatmapRenderer heatmapRenderer;
        private int framesSinceFpsUpdate;
        private bool pointCloudDragging;
        private bool suppressHeatmapToggleEvents;
        private bool uiReady;
        private Point lastPointCloudMousePosition;
        private double pointCloudYaw;
        private double pointCloudPitch;
        private double pointCloudZoom = 1.0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            uiReady = true;
            UpdatePointCloudViewText();
            ApplyVisualSettings();
            UpdateAdjustmentLabels();
            StartSensor();
        }

        private void StartSensor()
        {
            sensor = KinectSensor.KinectSensors.FirstOrDefault(candidate => candidate.Status == KinectStatus.Connected);

            if (sensor == null)
            {
                SetConnectionState(false, "No connected Kinect found");
                TrackingStatusText.Text = "Check USB, external power and Kinect SDK 1.8";
                return;
            }

            try
            {
                sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                sensor.SkeletonStream.Enable();
                sensor.SkeletonStream.TrackingMode = SeatedModeToggle.IsChecked == true
                    ? SkeletonTrackingMode.Seated
                    : SkeletonTrackingMode.Default;

                colorPixels = new byte[sensor.ColorStream.FramePixelDataLength];
                adjustedColorPixels = new byte[sensor.ColorStream.FramePixelDataLength];
                depthPixels = new DepthImagePixel[sensor.DepthStream.FramePixelDataLength];
                pointCloudPoints = new SkeletonPoint[sensor.DepthStream.FramePixelDataLength];
                skeletons = new Skeleton[sensor.SkeletonStream.FrameSkeletonArrayLength];

                colorBitmap = new WriteableBitmap(
                    sensor.ColorStream.FrameWidth,
                    sensor.ColorStream.FrameHeight,
                    96,
                    96,
                    PixelFormats.Bgr32,
                    null);

                bodyBitmap = new WriteableBitmap(
                    sensor.DepthStream.FrameWidth,
                    sensor.DepthStream.FrameHeight,
                    96,
                    96,
                    PixelFormats.Bgra32,
                    null);

                motionHeatmapBitmap = new WriteableBitmap(
                    sensor.DepthStream.FrameWidth,
                    sensor.DepthStream.FrameHeight,
                    96,
                    96,
                    PixelFormats.Bgra32,
                    null);

                depthHeatmapBitmap = new WriteableBitmap(
                    sensor.DepthStream.FrameWidth,
                    sensor.DepthStream.FrameHeight,
                    96,
                    96,
                    PixelFormats.Bgra32,
                    null);

                const int pointCloudWidth = 640;
                const int pointCloudHeight = 480;
                pointCloudBitmap = new WriteableBitmap(
                    pointCloudWidth,
                    pointCloudHeight,
                    96,
                    96,
                    PixelFormats.Bgra32,
                    null);

                bodyRenderer = new BodyDepthRenderer(sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                heatmapRenderer = new HeatmapRenderer(sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight);
                pointCloudRenderer = new PointCloudRenderer(
                    sensor.DepthStream.FrameWidth,
                    sensor.DepthStream.FrameHeight,
                    pointCloudWidth,
                    pointCloudHeight);

                ColorImage.Source = colorBitmap;
                BodyImage.Source = bodyBitmap;
                MotionHeatmapImage.Source = motionHeatmapBitmap;
                DepthHeatmapImage.Source = depthHeatmapBitmap;
                PointCloudImage.Source = pointCloudBitmap;

                sensor.AllFramesReady += Sensor_AllFramesReady;
                sensor.Start();

                SetConnectionState(true, "Kinect connected");
            }
            catch (IOException exception)
            {
                SetConnectionState(false, "Kinect is in use by another app");
                TrackingStatusText.Text = exception.Message;
            }
            catch (InvalidOperationException exception)
            {
                SetConnectionState(false, "Could not start Kinect");
                TrackingStatusText.Text = exception.Message;
            }
        }

        private void Sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (FreezeToggle.IsChecked == true)
            {
                return;
            }

            UpdateColorFrame(e);
            UpdateDepthFrame(e);
            UpdateSkeletonFrame(e);
            UpdateFps();
        }

        private void UpdateColorFrame(AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame == null)
                {
                    return;
                }

                frame.CopyPixelDataTo(colorPixels);
                byte[] displayPixels = ApplyCameraAdjustments(colorPixels);

                colorBitmap.WritePixels(
                    new Int32Rect(0, 0, frame.Width, frame.Height),
                    displayPixels,
                    frame.Width * frame.BytesPerPixel,
                    0);
            }
        }

        private byte[] ApplyCameraAdjustments(byte[] source)
        {
            int brightness = BrightnessSlider == null ? 0 : (int)Math.Round(BrightnessSlider.Value);
            double contrast = ContrastSlider == null ? 1.0 : ContrastSlider.Value;

            if (brightness == 0 && Math.Abs(contrast - 1.0) < 0.001)
            {
                return source;
            }

            for (int i = 0; i < source.Length; i += 4)
            {
                adjustedColorPixels[i] = AdjustChannel(source[i], brightness, contrast);
                adjustedColorPixels[i + 1] = AdjustChannel(source[i + 1], brightness, contrast);
                adjustedColorPixels[i + 2] = AdjustChannel(source[i + 2], brightness, contrast);
                adjustedColorPixels[i + 3] = source[i + 3];
            }

            return adjustedColorPixels;
        }

        private static byte AdjustChannel(byte value, int brightness, double contrast)
        {
            double adjusted = ((value - 128.0) * contrast) + 128.0 + brightness;
            return (byte)Math.Max(0.0, Math.Min(255.0, adjusted));
        }

        private void UpdateDepthFrame(AllFramesReadyEventArgs e)
        {
            using (DepthImageFrame frame = e.OpenDepthImageFrame())
            {
                if (frame == null)
                {
                    return;
                }

                frame.CopyDepthImagePixelDataTo(depthPixels);
                bool bodyOnly = BodyOnlyToggle.IsChecked == true;
                byte[] renderedPixels = bodyRenderer.Render(depthPixels, bodyOnly);

                bodyBitmap.WritePixels(
                    new Int32Rect(0, 0, frame.Width, frame.Height),
                    renderedPixels,
                    frame.Width * 4,
                    0);

                NoBodyText.Visibility = bodyRenderer.BodyPixelCount > 80
                    ? Visibility.Collapsed
                    : Visibility.Visible;

                if (MotionHeatToggle.IsChecked == true)
                {
                    byte[] heatPixels = heatmapRenderer.RenderMotion(
                        depthPixels,
                        bodyOnly,
                        (int)Math.Round(MotionThresholdSlider.Value),
                        MotionPersistenceSlider.Value);

                    motionHeatmapBitmap.WritePixels(
                        new Int32Rect(0, 0, frame.Width, frame.Height),
                        heatPixels,
                        frame.Width * 4,
                        0);
                }

                if (DepthHeatToggle.IsChecked == true)
                {
                    byte[] depthColours = heatmapRenderer.RenderDepth(depthPixels, bodyOnly);
                    depthHeatmapBitmap.WritePixels(
                        new Int32Rect(0, 0, frame.Width, frame.Height),
                        depthColours,
                        frame.Width * 4,
                        0);
                }

                sensor.CoordinateMapper.MapDepthFrameToSkeletonFrame(
                    DepthImageFormat.Resolution320x240Fps30,
                    depthPixels,
                    pointCloudPoints);

                byte[] pointCloudPixels = pointCloudRenderer.Render(
                    depthPixels,
                    pointCloudPoints,
                    bodyOnly,
                    pointCloudYaw,
                    pointCloudPitch,
                    pointCloudZoom);

                pointCloudBitmap.WritePixels(
                    new Int32Rect(0, 0, pointCloudBitmap.PixelWidth, pointCloudBitmap.PixelHeight),
                    pointCloudPixels,
                    pointCloudBitmap.PixelWidth * 4,
                    0);

                PointCloudCountText.Text = pointCloudRenderer.PointCount.ToString("N0") + " sampled points";
            }
        }

        private void UpdateSkeletonFrame(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                {
                    return;
                }

                frame.CopySkeletonDataTo(skeletons);
                Skeleton trackedSkeleton = skeletons.FirstOrDefault(item => item.TrackingState == SkeletonTrackingState.Tracked);

                if (trackedSkeleton == null)
                {
                    SkeletonCanvas.Children.Clear();
                    TrackingStatusText.Text = "No skeleton tracked";
                    return;
                }

                SmoothedSkeleton smoothedSkeleton = jointSmoother.Update(trackedSkeleton, SmoothingSlider.Value);

                if (SkeletonToggle.IsChecked == true)
                {
                    SkeletonRenderer.Draw(
                        SkeletonCanvas,
                        sensor,
                        trackedSkeleton,
                        smoothedSkeleton,
                        RawSkeletonToggle.IsChecked == true);
                }
                else
                {
                    SkeletonCanvas.Children.Clear();
                }

                int trackedJointCount = trackedSkeleton.Joints.Count(joint => joint.TrackingState == JointTrackingState.Tracked);
                int inferredJointCount = trackedSkeleton.Joints.Count(joint => joint.TrackingState == JointTrackingState.Inferred);
                TrackingStatusText.Text = string.Format(
                    "Tracking ID {0}  |  {1} tracked joints  |  {2} inferred",
                    trackedSkeleton.TrackingId,
                    trackedJointCount,
                    inferredJointCount);

                if (recorder.IsRecording)
                {
                    recorder.Capture(trackedSkeleton, smoothedSkeleton);
                }
            }
        }

        private void UpdateFps()
        {
            framesSinceFpsUpdate++;
            if (fpsClock.ElapsedMilliseconds < 1000)
            {
                return;
            }

            double fps = framesSinceFpsUpdate * 1000.0 / fpsClock.ElapsedMilliseconds;
            FpsText.Text = fps.ToString("0.0");
            framesSinceFpsUpdate = 0;
            fpsClock.Restart();
        }

        private void SeatedModeToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (sensor == null || !sensor.IsRunning)
            {
                return;
            }

            sensor.SkeletonStream.TrackingMode = SeatedModeToggle.IsChecked == true
                ? SkeletonTrackingMode.Seated
                : SkeletonTrackingMode.Default;
            jointSmoother.Reset();
        }

        private void VisualToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (!uiReady)
            {
                return;
            }

            ApplyVisualSettings();
        }

        private void ApplyVisualSettings()
        {
            if (SkeletonCanvas == null)
            {
                return;
            }

            bool skeletonVisible = SkeletonToggle.IsChecked == true;
            SkeletonCanvas.Visibility = skeletonVisible ? Visibility.Visible : Visibility.Collapsed;
            RawSkeletonToggle.IsEnabled = skeletonVisible;

            if (!skeletonVisible)
            {
                SkeletonCanvas.Children.Clear();
            }

            GridOverlayCanvas.Visibility = GridToggle.IsChecked == true
                ? Visibility.Visible
                : Visibility.Collapsed;

            FreezeBadge.Visibility = FreezeToggle.IsChecked == true
                ? Visibility.Visible
                : Visibility.Collapsed;

            CameraSurface.RenderTransform = MirrorToggle.IsChecked == true
                ? new ScaleTransform(-1.0, 1.0)
                : Transform.Identity;

            bool focusCamera = FocusCameraToggle.IsChecked == true;
            RightPanelBorder.Visibility = focusCamera ? Visibility.Collapsed : Visibility.Visible;
            RightPanelSpacerColumn.Width = focusCamera ? new GridLength(0) : new GridLength(16);
            RightPanelColumn.Width = focusCamera ? new GridLength(0) : new GridLength(2, GridUnitType.Star);

            UpdateCameraModeText();
        }

        private void HeatmapToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (!uiReady || suppressHeatmapToggleEvents)
            {
                return;
            }

            suppressHeatmapToggleEvents = true;

            if (sender == MotionHeatToggle && MotionHeatToggle.IsChecked == true)
            {
                DepthHeatToggle.IsChecked = false;
            }
            else if (sender == DepthHeatToggle && DepthHeatToggle.IsChecked == true)
            {
                MotionHeatToggle.IsChecked = false;
            }

            suppressHeatmapToggleEvents = false;

            MotionHeatmapImage.Visibility = MotionHeatToggle.IsChecked == true
                ? Visibility.Visible
                : Visibility.Collapsed;
            DepthHeatmapImage.Visibility = DepthHeatToggle.IsChecked == true
                ? Visibility.Visible
                : Visibility.Collapsed;

            UpdateCameraModeText();
        }

        private void UpdateCameraModeText()
        {
            if (CameraModeText == null)
            {
                return;
            }

            string mode = "RGB CAMERA";
            if (MotionHeatToggle.IsChecked == true)
            {
                mode += " + MOTION HEAT";
            }
            else if (DepthHeatToggle.IsChecked == true)
            {
                mode += " + DEPTH HEAT";
            }

            if (SkeletonToggle.IsChecked == true)
            {
                mode += " + SKELETON";
            }

            CameraModeText.Text = mode;
        }

        private void CameraAdjustmentSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateAdjustmentLabels();
        }

        private void SmoothingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateAdjustmentLabels();
        }

        private void HeatmapSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateAdjustmentLabels();
        }

        private void UpdateAdjustmentLabels()
        {
            if (BrightnessValueText != null)
            {
                BrightnessValueText.Text = Math.Round(BrightnessSlider.Value).ToString("0");
            }

            if (ContrastValueText != null)
            {
                ContrastValueText.Text = ContrastSlider.Value.ToString("0.00");
            }

            if (SmoothingValueText != null)
            {
                SmoothingValueText.Text = SmoothingSlider.Value.ToString("0.00");
            }

            if (MotionThresholdValueText != null)
            {
                MotionThresholdValueText.Text = Math.Round(MotionThresholdSlider.Value).ToString("0") + " mm";
            }

            if (MotionPersistenceValueText != null)
            {
                MotionPersistenceValueText.Text = MotionPersistenceSlider.Value.ToString("0.000");
            }
        }

        private void ClearHeatmapButton_Click(object sender, RoutedEventArgs e)
        {
            if (heatmapRenderer != null)
            {
                heatmapRenderer.ClearMotion();
            }

            ClearBitmap(motionHeatmapBitmap);
            TrackingStatusText.Text = "Motion heat cleared";
        }

        private static void ClearBitmap(WriteableBitmap bitmap)
        {
            if (bitmap == null)
            {
                return;
            }

            byte[] empty = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
            bitmap.WritePixels(
                new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
                empty,
                bitmap.PixelWidth * 4,
                0);
        }

        private void ResetCameraButton_Click(object sender, RoutedEventArgs e)
        {
            BrightnessSlider.Value = 0;
            ContrastSlider.Value = 1.0;
            MirrorToggle.IsChecked = true;
            FreezeToggle.IsChecked = false;
            GridToggle.IsChecked = false;
            FocusCameraToggle.IsChecked = false;
            MotionHeatToggle.IsChecked = false;
            DepthHeatToggle.IsChecked = false;
            ClearHeatmapButton_Click(sender, e);
            ApplyVisualSettings();
            TrackingStatusText.Text = "Camera view reset";
        }

        private void PointCloudViewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            pointCloudDragging = true;
            lastPointCloudMousePosition = e.GetPosition(PointCloudViewport);
            PointCloudViewport.CaptureMouse();
            e.Handled = true;
        }

        private void PointCloudViewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            pointCloudDragging = false;
            PointCloudViewport.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void PointCloudViewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (!pointCloudDragging || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point currentPosition = e.GetPosition(PointCloudViewport);
            Vector delta = currentPosition - lastPointCloudMousePosition;
            lastPointCloudMousePosition = currentPosition;

            pointCloudYaw += delta.X * 0.35;
            pointCloudPitch = Math.Max(-85.0, Math.Min(85.0, pointCloudPitch - (delta.Y * 0.35)));
            UpdatePointCloudViewText();
            e.Handled = true;
        }

        private void PointCloudViewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            pointCloudZoom *= e.Delta > 0 ? 1.1 : (1.0 / 1.1);
            pointCloudZoom = Math.Max(0.35, Math.Min(3.0, pointCloudZoom));
            UpdatePointCloudViewText();
            e.Handled = true;
        }

        private void ResetPointCloudViewButton_Click(object sender, RoutedEventArgs e)
        {
            pointCloudYaw = 0.0;
            pointCloudPitch = 0.0;
            pointCloudZoom = 1.0;
            UpdatePointCloudViewText();
        }

        private void UpdatePointCloudViewText()
        {
            if (PointCloudViewText == null)
            {
                return;
            }

            PointCloudViewText.Text = string.Format(
                "Yaw {0:0}°  Pitch {1:0}°  Zoom {2:0.00}x",
                pointCloudYaw,
                pointCloudPitch,
                pointCloudZoom);
        }

        private void ExportPointCloudButton_Click(object sender, RoutedEventArgs e)
        {
            if (depthPixels == null || pointCloudPoints == null || pointCloudRenderer == null)
            {
                TrackingStatusText.Text = "Wait for the first depth frame before exporting";
                return;
            }

            try
            {
                bool bodyOnly = BodyOnlyToggle.IsChecked == true;
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exports");
                string mode = bodyOnly ? "body" : "scene";
                string path = Path.Combine(
                    folder,
                    "kinect-reframe-" + mode + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".ply");

                int exportedPoints = PointCloudExporter.ExportPly(
                    path,
                    depthPixels,
                    pointCloudPoints,
                    bodyOnly,
                    2);

                TrackingStatusText.Text = string.Format("Exported {0:N0} points to {1}", exportedPoints, path);
            }
            catch (Exception exception)
            {
                TrackingStatusText.Text = "PLY export failed: " + exception.Message;
            }
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (!recorder.IsRecording)
            {
                recorder.Start(SeatedModeToggle.IsChecked == true);
                RecordButton.Content = "Stop and save";
                RecordButton.Background = new SolidColorBrush(Color.FromRgb(239, 115, 115));
                return;
            }

            SaveRecording();
        }

        private void SaveRecording()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recordings");
            string path = recorder.StopAndSave(folder);
            RecordButton.Content = "Start recording";
            RecordButton.ClearValue(BackgroundProperty);

            if (!string.IsNullOrWhiteSpace(path))
            {
                TrackingStatusText.Text = "Saved " + path;
            }
        }

        private void SnapshotButton_Click(object sender, RoutedEventArgs e)
        {
            SaveVisualSnapshot(RootVisual, "kinect-reframe-app");
        }

        private void CameraSnapshotButton_Click(object sender, RoutedEventArgs e)
        {
            SaveVisualSnapshot(CameraViewport, "kinect-reframe-camera");
        }

        private void SaveVisualSnapshot(Visual visual, string prefix)
        {
            FrameworkElement element = visual as FrameworkElement;
            if (element == null || element.ActualWidth < 1 || element.ActualHeight < 1)
            {
                TrackingStatusText.Text = "Nothing is ready to capture yet";
                return;
            }

            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "captures");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, prefix + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png");

            int width = Math.Max(1, (int)element.ActualWidth);
            int height = Math.Max(1, (int)element.ActualHeight);
            RenderTargetBitmap render = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            render.Render(visual);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(render));
            using (FileStream stream = File.Create(path))
            {
                encoder.Save(stream);
            }

            TrackingStatusText.Text = "Saved " + path;
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            jointSmoother.Reset();
            TrackingStatusText.Text = "Tracking filters reset";
        }

        private void SetConnectionState(bool connected, string text)
        {
            ConnectionIndicator.Fill = new SolidColorBrush(connected
                ? Color.FromRgb(38, 230, 184)
                : Color.FromRgb(216, 88, 88));
            ConnectionText.Text = text;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            uiReady = false;

            if (recorder.IsRecording)
            {
                SaveRecording();
            }

            if (sensor == null)
            {
                return;
            }

            sensor.AllFramesReady -= Sensor_AllFramesReady;
            if (sensor.IsRunning)
            {
                sensor.Stop();
            }

            sensor = null;
        }
    }
}
