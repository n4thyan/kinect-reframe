using System;
using System.Collections.Generic;
using Microsoft.Kinect;

namespace KinectReframe.Tracking
{
    public sealed class JointSmoother
    {
        private const int MaximumHeldFrames = 8;
        private const float MaximumPredictionStepMetres = 0.075f;
        private readonly Dictionary<JointType, JointHistory> histories = new Dictionary<JointType, JointHistory>();
        private int activeTrackingId;

        public SmoothedSkeleton Update(Skeleton skeleton, double responsiveness)
        {
            if (skeleton == null)
            {
                throw new ArgumentNullException("skeleton");
            }

            if (activeTrackingId != skeleton.TrackingId)
            {
                Reset();
                activeTrackingId = skeleton.TrackingId;
            }

            float baseAlpha = (float)Clamp(responsiveness, 0.01, 1.0);
            SmoothedSkeleton result = new SmoothedSkeleton(skeleton.TrackingId, skeleton.TrackingState);

            foreach (Joint joint in skeleton.Joints)
            {
                JointHistory history;
                if (!histories.TryGetValue(joint.JointType, out history))
                {
                    history = new JointHistory();
                    histories[joint.JointType] = history;
                }

                if (joint.TrackingState != JointTrackingState.NotTracked)
                {
                    UpdateMeasuredJoint(result, joint, history, baseAlpha);
                    continue;
                }

                UpdateMissingJoint(result, joint, history);
            }

            return result;
        }

        public void Reset()
        {
            histories.Clear();
            activeTrackingId = 0;
        }

        private static void UpdateMeasuredJoint(
            SmoothedSkeleton result,
            Joint joint,
            JointHistory history,
            float baseAlpha)
        {
            SkeletonPoint measuredVelocity = new SkeletonPoint();
            float movement = 0.0f;

            if (history.HasRawValue)
            {
                measuredVelocity = Subtract(joint.Position, history.RawPosition);
                movement = Length(measuredVelocity);
            }

            // Slow joints receive stronger smoothing. Fast deliberate movement raises alpha,
            // reducing the lag that a fixed low-pass filter normally introduces.
            float motionFactor = (float)Clamp(movement / 0.085f, 0.0, 1.0);
            float trackingAlpha = joint.TrackingState == JointTrackingState.Tracked
                ? baseAlpha
                : Math.Max(0.04f, baseAlpha * 0.30f);
            float adaptiveAlpha = trackingAlpha + ((1.0f - trackingAlpha) * motionFactor * 0.78f);

            SkeletonPoint position = history.HasValue
                ? Blend(history.Position, joint.Position, adaptiveAlpha)
                : joint.Position;

            if (history.HasRawValue)
            {
                float velocityAlpha = joint.TrackingState == JointTrackingState.Tracked ? 0.42f : 0.20f;
                history.Velocity = Blend(history.Velocity, measuredVelocity, velocityAlpha);
            }
            else
            {
                history.Velocity = new SkeletonPoint();
            }

            // Prevent old velocity from carrying on after the user has stopped.
            if (movement < 0.0035f)
            {
                history.Velocity = Scale(history.Velocity, 0.55f);
            }

            history.Position = position;
            history.RawPosition = joint.Position;
            history.HasValue = true;
            history.HasRawValue = true;
            history.MissingFrames = 0;

            result.Joints[joint.JointType] = new SmoothedJoint(
                joint.JointType,
                position,
                joint.TrackingState,
                false);
        }

        private static void UpdateMissingJoint(
            SmoothedSkeleton result,
            Joint joint,
            JointHistory history)
        {
            history.MissingFrames++;

            if (history.HasValue && history.MissingFrames <= MaximumHeldFrames)
            {
                float decay = (float)Math.Pow(0.68, history.MissingFrames - 1);
                SkeletonPoint step = LimitMagnitude(Scale(history.Velocity, decay), MaximumPredictionStepMetres);
                SkeletonPoint predicted = Add(history.Position, step);

                history.Position = predicted;
                history.Velocity = Scale(history.Velocity, 0.72f);

                result.Joints[joint.JointType] = new SmoothedJoint(
                    joint.JointType,
                    predicted,
                    JointTrackingState.Inferred,
                    true);
                return;
            }

            history.Velocity = new SkeletonPoint();
            result.Joints[joint.JointType] = new SmoothedJoint(
                joint.JointType,
                joint.Position,
                JointTrackingState.NotTracked,
                false);
        }

        private static SkeletonPoint Blend(SkeletonPoint previous, SkeletonPoint current, float alpha)
        {
            return new SkeletonPoint
            {
                X = previous.X + ((current.X - previous.X) * alpha),
                Y = previous.Y + ((current.Y - previous.Y) * alpha),
                Z = previous.Z + ((current.Z - previous.Z) * alpha)
            };
        }

        private static SkeletonPoint Add(SkeletonPoint first, SkeletonPoint second)
        {
            return new SkeletonPoint
            {
                X = first.X + second.X,
                Y = first.Y + second.Y,
                Z = first.Z + second.Z
            };
        }

        private static SkeletonPoint Subtract(SkeletonPoint first, SkeletonPoint second)
        {
            return new SkeletonPoint
            {
                X = first.X - second.X,
                Y = first.Y - second.Y,
                Z = first.Z - second.Z
            };
        }

        private static SkeletonPoint Scale(SkeletonPoint point, float amount)
        {
            return new SkeletonPoint
            {
                X = point.X * amount,
                Y = point.Y * amount,
                Z = point.Z * amount
            };
        }

        private static SkeletonPoint LimitMagnitude(SkeletonPoint point, float maximum)
        {
            float length = Length(point);
            if (length <= maximum || length <= 0.000001f)
            {
                return point;
            }

            return Scale(point, maximum / length);
        }

        private static float Length(SkeletonPoint point)
        {
            return (float)Math.Sqrt(
                (point.X * point.X) +
                (point.Y * point.Y) +
                (point.Z * point.Z));
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private sealed class JointHistory
        {
            public bool HasValue { get; set; }
            public bool HasRawValue { get; set; }
            public SkeletonPoint Position { get; set; }
            public SkeletonPoint RawPosition { get; set; }
            public SkeletonPoint Velocity { get; set; }
            public int MissingFrames { get; set; }
        }
    }

    public sealed class SmoothedSkeleton
    {
        public SmoothedSkeleton(int trackingId, SkeletonTrackingState trackingState)
        {
            TrackingId = trackingId;
            TrackingState = trackingState;
            Joints = new Dictionary<JointType, SmoothedJoint>();
        }

        public int TrackingId { get; private set; }
        public SkeletonTrackingState TrackingState { get; private set; }
        public IDictionary<JointType, SmoothedJoint> Joints { get; private set; }
    }

    public sealed class SmoothedJoint
    {
        public SmoothedJoint(
            JointType jointType,
            SkeletonPoint position,
            JointTrackingState trackingState,
            bool isPredicted)
        {
            JointType = jointType;
            Position = position;
            TrackingState = trackingState;
            IsPredicted = isPredicted;
        }

        public JointType JointType { get; private set; }
        public SkeletonPoint Position { get; private set; }
        public JointTrackingState TrackingState { get; private set; }
        public bool IsPredicted { get; private set; }
    }
}
