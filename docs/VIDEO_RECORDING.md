# Video recording

Kinect Reframe records video without requiring FFmpeg, a codec pack or an additional NuGet package. Frames are captured from the WPF composition, JPEG encoded on a worker thread and written into a standard MJPEG AVI container.

## Recording modes

### Camera video

**Record camera video** captures the 640 × 480 camera composition at the selected output scale.

The recording includes whichever visual layers are active:

- mirrored or unmirrored RGB
- enhanced skeleton
- raw skeleton
- motion heat
- depth heat
- framing grid
- brightness and contrast adjustments

The application chrome, mode label and buttons are not included.

### Render video

**Record render video** captures the selected renderer tab:

- depth hologram
- 3D point cloud

The selected renderer is locked until recording stops so one AVI cannot silently change source halfway through.

The clean render image is recorded without the tab strip, drag instructions or application controls.

## Output settings

### Output scale

The base composition is 640 × 480. Available scale values produce:

| Scale | Output |
| --- | --- |
| 0.5× | 320 × 240 |
| 1.0× | 640 × 480 |
| 1.5× | 960 × 720 |
| 2.0× | 1280 × 960 |
| 2.5× | 1600 × 1200 |
| 3.0× | 1920 × 1440 |

Scaling above 1× increases output dimensions but cannot create detail that the Kinect did not capture.

### Video FPS

The selectable range is 5–30 frames per second. Fifteen FPS is the default because it provides a useful balance between motion, CPU load and file size on older hardware.

### JPEG quality

MJPEG stores every frame as an independent JPEG image. Higher quality values increase detail and file size while placing more load on the encoder.

The default is 82.

## Performance behaviour

The video recorder deliberately does not block the Kinect frame callback.

1. The UI thread captures the requested visual at the selected resolution.
2. A frozen WPF bitmap is placed into a bounded queue.
3. A dedicated STA worker thread JPEG encodes queued frames.
4. Encoded frames are appended to the AVI stream.
5. When the queue is full, the newest frame is dropped and the counter increases.

This policy keeps depth, skeleton and camera processing responsive even when the requested recording settings are too heavy for the PC.

The recording badge shows:

- elapsed time
- selected output FPS
- frames written
- frames dropped

Large output sizes, high JPEG quality, 30 FPS and dense full-scene point clouds are the most demanding combination.

## Output location

Debug builds save video beside the executable:

```text
src\KinectReframe.App\bin\Debug\videos\
```

Release builds use:

```text
src\KinectReframe.App\bin\Release\videos\
```

Use **Open output folder** inside the application to open the executable directory.

File names identify the source and resolution:

```text
kinect-reframe-camera-640x480-YYYYMMDD-HHMMSS.avi
kinect-reframe-depth-render-640x480-YYYYMMDD-HHMMSS.avi
kinect-reframe-point-cloud-640x480-YYYYMMDD-HHMMSS.avi
```

## Format and limitations

- container: AVI
- video codec: MJPEG
- audio: none
- constant declared frame rate
- dropped frames are omitted rather than duplicated
- each frame is independently seekable
- output is intended for local experiments and editing, not final distribution

A later encoder backend may add audio, more efficient codecs and automatic file splitting. MJPEG is used first because it is transparent, dependency-free and widely readable.

## Hardware test checklist

Test short clips first.

1. Select 1× output, 15 FPS and quality 82.
2. Record ten seconds of camera video.
3. Stop and confirm the AVI opens and reports roughly ten seconds.
4. Enable skeleton or motion heat and verify it appears in the camera recording.
5. Record the depth hologram.
6. Select the 3D point cloud, rotate it while recording and verify the movement is captured.
7. Repeat at 0.5× and 2× and verify dimensions.
8. Try 30 FPS and note dropped frames.
9. Close the application during a short recording and verify the file is finalised.
10. Report the exact settings, visible FPS, written frames and dropped frames for any failure.
