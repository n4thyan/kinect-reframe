using System.Collections.Generic;
using System.Runtime.Serialization;

namespace KinectReframe.Tracking
{
    [DataContract]
    public sealed class TrackingRecording
    {
        public TrackingRecording()
        {
            Frames = new List<TrackingFrame>();
        }

        [DataMember(Order = 1)]
        public string Format { get; set; }

        [DataMember(Order = 2)]
        public string CreatedUtc { get; set; }

        [DataMember(Order = 3)]
        public bool SeatedMode { get; set; }

        [DataMember(Order = 4)]
        public List<TrackingFrame> Frames { get; set; }
    }

    [DataContract]
    public sealed class TrackingFrame
    {
        public TrackingFrame()
        {
            Joints = new List<JointSample>();
        }

        [DataMember(Order = 1)]
        public long TimestampMilliseconds { get; set; }

        [DataMember(Order = 2)]
        public int TrackingId { get; set; }

        [DataMember(Order = 3)]
        public List<JointSample> Joints { get; set; }
    }

    [DataContract]
    public sealed class JointSample
    {
        [DataMember(Order = 1)]
        public string Joint { get; set; }

        [DataMember(Order = 2)]
        public string TrackingState { get; set; }

        [DataMember(Order = 3)]
        public bool Predicted { get; set; }

        [DataMember(Order = 4)]
        public float RawX { get; set; }

        [DataMember(Order = 5)]
        public float RawY { get; set; }

        [DataMember(Order = 6)]
        public float RawZ { get; set; }

        [DataMember(Order = 7)]
        public float SmoothedX { get; set; }

        [DataMember(Order = 8)]
        public float SmoothedY { get; set; }

        [DataMember(Order = 9)]
        public float SmoothedZ { get; set; }
    }
}
