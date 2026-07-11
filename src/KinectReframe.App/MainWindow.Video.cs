using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using KinectReframe.Services;

namespace KinectReframe
{
    public partial class MainWindow
    {
        private readonly VideoRecorder videoRecorder = new VideoRecorder();
        private readonly Stopwatch videoClock = new Stopwatch();
        private DispatcherTimer videoCaptureTimer;
        private VideoCaptureSource activeVideoSource;
        private int activeVideoWidth;
        private int activeVideoHeight;

        private void CameraVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (videoRecorder.IsRecording)
            {
                if (activeVideoSource == VideoCaptureSource.Camera)
                {
                    StopActiveVideoRecording(true);
                }
                else
                {
                    TrackingStatusText.Text = "Stop the current render recording before starting camera video";
                }

                return;
            }

            StartVideoRecording(VideoCaptureSource.Camera);
        }

        private void RenderVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (videoRecorder.IsRecording)
            {
                if (activeVideoSource == VideoCaptureSource.DepthRender || activeVideoSource == VideoCaptureSource.PointCloud)
                {
                    StopActiveVideoRecording(true);
                }
                else
                {
                    TrackingStatusText.Text = "Stop the current camera recording before starting render video";
                }

                return;
            }

            VideoCaptureSource source = RendererTabs.SelectedIndex == 0
                ? VideoCaptureSource.DepthRender
                : VideoCaptureSource.PointCloud;
            StartVideoRecording(source);
        }

        private void StartVideoRecording(VideoCaptureSource source)
        {
            try
            {
                if (source == VideoCaptureSource.Camera)
                {
                    SelectPreviewMode(StudioPreviewMode.Camera);
                }
                else if (source == VideoCaptureSource.DepthRender)
                {
                    SelectPreviewMode(StudioPreviewMode.Depth);
                }
                else
                {
                    SelectPreviewMode(StudioPreviewMode.PointCloud);
                }

                double scale = OutputScaleSlider == null ? 1.0 : OutputScaleSlider.Value;
                scale = Math.Max(0.5, Math.Min(3.0, scale));
                activeVideoWidth = Math.Max(2, (int)Math.Round(640.0 * scale));
                activeVideoHeight = Math.Max(2, (int)Math.Round(480.0 * scale));

                int fps = VideoFpsSlider == null ? 15 : (int)Math.Round(VideoFpsSlider.Value);
                int quality = VideoQualitySlider == null ? 82 : (int)Math.Round(VideoQualitySlider.Value);
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "videos");
                Directory.CreateDirectory(folder);

                string mode = GetVideoModeName(source);
                string path = Path.Combine(
                    folder,
                    "kinect-reframe-" + mode + "-" + activeVideoWidth + "x" + activeVideoHeight + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".avi");

                videoRecorder.Start(path, activeVideoWidth, activeVideoHeight, fps, quality);
                activeVideoSource = source;
                videoClock.Restart();

                if (videoCaptureTimer == null)
                {
                    videoCaptureTimer = new DispatcherTimer(DispatcherPriority.Render);
                    videoCaptureTimer.Tick += VideoCaptureTimer_Tick;
                }

                videoCaptureTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / fps);
                videoCaptureTimer.Start();
                SetVideoRecordingUi(true);
                CaptureCurrentVideoFrame();
                TrackingStatusText.Text = "Recording " + GetVideoDisplayName(source) + " to " + path;
                StatusBarMessageText.Text = "Recording " + GetVideoDisplayName(source);
            }
            catch (Exception exception)
            {
                activeVideoSource = VideoCaptureSource.None;
                SetVideoRecordingUi(false);
                TrackingStatusText.Text = "Could not start video recording: " + exception.Message;
                StatusBarMessageText.Text = "Recording could not start";
            }
        }

        private void VideoCaptureTimer_Tick(object sender, EventArgs e)
        {
            if (!videoRecorder.IsRecording)
            {
                return;
            }

            try
            {
                CaptureCurrentVideoFrame();
                UpdateVideoStatusBadge();
            }
            catch (Exception exception)
            {
                TrackingStatusText.Text = "Video capture failed: " + exception.Message;
                StopActiveVideoRecording(false);
            }
        }

        private void CaptureCurrentVideoFrame()
        {
            FrameworkElement source = GetActiveVideoElement();
            if (source == null || source.ActualWidth < 1 || source.ActualHeight < 1)
            {
                return;
            }

            BitmapSource frame = CaptureElementAtResolution(source, activeVideoWidth, activeVideoHeight);
            videoRecorder.TryAddFrame(frame);
        }

        private FrameworkElement GetActiveVideoElement()
        {
            switch (activeVideoSource)
            {
                case VideoCaptureSource.Camera:
                    return CameraSurface;
                case VideoCaptureSource.DepthRender:
                    return BodyImage;
                case VideoCaptureSource.PointCloud:
                    return PointCloudImage;
                default:
                    return null;
            }
        }

        private static BitmapSource CaptureElementAtResolution(FrameworkElement element, int width, int height)
        {
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                context.DrawRectangle(Brushes.Black, null, new Rect(0, 0, width, height));
                VisualBrush brush = new VisualBrush(element)
                {
                    Stretch = Stretch.Uniform,
                    AlignmentX = AlignmentX.Center,
                    AlignmentY = AlignmentY.Center
                };
                context.DrawRectangle(brush, null, new Rect(0, 0, width, height));
            }

            RenderTargetBitmap render = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            render.Render(visual);
            render.Freeze();
            return render;
        }

        private void StopActiveVideoRecording(bool showResult)
        {
            if (videoCaptureTimer != null)
            {
                videoCaptureTimer.Stop();
            }

            VideoCaptureSource completedSource = activeVideoSource;
            videoClock.Stop();
            VideoRecordingResult result = videoRecorder.Stop();
            activeVideoSource = VideoCaptureSource.None;
            SetVideoRecordingUi(false);

            if (!showResult)
            {
                return;
            }

            if (result.Error != null)
            {
                TrackingStatusText.Text = "Video recording failed: " + result.Error.Message;
                StatusBarMessageText.Text = "Video recording failed";
                return;
            }

            if (result.FramesWritten <= 0)
            {
                TrackingStatusText.Text = "No video frames were written";
                StatusBarMessageText.Text = "No video frames written";
                return;
            }

            TrackingStatusText.Text = string.Format(
                "Saved {0} video: {1:N0} frames, {2:N0} dropped • {3}",
                GetVideoDisplayName(completedSource),
                result.FramesWritten,
                result.DroppedFrames,
                result.Path);
            StatusBarMessageText.Text = string.Format(
                "Saved {0:N0} frames • {1:N0} dropped",
                result.FramesWritten,
                result.DroppedFrames);
        }

        private void SetVideoRecordingUi(bool recording)
        {
            VideoStatusBadge.Visibility = recording ? Visibility.Visible : Visibility.Collapsed;
            OutputScaleSlider.IsEnabled = !recording;
            VideoFpsSlider.IsEnabled = !recording;
            VideoQualitySlider.IsEnabled = !recording;
            FreezeToggle.IsEnabled = !recording;
            SceneList.IsEnabled = !recording;
            CameraPreviewModeButton.IsEnabled = !recording;
            DepthPreviewModeButton.IsEnabled = !recording;
            PointCloudPreviewModeButton.IsEnabled = !recording;
            SplitPreviewModeButton.IsEnabled = !recording;

            bool cameraRecording = recording && activeVideoSource == VideoCaptureSource.Camera;
            bool renderRecording = recording && !cameraRecording;

            CameraVideoButton.Content = cameraRecording ? "Stop camera video" : "Record camera video";
            RenderVideoButton.Content = renderRecording ? "Stop render video" : "Record render video";
            CameraVideoButton.IsEnabled = !recording || cameraRecording;
            RenderVideoButton.IsEnabled = !recording || renderRecording;
            RendererTabs.IsEnabled = !renderRecording;

            if (recording)
            {
                SolidColorBrush stopBrush = new SolidColorBrush(Color.FromRgb(215, 90, 98));
                if (cameraRecording)
                {
                    CameraVideoButton.Background = stopBrush;
                }
                else
                {
                    RenderVideoButton.Background = stopBrush;
                }

                UpdateVideoStatusBadge();
            }
            else
            {
                CameraVideoButton.ClearValue(BackgroundProperty);
                RenderVideoButton.ClearValue(BackgroundProperty);
                VideoStatusText.Text = "REC 00:00";
            }
        }

        private void UpdateVideoStatusBadge()
        {
            if (!videoRecorder.IsRecording)
            {
                return;
            }

            VideoStatusText.Text = string.Format(
                "REC {0}:{1:00} • {2} fps • {3:N0} frames • {4:N0} dropped",
                (int)videoClock.Elapsed.TotalMinutes,
                videoClock.Elapsed.Seconds,
                videoRecorder.FramesPerSecond,
                videoRecorder.FramesWritten,
                videoRecorder.DroppedFrames);
        }

        private void VideoSettingSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VideoFpsValueText != null && VideoFpsSlider != null)
            {
                VideoFpsValueText.Text = Math.Round(VideoFpsSlider.Value).ToString("0");
            }

            if (VideoQualityValueText != null && VideoQualitySlider != null)
            {
                VideoQualityValueText.Text = Math.Round(VideoQualitySlider.Value).ToString("0");
            }
        }

        private void OpenOutputFolderButton_Click(object sender, RoutedEventArgs e)
        {
            string folder = AppDomain.CurrentDomain.BaseDirectory;
            Directory.CreateDirectory(Path.Combine(folder, "videos"));
            Process.Start("explorer.exe", folder);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                if (videoRecorder.IsRecording)
                {
                    StopActiveVideoRecording(false);
                }
            }
            finally
            {
                videoRecorder.Dispose();
            }
        }

        private static string GetVideoModeName(VideoCaptureSource source)
        {
            switch (source)
            {
                case VideoCaptureSource.Camera:
                    return "camera";
                case VideoCaptureSource.DepthRender:
                    return "depth-render";
                case VideoCaptureSource.PointCloud:
                    return "point-cloud";
                default:
                    return "video";
            }
        }

        private static string GetVideoDisplayName(VideoCaptureSource source)
        {
            switch (source)
            {
                case VideoCaptureSource.Camera:
                    return "camera";
                case VideoCaptureSource.DepthRender:
                    return "depth render";
                case VideoCaptureSource.PointCloud:
                    return "3D point-cloud render";
                default:
                    return "video";
            }
        }

        private enum VideoCaptureSource
        {
            None,
            Camera,
            DepthRender,
            PointCloud
        }
    }
}
