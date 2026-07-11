# Kinect Reframe session format

Tracking sessions use JSON and the file extension `.krs.json`.

The current format identifier is:

```text
kinect-reframe-session-v1
```

## Top-level object

```json
{
  "Format": "kinect-reframe-session-v1",
  "CreatedUtc": "2026-07-11T12:00:00.0000000Z",
  "SeatedMode": true,
  "Frames": []
}
```

## Frame

Each frame contains elapsed time from the start of the recording, the active Kinect tracking ID and one entry per Kinect joint.

```json
{
  "TimestampMilliseconds": 133,
  "TrackingId": 1,
  "Joints": []
}
```

## Joint sample

```json
{
  "Joint": "HandRight",
  "TrackingState": "Tracked",
  "Predicted": false,
  "RawX": 0.28,
  "RawY": 0.31,
  "RawZ": 1.74,
  "SmoothedX": 0.27,
  "SmoothedY": 0.30,
  "SmoothedZ": 1.75
}
```

Coordinates are Kinect skeleton-space metres:

- positive X points to the sensor's left or right according to Kinect camera space
- positive Y points upward
- positive Z points away from the sensor

Consumers should rely on the Kinect SDK camera-space convention rather than treating values as screen pixels.

## Prediction semantics

`Predicted: true` currently means Kinect Reframe preserved a recent valid joint position for a small number of frames after the Kinect reported it as not tracked. It does not mean the Kinect directly measured the joint in that frame.

Future formats may add velocity, confidence, filter settings and model landmarks. Any incompatible schema change must use a new `Format` value.
