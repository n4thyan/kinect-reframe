// This file is compiled only when UseKinectStub=true.
// It exists so CI can validate C# and XAML without installing the proprietary Kinect SDK.
// Real builds reference Microsoft.Kinect.dll from Kinect for Windows SDK 1.8 instead.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Kinect
{
    public enum KinectStatus
    {
        Undefined,
        Connected,
        Disconnected,
        NotPowered,
        NotReady,
        DeviceNotGenuine,
        DeviceNotSupported,
        Initializing,
        InsufficientBandwidth
    }

    public enum ColorImageFormat
    {
        Undefined,
        RgbResolution640x480Fps30
    }

    public enum DepthImageFormat
    {
        Undefined,
        Resolution320x240Fps30
    }

    public enum SkeletonTrackingMode
    {
        Default,
        Seated
    }

    public enum SkeletonTrackingState
    {
        NotTracked,
        PositionOnly,
        Tracked
    }

    public enum JointTrackingState
    {
        NotTracked,
        Inferred,
        Tracked
    }

    public enum JointType
    {
        HipCenter,
        Spine,
        ShoulderCenter,
        Head,
        ShoulderLeft,
        ElbowLeft,
        WristLeft,
        HandLeft,
        ShoulderRight,
        ElbowRight,
        WristRight,
        HandRight,
        HipLeft,
        KneeLeft,
        AnkleLeft,
        FootLeft,
        HipRight,
        KneeRight,
        AnkleRight,
        FootRight
    }

    public struct SkeletonPoint
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public struct ColorImagePoint
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public struct DepthImagePixel
    {
        public int Depth { get; set; }
        public int PlayerIndex { get; set; }
    }

    public struct Joint
    {
        public JointType JointType { get; set; }
        public SkeletonPoint Position { get; set; }
        public JointTrackingState TrackingState { get; set; }
    }

    public sealed class JointCollection : IEnumerable<Joint>
    {
        private readonly Dictionary<JointType, Joint> joints = new Dictionary<JointType, Joint>();

        public JointCollection()
        {
            foreach (JointType jointType in Enum.GetValues(typeof(JointType)))
            {
                joints[jointType] = new Joint
                {
                    JointType = jointType,
                    TrackingState = JointTrackingState.NotTracked
                };
            }
        }

        public Joint this[JointType jointType]
        {
            get { return joints[jointType]; }
        }

        public IEnumerator<Joint> GetEnumerator()
        {
            return joints.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public sealed class Skeleton
    {
        public Skeleton()
        {
            Joints = new JointCollection();
        }

        public int TrackingId { get; set; }
        public SkeletonTrackingState TrackingState { get; set; }
        public JointCollection Joints { get; private set; }
    }

    public sealed class ColorImageStream
    {
        public int FrameWidth { get; private set; }
        public int FrameHeight { get; private set; }
        public int FramePixelDataLength { get; private set; }

        public void Enable(ColorImageFormat format)
        {
            FrameWidth = 640;
            FrameHeight = 480;
            FramePixelDataLength = FrameWidth * FrameHeight * 4;
        }
    }

    public sealed class DepthImageStream
    {
        public int FrameWidth { get; private set; }
        public int FrameHeight { get; private set; }
        public int FramePixelDataLength { get; private set; }

        public void Enable(DepthImageFormat format)
        {
            FrameWidth = 320;
            FrameHeight = 240;
            FramePixelDataLength = FrameWidth * FrameHeight;
        }
    }

    public sealed class SkeletonStream
    {
        public SkeletonStream()
        {
            FrameSkeletonArrayLength = 6;
        }

        public int FrameSkeletonArrayLength { get; private set; }
        public SkeletonTrackingMode TrackingMode { get; set; }

        public void Enable()
        {
        }
    }

    public sealed class CoordinateMapper
    {
        public ColorImagePoint MapSkeletonPointToColorPoint(
            SkeletonPoint point,
            ColorImageFormat format)
        {
            return new ColorImagePoint
            {
                X = (int)((point.X + 1.0f) * 320.0f),
                Y = (int)((1.0f - point.Y) * 240.0f)
            };
        }

        public void MapDepthFrameToSkeletonFrame(
            DepthImageFormat format,
            DepthImagePixel[] depthPixels,
            SkeletonPoint[] skeletonPoints)
        {
            if (depthPixels == null || skeletonPoints == null)
            {
                return;
            }

            int length = Math.Min(depthPixels.Length, skeletonPoints.Length);
            const int width = 320;
            const int height = 240;
            for (int index = 0; index < length; index++)
            {
                int x = index % width;
                int y = index / width;
                float z = depthPixels[index].Depth / 1000.0f;
                skeletonPoints[index] = new SkeletonPoint
                {
                    X = ((x - (width * 0.5f)) / width) * z,
                    Y = (((height * 0.5f) - y) / height) * z,
                    Z = z
                };
            }
        }
    }

    public sealed class KinectSensorCollection : IEnumerable<KinectSensor>
    {
        private readonly List<KinectSensor> sensors = new List<KinectSensor>();

        public IEnumerator<KinectSensor> GetEnumerator()
        {
            return sensors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public sealed class KinectSensor
    {
        private static readonly KinectSensorCollection Sensors = new KinectSensorCollection();

        public KinectSensor()
        {
            ColorStream = new ColorImageStream();
            DepthStream = new DepthImageStream();
            SkeletonStream = new SkeletonStream();
            CoordinateMapper = new CoordinateMapper();
        }

        public static KinectSensorCollection KinectSensors
        {
            get { return Sensors; }
        }

        public KinectStatus Status { get; set; }
        public bool IsRunning { get; private set; }
        public string UniqueKinectId { get; set; }
        public ColorImageStream ColorStream { get; private set; }
        public DepthImageStream DepthStream { get; private set; }
        public SkeletonStream SkeletonStream { get; private set; }
        public CoordinateMapper CoordinateMapper { get; private set; }

        public event EventHandler<AllFramesReadyEventArgs> AllFramesReady;

        public void Start()
        {
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        internal void RaiseAllFramesReady(AllFramesReadyEventArgs args)
        {
            EventHandler<AllFramesReadyEventArgs> handler = AllFramesReady;
            if (handler != null)
            {
                handler(this, args);
            }
        }
    }

    public sealed class AllFramesReadyEventArgs : EventArgs
    {
        public ColorImageFrame OpenColorImageFrame()
        {
            return null;
        }

        public DepthImageFrame OpenDepthImageFrame()
        {
            return null;
        }

        public SkeletonFrame OpenSkeletonFrame()
        {
            return null;
        }
    }

    public sealed class ColorImageFrame : IDisposable
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int BytesPerPixel { get; set; }

        public void CopyPixelDataTo(byte[] pixels)
        {
        }

        public void Dispose()
        {
        }
    }

    public sealed class DepthImageFrame : IDisposable
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public void CopyDepthImagePixelDataTo(DepthImagePixel[] pixels)
        {
        }

        public void Dispose()
        {
        }
    }

    public sealed class SkeletonFrame : IDisposable
    {
        public void CopySkeletonDataTo(Skeleton[] skeletons)
        {
        }

        public void Dispose()
        {
        }
    }
}
