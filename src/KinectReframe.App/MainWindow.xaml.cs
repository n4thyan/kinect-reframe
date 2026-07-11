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
        private byte[] colorPixels;
        private DepthImagePixel[] depthPixels;
        private SkeletonPoint[] pointCloudPoints;
        private Skeleton[] skeletons;
        private BodyDepthRenderer bodyRenderer;
        private PointCloudRenderer pointCloudRenderer;
        private int framesSinceFpsUpdate;
        private bool pointCloudDragging;
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
            UpdatePointCloudViewText();
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
                sensor.SkeletonStream.TrackingMode = SeatedModeCheckBox.IsChecked == true
                    ? SkeletonTrackingMode.Seated
                    : SkeletonTrackingMode.Default;

                colorPixels = new byte[sensor.ColorStream.FramePixelDataLength];
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
                pointCloudRenderer = new PointCloudRenderer(
                    sensor.DepthStream.FrameWidth,
                    sensor.DepthStream.FrameHeight,
                    pointCloudWidth,
                    pointCloudHeight);

                ColorImage.Source = colorBitmap;
                BodyImage.Source = bodyBitmap;
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
                colorBitmap.WritePixels(
                    new Int32Rect(0, 0, frame.Width, frame.Height),
                    colorPixels,
                    frame.Width * frame.BytesPerPixel,
                    0);
            }
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
                bool bodyOnly = BodyOnlyCheckBox.IsChecked == true;
                byte[] renderedPixels = bodyRenderer.Render(depthPixels, bodyOnly);

                bodyBitmap.WritePixels(
                    new Int32Rect(0, 0, frame.Width, frame.Height),
                    renderedPixels,
                    frame.Width * 4,
                    0);

                NoBodyText.Visibility = bodyRenderer.BodyPixelCount > 80
                    ? Visibility.Collapsed
                    : Visibility.Visible;

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
                SkeletonRenderer.Draw(
                    SkeletonCanvas,
                    sensor,
                    trackedSkeleton,
                    smoothedSkeleton,
                    ShowRawCheckBox.IsChecked == true);

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

        private void SeatedModeCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sensor == null || !sensor.IsRunning)
            {
                return;
            }

            sensor.SkeletonStream.TrackingMode = SeatedModeCheckBox.IsChecked == true
                ? SkeletonTrackingMode.Seated
                : SkeletonTrackingMode.Default;
            jointSmoother.Reset();
        }

        private void SmoothingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SmoothingValueText != null)
            {
                SmoothingValueText.Text = e.NewValue.ToString("0.00");
            }
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

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (!recorder.IsRecording)
            {
                recorder.Start(SeatedModeCheckBox.IsChecked == true);
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
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "captures");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "kinect-reframe-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png");

            int width = Math.Max(1, (int)RootVisual.ActualWidth);
            int height = Math.Max(1, (int)RootVisual.ActualHeight);
            RenderTargetBitmap render = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            render.Render(RootVisual);

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
