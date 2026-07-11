using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;
using Microsoft.Kinect;
using KinectReframe.Tracking;

namespace KinectReframe.Services
{
    public sealed class SessionRecorder
    {
        private readonly Stopwatch clock = new Stopwatch();
        private TrackingRecording recording;

        public bool IsRecording
        {
            get { return recording != null; }
        }

        public void Start(bool seatedMode)
        {
            recording = new TrackingRecording
            {
                Format = "kinect-reframe-session-v1",
                CreatedUtc = DateTime.UtcNow.ToString("O"),
                SeatedMode = seatedMode
            };

            clock.Restart();
        }

        public void Capture(Skeleton rawSkeleton, SmoothedSkeleton smoothedSkeleton)
        {
            if (recording == null || rawSkeleton == null || smoothedSkeleton == null)
            {
                return;
            }

            TrackingFrame frame = new TrackingFrame
            {
                TimestampMilliseconds = clock.ElapsedMilliseconds,
                TrackingId = rawSkeleton.TrackingId
            };

            foreach (Joint rawJoint in rawSkeleton.Joints)
            {
                SmoothedJoint smoothedJoint;
                if (!smoothedSkeleton.Joints.TryGetValue(rawJoint.JointType, out smoothedJoint))
                {
                    continue;
                }

                frame.Joints.Add(new JointSample
                {
                    Joint = rawJoint.JointType.ToString(),
                    TrackingState = smoothedJoint.TrackingState.ToString(),
                    Predicted = smoothedJoint.IsPredicted,
                    RawX = rawJoint.Position.X,
                    RawY = rawJoint.Position.Y,
                    RawZ = rawJoint.Position.Z,
                    SmoothedX = smoothedJoint.Position.X,
                    SmoothedY = smoothedJoint.Position.Y,
                    SmoothedZ = smoothedJoint.Position.Z
                });
            }

            recording.Frames.Add(frame);
        }

        public string StopAndSave(string folder)
        {
            if (recording == null)
            {
                return null;
            }

            clock.Stop();
            Directory.CreateDirectory(folder);
            string path = Path.Combine(
                folder,
                "session-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".krs.json");

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(TrackingRecording));
            using (FileStream stream = File.Create(path))
            {
                serializer.WriteObject(stream, recording);
            }

            recording = null;
            return path;
        }
    }
}
