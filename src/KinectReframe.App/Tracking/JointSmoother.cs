using System;
using System.Collections.Generic;
using Microsoft.Kinect;

namespace KinectReframe.Tracking
{
    public sealed class JointSmoother
    {
        private const int MaximumHeldFrames = 8;
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

            float alpha = (float)Math.Max(0.01, Math.Min(1.0, responsiveness));
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
                    float effectiveAlpha = joint.TrackingState == JointTrackingState.Tracked
                        ? alpha
                        : Math.Max(0.05f, alpha * 0.35f);

                    SkeletonPoint position = history.HasValue
                        ? Blend(history.Position, joint.Position, effectiveAlpha)
                        : joint.Position;

                    history.Position = position;
                    history.HasValue = true;
                    history.MissingFrames = 0;

                    result.Joints[joint.JointType] = new SmoothedJoint(
                        joint.JointType,
                        position,
                        joint.TrackingState,
                        false);
                    continue;
                }

                history.MissingFrames++;
                if (history.HasValue && history.MissingFrames <= MaximumHeldFrames)
                {
                    result.Joints[joint.JointType] = new SmoothedJoint(
                        joint.JointType,
                        history.Position,
                        JointTrackingState.Inferred,
                        true);
                }
                else
                {
                    result.Joints[joint.JointType] = new SmoothedJoint(
                        joint.JointType,
                        joint.Position,
                        JointTrackingState.NotTracked,
                        false);
                }
            }

            return result;
        }

        public void Reset()
        {
            histories.Clear();
            activeTrackingId = 0;
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

        private sealed class JointHistory
        {
            public bool HasValue { get; set; }
            public SkeletonPoint Position { get; set; }
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
