using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Kinect;
using KinectReframe.Tracking;

namespace KinectReframe.Rendering
{
    public static class SkeletonRenderer
    {
        private static readonly Brush RawBrush = new SolidColorBrush(Color.FromArgb(150, 222, 230, 234));
        private static readonly Brush EnhancedBrush = new SolidColorBrush(Color.FromRgb(38, 230, 184));
        private static readonly Brush InferredBrush = new SolidColorBrush(Color.FromRgb(91, 188, 255));
        private static readonly Brush PredictedBrush = new SolidColorBrush(Color.FromRgb(255, 196, 71));

        private static readonly Bone[] Bones =
        {
            new Bone(JointType.Head, JointType.ShoulderCenter),
            new Bone(JointType.ShoulderCenter, JointType.ShoulderLeft),
            new Bone(JointType.ShoulderCenter, JointType.ShoulderRight),
            new Bone(JointType.ShoulderCenter, JointType.Spine),
            new Bone(JointType.Spine, JointType.HipCenter),
            new Bone(JointType.HipCenter, JointType.HipLeft),
            new Bone(JointType.HipCenter, JointType.HipRight),

            new Bone(JointType.ShoulderLeft, JointType.ElbowLeft),
            new Bone(JointType.ElbowLeft, JointType.WristLeft),
            new Bone(JointType.WristLeft, JointType.HandLeft),

            new Bone(JointType.ShoulderRight, JointType.ElbowRight),
            new Bone(JointType.ElbowRight, JointType.WristRight),
            new Bone(JointType.WristRight, JointType.HandRight),

            new Bone(JointType.HipLeft, JointType.KneeLeft),
            new Bone(JointType.KneeLeft, JointType.AnkleLeft),
            new Bone(JointType.AnkleLeft, JointType.FootLeft),

            new Bone(JointType.HipRight, JointType.KneeRight),
            new Bone(JointType.KneeRight, JointType.AnkleRight),
            new Bone(JointType.AnkleRight, JointType.FootRight)
        };

        public static void Draw(
            Canvas canvas,
            KinectSensor sensor,
            Skeleton rawSkeleton,
            SmoothedSkeleton smoothedSkeleton,
            bool showRaw)
        {
            canvas.Children.Clear();

            if (showRaw)
            {
                DrawRawSkeleton(canvas, sensor, rawSkeleton);
            }

            DrawEnhancedSkeleton(canvas, sensor, smoothedSkeleton);
        }

        private static void DrawRawSkeleton(Canvas canvas, KinectSensor sensor, Skeleton skeleton)
        {
            foreach (Bone bone in Bones)
            {
                Joint first = skeleton.Joints[bone.First];
                Joint second = skeleton.Joints[bone.Second];

                if (first.TrackingState == JointTrackingState.NotTracked ||
                    second.TrackingState == JointTrackingState.NotTracked)
                {
                    continue;
                }

                AddBone(
                    canvas,
                    sensor,
                    first.Position,
                    second.Position,
                    RawBrush,
                    2,
                    first.TrackingState == JointTrackingState.Inferred || second.TrackingState == JointTrackingState.Inferred);
            }

            foreach (Joint joint in skeleton.Joints)
            {
                if (joint.TrackingState != JointTrackingState.NotTracked)
                {
                    AddJoint(canvas, sensor, joint.Position, RawBrush, 4);
                }
            }
        }

        private static void DrawEnhancedSkeleton(Canvas canvas, KinectSensor sensor, SmoothedSkeleton skeleton)
        {
            foreach (Bone bone in Bones)
            {
                SmoothedJoint first;
                SmoothedJoint second;
                if (!skeleton.Joints.TryGetValue(bone.First, out first) ||
                    !skeleton.Joints.TryGetValue(bone.Second, out second) ||
                    first.TrackingState == JointTrackingState.NotTracked ||
                    second.TrackingState == JointTrackingState.NotTracked)
                {
                    continue;
                }

                bool predicted = first.IsPredicted || second.IsPredicted;
                bool inferred = first.TrackingState == JointTrackingState.Inferred ||
                                second.TrackingState == JointTrackingState.Inferred;
                Brush brush = predicted ? PredictedBrush : inferred ? InferredBrush : EnhancedBrush;

                AddBone(canvas, sensor, first.Position, second.Position, brush, 5, predicted || inferred);
            }

            foreach (KeyValuePair<JointType, SmoothedJoint> pair in skeleton.Joints)
            {
                SmoothedJoint joint = pair.Value;
                if (joint.TrackingState == JointTrackingState.NotTracked)
                {
                    continue;
                }

                Brush brush = joint.IsPredicted
                    ? PredictedBrush
                    : joint.TrackingState == JointTrackingState.Inferred
                        ? InferredBrush
                        : EnhancedBrush;
                double size = pair.Key == JointType.HandLeft || pair.Key == JointType.HandRight ? 12 : 8;
                AddJoint(canvas, sensor, joint.Position, brush, size);
            }
        }

        private static void AddBone(
            Canvas canvas,
            KinectSensor sensor,
            SkeletonPoint first,
            SkeletonPoint second,
            Brush brush,
            double thickness,
            bool dashed)
        {
            ColorImagePoint firstPoint = Map(sensor, first);
            ColorImagePoint secondPoint = Map(sensor, second);

            Line line = new Line
            {
                X1 = firstPoint.X,
                Y1 = firstPoint.Y,
                X2 = secondPoint.X,
                Y2 = secondPoint.Y,
                Stroke = brush,
                StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            if (dashed)
            {
                line.StrokeDashArray = new DoubleCollection { 3, 2 };
            }

            canvas.Children.Add(line);
        }

        private static void AddJoint(
            Canvas canvas,
            KinectSensor sensor,
            SkeletonPoint position,
            Brush brush,
            double size)
        {
            ColorImagePoint point = Map(sensor, position);
            Ellipse ellipse = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = brush,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            Canvas.SetLeft(ellipse, point.X - (size / 2));
            Canvas.SetTop(ellipse, point.Y - (size / 2));
            canvas.Children.Add(ellipse);
        }

        private static ColorImagePoint Map(KinectSensor sensor, SkeletonPoint point)
        {
            return sensor.CoordinateMapper.MapSkeletonPointToColorPoint(
                point,
                ColorImageFormat.RgbResolution640x480Fps30);
        }

        private sealed class Bone
        {
            public Bone(JointType first, JointType second)
            {
                First = first;
                Second = second;
            }

            public JointType First { get; private set; }
            public JointType Second { get; private set; }
        }
    }
}
