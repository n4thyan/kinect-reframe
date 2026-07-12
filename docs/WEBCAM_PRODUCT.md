# Kinect webcam and camera utility direction

Kinect Reframe is moving from a collection of tracking experiments into a practical Windows camera utility built around the Xbox 360 Kinect.

The application should remain honest about what the sensor can and cannot do. It should expose the Kinect's unusual capabilities in a coherent camera workflow rather than presenting them as disconnected demos.

## Product goals

A useful release should provide four clear jobs:

1. **Camera composition**
   - clean RGB preview
   - mirror, brightness and contrast
   - digital zoom and pan
   - skeleton-assisted automatic framing
   - overlays and framing guides
   - screenshots and video recording

2. **Kinect sensor control**
   - motor tilt
   - accelerometer readout
   - seated and full-body tracking
   - visible connection and performance state

3. **Microphone-array utility**
   - local input-level meter
   - detected sound-source direction
   - beam direction and confidence
   - later audio recording after format and sync validation

4. **Depth and tracking tools**
   - body segmentation
   - motion and depth heatmaps
   - depth hologram
   - interactive 3D point cloud
   - tracking-data capture

## First webcam utility pass

Implemented on `main`:

- cleaned Properties inspector with labels and values separated from slider tracks
- flatter dark expander headers
- digital zoom from 1.0× to 3.0×
- manual horizontal and vertical framing
- head-and-shoulders framing preset
- skeleton-assisted automatic subject framing
- motor tilt control from −27° through +27°
- tilt debounce to avoid repeatedly hammering the Kinect motor
- live accelerometer values
- optional Kinect microphone-array monitor
- local dBFS input meter
- sound-source angle, confidence and beam-angle display

The microphone monitor does not send audio to the current MJPEG recorder. Camera AVI files remain video-only until audio capture and A/V synchronisation have been tested properly.

## Release sequence

### 0.3 usability pass

- validate framing, tilt and microphone controls on physical hardware
- persist framing and sensor preferences
- improve empty, disconnected and error states
- add a device-information panel
- audit labels, spacing, keyboard navigation and small-screen behaviour

### 0.4 camera effects

- RGB-aligned player mask
- background cutout or replacement
- optional background blur
- overlay opacity controls
- saved camera profiles

### 0.5 output integration

- virtual-camera research for Zoom, Discord and browser applications
- evaluate DirectShow/Media Foundation virtual-camera approaches
- retain projector output as the dependency-free fallback
- optional audio recording with measured synchronisation

### 1.0 release criteria

- clean install and first-run flow
- no known startup or shutdown crashes
- stable camera, depth and skeleton operation
- video output finalises reliably
- settings survive restart
- motor and microphone failures are non-fatal
- keyboard and mouse controls are documented
- release ZIP or installer includes version, licence and troubleshooting information

## Important distinction

The current application can preview, compose, project, capture and record Kinect camera output. Appearing as a selectable webcam inside another application requires a separate Windows virtual-camera component. That work should not be described as complete until another application can select and consume the Kinect Reframe output directly.
