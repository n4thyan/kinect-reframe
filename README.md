# Kinect Reframe

Kinect Reframe is an experimental Xbox 360 Kinect tracking, depth-visualisation and real-time 3D rendering lab.

The current build uses Kinect for Windows SDK 1.8, deterministic filtering and conventional geometry. It does **not** currently embed an LLM or learned pose model. Modern pose-model fusion remains an optional future research direction rather than a claim about the present prototype.

## Current status

The core camera, depth, skeleton, heatmap and point-cloud features have been built and tested against a physical Xbox 360 Kinect on Windows.

Validated features:

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
- mirror, freeze, framing grid and focus-camera controls
- software brightness and contrast adjustment
- clean camera-output screenshots from 0.5× to 3× scale
- JSON recording of raw and enhanced skeleton data
- live FPS, tracking state and point-count reporting
- ASCII PLY export for external 3D tools

Newly implemented and awaiting the next physical hardware test:

- actual camera-composition video recording
- depth-hologram video recording
- 3D point-cloud render recording
- selectable 5–30 FPS output
- selectable JPEG quality
- 320 × 240 through 1920 × 1440 output dimensions
- asynchronous encoding with a bounded queue
- visible elapsed time, written-frame count and dropped-frame count
- dedicated `videos` output folder and output-folder shortcut

The video encoder writes dependency-free **MJPEG AVI** files. It does not currently record audio.

On the tested PC, the application generally remains close to the Kinect's 30 FPS capture rate, with heavier full-scene 3D settings reducing frame rate depending on detail and point size.

The skeleton and heatmap overlays start **off**, leaving a clean camera view.

## What the project is

Kinect Reframe currently combines three types of processing:

1. **Kinect SDK tracking** for RGB, depth, player segmentation and skeleton joints.
2. **Temporal signal processing** for smoothing and short-lived joint prediction.
3. **3D geometry** for converting depth pixels into rotatable camera-space points.

The term “enhanced skeleton” refers to the filtered and temporarily predicted result. It does not mean an AI model is currently generating body pose.

## Camera and overlay controls

| Control | Purpose |
| --- | --- |
| Skeleton | Shows the enhanced skeleton overlay. Off by default. |
| Raw skeleton | Adds the unfiltered Kinect skeleton for comparison. |
| Motion heat | Shows recent depth movement and fading trails. |
| Depth heat | Colours pixels by measured distance from the Kinect. |
| Mirror | Flips the complete camera composition. |
| Freeze | Holds the current RGB, depth and tracking frame. |
| Grid | Adds rule-of-thirds guides and a centre marker. |
| Focus camera | Hides the right renderer panel and enlarges the camera. |
| Body-only renders | Excludes untracked room pixels from heatmaps and 3D renders. |
| Seated tracking | Uses Kinect SDK seated upper-body tracking. |
| Output scale | Sets PNG and video output between 320 × 240 and 1920 × 1440. |

Brightness and contrast are display-only adjustments. They do not alter Kinect hardware exposure or saved skeleton coordinates.

The heatmaps are **not thermal imaging**:

- motion heat shows depth change over time
- depth heat shows distance from the sensor

## Tracking behaviour

The enhanced skeleton uses a velocity-aware temporal filter:

- stationary joints receive stronger smoothing to reduce jitter
- fast deliberate movement receives a quicker response to reduce lag
- briefly lost joints continue along a short, decaying velocity estimate
- predicted joints remain amber and time out after eight frames

Predicted joints are never labelled as directly observed measurements.

## 3D rendering

Open the **3D POINT CLOUD** tab:

- drag with the left mouse button to orbit
- use the mouse wheel to zoom
- use **3D detail** to trade performance for density
- use **Point size** to switch between fine particles and a more solid appearance
- toggle **3D shading** to compare flat hologram colour with depth-derived surface lighting
- toggle **Body-only renders** to switch between the tracked person and visible room
- use **Reset 3D** to restore the default view and quality settings

The renderer converts each valid Kinect depth sample into a camera-space `X/Y/Z` point. Local depth neighbours estimate surface orientation for shading.

### Why the back of the model can look like another face

A single Kinect only measures surfaces visible from its own viewpoint. The live renderer is therefore a **single-view 2.5D point cloud**, not a complete scan of every side of the body.

When the view is rotated behind the subject, the renderer is looking at the reverse side of the same front-facing depth shell. Facial relief can therefore appear mirrored or embossed on the back of the head. This is expected single-camera behaviour, not hidden rear geometry.

A true all-round model would require moving or multiplying the sensor, accumulating registered frames, or explicitly inferring unseen geometry. Kinect Reframe currently renders measured points only.

## Video recording

The two video controls are separate from skeleton-data recording:

- **Record camera video** captures the clean 640 × 480 camera composition, including whichever skeleton, heatmap and grid layers are currently visible.
- **Record render video** captures the currently selected renderer: depth hologram or 3D point cloud.
- **Video FPS** selects 5–30 output frames per second.
- **JPEG quality** controls MJPEG compression quality.
- **Output scale** controls the final AVI dimensions.

Encoding happens on a worker thread. A bounded queue prevents video compression from blocking Kinect capture. When the encoder cannot keep up, frames are deliberately dropped and counted in the recording badge rather than stalling the live application.

While recording, output size, video FPS and JPEG quality are locked. The selected render tab is also locked for render recordings so one file cannot silently switch between depth and point-cloud content.

Video files are written as `.avi` with MJPEG video and no audio. See [`docs/VIDEO_RECORDING.md`](docs/VIDEO_RECORDING.md) for implementation and testing details.

## Tracking-data recording

**Record tracking data** saves motion-capture data rather than video. Files use `.krs.json` and contain raw joints, enhanced joints, tracking state and prediction state for each frame.

Runtime files are written beside the executable:

```text
captures/    PNG interface and camera-output images
recordings/  timestamped .krs.json skeleton sessions
videos/      camera, depth-render and point-cloud .avi files
exports/     body or scene .ply point clouds
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

The project builds as **x86** for Kinect SDK 1.8 compatibility.

## Run it

1. Install Kinect for Windows SDK 1.8.
2. Confirm `Kinect Explorer-WPF` or `Skeleton Basics-WPF` can access the sensor.
3. Clone this repository.
4. Open `KinectReframe.sln` in Visual Studio.
5. Select `Debug | x86`.
6. Build and run `KinectReframe.App`.

PowerShell build:

```powershell
.\scripts\check-environment.ps1
.\scripts\build.ps1
```

## Architecture

```text
Kinect RGB stream ───────────────> live camera composition
                                    ├────────> scaled PNG output
                                    └────────> async MJPEG AVI recorder
Kinect depth + player index ─────> depth hologram renderer
                 ├───────────────> motion/depth heatmaps
                 └───────────────> camera-space X/Y/Z mapping
                                    ├────────> configurable shaded point cloud
                                    ├────────> render-video recorder
                                    └────────> PLY exporter
Kinect skeleton ─────────────────> raw skeleton
                  └──────────────> adaptive smoothing + short velocity prediction
                                   └────────> enhanced skeleton + JSON recorder
```

## Continuous integration

The Windows workflow compiles the WPF and XAML structure with a small Kinect API shim because GitHub-hosted runners do not include Kinect SDK 1.8. Normal builds use the real `Microsoft.Kinect.dll` installed on the development PC.

CI compilation does not replace physical Kinect or video-playback testing.

## Planned work

Near-term:

- validate AVI playback, frame rate, scaling and dropped-frame reporting on the Kinect PC
- replay and side-by-side tracking comparison
- tracking jitter and latency metrics
- RGB-aligned point-cloud colouring
- additional point-cloud render styles
- better treatment of depth holes and occlusion edges
- recording pause/resume and optional automatic file splitting
- substantial UI and interaction redesign

Optional later research:

- modern pose estimation through ONNX Runtime
- Kinect depth mapped onto RGB pose landmarks
- confidence-weighted fusion between Kinect and model estimates
- multi-frame or multi-camera reconstruction
- ghost replay and motion trails

See [`docs/ROADMAP.md`](docs/ROADMAP.md) for the staged plan.

## Important limitation

Kinect Reframe cannot recover body parts or surfaces that no sensor observed. Predicted joints and any future inferred geometry must remain visibly distinct from measured data. The project is an experimental visual tracker, not a medical or safety system.
