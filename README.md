# Kinect Reframe

Kinect Reframe is an experimental Xbox 360 Kinect tracking, depth-visualisation and real-time 3D rendering studio for Windows.

The current build uses Kinect for Windows SDK 1.8, deterministic filtering and conventional geometry. It does **not** currently embed an LLM or learned pose model. Modern pose-model fusion remains an optional future research direction rather than a claim about the present application.

## Current status

The camera, depth, skeleton, heatmap and point-cloud pipeline has been tested against a physical Xbox 360 Kinect on Windows.

Validated core features:

- RGB camera feed at 640 × 480
- depth feed at 320 × 240
- seated and full-body skeleton tracking
- optional raw and enhanced skeleton overlays
- velocity-aware adaptive joint smoothing
- short, decaying prediction through momentary joint loss
- tracked, inferred and predicted joint colours
- body-only depth hologram rendering
- live camera-space 3D point-cloud rendering
- adjustable 3D detail, point size and surface shading
- mouse-drag orbit, mouse-wheel zoom and view reset
- body-only and full-scene point-cloud modes
- motion-history and depth-distance heatmaps
- software brightness and contrast adjustment
- clean camera-output screenshots from 0.5× to 3× scale
- JSON recording of raw and enhanced skeleton data
- live FPS, tracking state and point-count reporting
- ASCII PLY export for external 3D tools

Implemented and ready for the next local validation pass:

- camera-composition video recording
- depth-hologram video recording
- 3D point-cloud render recording
- selectable 5–30 FPS output
- selectable JPEG quality
- 320 × 240 through 1920 × 1440 output dimensions
- asynchronous encoding with a bounded queue
- written-frame and dropped-frame reporting
- dependency-free MJPEG AVI output

The video encoder does not currently record audio.

## Windows studio interface

Kinect Reframe now uses an OBS-style desktop workspace rather than the original prototype control strip.

The application shell includes:

- File, View, Tools and Help menus
- resizable Scenes and Sources dock
- Camera, Depth, 3D and Split preview modes
- context-aware Properties inspector
- dedicated Recording, Capture & Files and Session Status docks
- persistent status bar
- compact charcoal and blue Windows styling
- toast notifications
- Windows taskbar recording state
- F11 fullscreen preview
- F9 projector preview window
- saved window position, size and working settings
- persistent custom scenes
- user-facing crash reports and local diagnostic logs

Built-in scenes:

- Clean Camera
- Skeleton Tracking
- Motion Heatmap
- Depth Heatmap
- Depth Hologram
- 3D Point Cloud
- Camera + 3D
- Tracking Debug

Right-click the Scenes dock to save, duplicate, rename or delete custom layouts.

See [`docs/UI_DESIGN.md`](docs/UI_DESIGN.md) for the interface design and shortcuts.

## What the project is

Kinect Reframe currently combines three types of processing:

1. **Kinect SDK tracking** for RGB, depth, player segmentation and skeleton joints.
2. **Temporal signal processing** for smoothing and short-lived joint prediction.
3. **3D geometry** for converting depth pixels into rotatable camera-space points.

The term “enhanced skeleton” refers to the filtered and temporarily predicted result. It does not mean an AI model is currently generating body pose.

## Camera and overlays

| Source or control | Purpose |
| --- | --- |
| Skeleton overlay | Shows the enhanced skeleton. Off by default. |
| Raw skeleton | Adds the unfiltered Kinect skeleton for comparison. |
| Motion heat | Shows recent depth movement and fading trails. |
| Depth heat | Colours pixels by measured distance from the Kinect. |
| Mirror | Flips the complete camera composition. |
| Freeze | Holds the current RGB, depth and tracking frame. |
| Framing grid | Adds rule-of-thirds guides and a centre marker. |
| Body-only render | Excludes untracked room pixels from heatmaps and 3D renders. |
| Seated tracking | Uses Kinect SDK seated upper-body tracking. |
| Output scale | Sets PNG and video output between 320 × 240 and 1920 × 1440. |

Brightness and contrast are display-only adjustments. They do not alter Kinect hardware exposure or saved skeleton coordinates.

The heatmaps are **not thermal imaging**:

- motion heat shows depth change over time
- depth heat shows distance from the sensor

## Tracking behaviour

The enhanced skeleton uses a velocity-aware temporal filter:

- stationary joints receive stronger smoothing to reduce jitter
- fast movement receives a quicker response to reduce lag
- briefly lost joints continue along a short, decaying velocity estimate
- predicted joints remain amber and time out after eight frames

Predicted joints are never labelled as directly observed measurements.

## 3D rendering

Open the **3D** preview:

- drag with the left mouse button to orbit
- use the mouse wheel to zoom
- use **Detail** to trade performance for density
- use **Point size** to switch between fine particles and a more solid appearance
- toggle **Surface shading** to compare flat hologram colour with depth-derived lighting
- toggle **Body-only render** to switch between the tracked person and visible room
- use **Reset 3D view** to restore the default camera angle

The renderer converts each valid Kinect depth sample into a camera-space `X/Y/Z` point. Local depth neighbours estimate surface orientation for shading.

### Why the back can look like another face

A single Kinect only measures surfaces visible from its own viewpoint. The live renderer is therefore a **single-view 2.5D point cloud**, not a complete scan of every side of the body.

When the view is rotated behind the subject, it shows the reverse side of the same front-facing depth shell. Facial relief can therefore appear on the back of the head. This is expected single-camera behaviour, not captured rear geometry.

A true all-round model would require moving or multiplying the sensor, accumulating registered frames, or explicitly inferring unseen geometry. Kinect Reframe currently renders measured points only.

## Video recording

- **Record camera video** captures the RGB composition and visible overlays.
- **Record render video** captures the selected depth or 3D renderer.
- **Video FPS** selects 5–30 output frames per second.
- **JPEG quality** controls MJPEG compression quality.
- **Output scale** controls final AVI dimensions.

Encoding happens on a worker thread. A bounded queue prevents compression from blocking Kinect capture. When the encoder cannot keep up, frames are dropped and counted rather than stalling the live application.

Video files are written as `.avi` with MJPEG video and no audio. See [`docs/VIDEO_RECORDING.md`](docs/VIDEO_RECORDING.md).

## Tracking-data recording

**Record tracking data** saves motion-capture data rather than video. Files use `.krs.json` and contain raw joints, enhanced joints, tracking state and prediction state for each frame.

Runtime files are written beside the executable:

```text
captures/    PNG interface and camera-output images
recordings/  timestamped .krs.json skeleton sessions
videos/      camera, depth-render and point-cloud .avi files
exports/     body or scene .ply point clouds
```

Per-user application state is stored under:

```text
%LOCALAPPDATA%\KinectReframe\settings.xml
%LOCALAPPDATA%\KinectReframe\Logs\
```

Analyse a skeleton recording with Python 3:

```powershell
python .\tools\analyse_recording.py .\recordings\session-YYYYMMDD-HHMMSS.krs.json
```

## Requirements

- Windows 10 or Windows 11
- Xbox 360 Kinect with USB and external power adapter
- Kinect for Windows SDK 1.8
- Visual Studio 2022 with the **.NET desktop development** workload
- .NET Framework 4.8 targeting pack

The application builds as **x86** for Kinect SDK 1.8 compatibility and includes a per-monitor DPI-aware Windows manifest.

## Build and run

1. Install Kinect for Windows SDK 1.8.
2. Confirm `Kinect Explorer-WPF` or `Skeleton Basics-WPF` can access the sensor.
3. Clone this repository.
4. Open `KinectReframe.sln` in Visual Studio.
5. Select `Debug | x86`.
6. Build and run `KinectReframe.App`.

PowerShell:

```powershell
.\scripts\check-environment.ps1
.\scripts\build.ps1
```

Create a release ZIP:

```powershell
.\scripts\package-release.ps1
```

The package is written under `dist/`.

## Architecture

```text
Kinect RGB stream ───────────────> live camera composition
                                    ├────────> scaled PNG output
                                    └────────> async MJPEG AVI recorder
Kinect depth + player index ─────> depth hologram renderer
                 ├───────────────> motion/depth heatmaps
                 └───────────────> camera-space X/Y/Z mapping
                                    ├────────> configurable shaded point cloud
                                    ├────────> projector and render recorder
                                    └────────> PLY exporter
Kinect skeleton ─────────────────> raw skeleton
                  └──────────────> adaptive smoothing + short velocity prediction
                                   └────────> enhanced skeleton + JSON recorder
```

## Planned work

Near-term:

- local validation of the expanded studio interface and video recorder
- replay and side-by-side tracking comparison
- tracking jitter and latency metrics
- RGB-aligned point-cloud colouring
- additional point-cloud render styles
- better depth-hole and occlusion-edge treatment
- source opacity, lock and reorder controls
- packaged icon and installer

Optional later research:

- modern pose estimation through ONNX Runtime
- Kinect depth mapped onto RGB pose landmarks
- confidence-weighted fusion between Kinect and model estimates
- multi-frame or multi-camera reconstruction
- ghost replay and motion trails

See [`docs/ROADMAP.md`](docs/ROADMAP.md) for the staged plan.

## Important limitation

Kinect Reframe cannot recover body parts or surfaces that no sensor observed. Predicted joints and any future inferred geometry must remain visibly distinct from measured data. The project is an experimental visual tracker, not a medical or safety system.
