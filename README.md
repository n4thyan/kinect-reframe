# Kinect Reframe

Kinect Reframe is an experimental tracking and rendering lab for the Xbox 360 Kinect.

The long-term goal is to combine the Kinect's RGB, depth and skeleton streams with modern pose estimation and temporal tracking so the old sensor can produce cleaner seated tracking and richer real-time body renders.

## Current prototype

The WPF prototype includes:

- Kinect SDK 1.8 sensor discovery and startup
- RGB camera feed at 640 x 480
- depth feed at 320 x 240
- seated and full-body skeleton modes
- optional raw and enhanced skeleton overlays
- velocity-aware adaptive smoothing
- short decaying joint prediction through momentary tracking loss
- tracked, inferred and predicted joint colours
- body-only depth hologram render using the Kinect player mask
- live camera-space 3D point-cloud renderer
- adjustable 3D detail, point size and surface shading
- mouse-drag orbit, mouse-wheel zoom and view reset
- body-only and full-scene point-cloud modes
- ASCII PLY export for Blender, MeshLab and other 3D tools
- motion-history and depth-distance heatmaps
- mirror, freeze, framing grid and camera-focus controls
- software brightness and contrast adjustment
- separate application and clean camera-output screenshots
- camera-output scaling from 0.5x to 3x
- JSON recording of raw and enhanced skeleton data
- live FPS, tracking status and sampled point count

The skeleton and both heatmap overlays start **off**, leaving a clean camera view. Every visual layer uses a clear on/off pill toggle.

The prototype deliberately starts without a large AI dependency. It creates a measurable baseline before pose-model fusion is added.

## Camera and overlay controls

| Control | Purpose |
| --- | --- |
| Skeleton | Shows the enhanced skeleton overlay. Off by default. |
| Raw skeleton | Adds the unfiltered Kinect skeleton for comparison. |
| Motion heat | Shows recent depth movement and fading trails. |
| Depth heat | Colours pixels by measured distance from the Kinect. |
| Mirror | Flips the entire camera composition, including overlays. |
| Freeze | Holds the current RGB, depth and tracking frame. |
| Grid | Adds rule-of-thirds guides and a centre marker. |
| Focus camera | Hides the right renderer panel and enlarges the camera. |
| Body-only renders | Excludes untracked room pixels from heatmaps and 3D renders. |
| Seated tracking | Uses Kinect SDK seated upper-body tracking. |
| Output scale | Saves the clean camera composition between 320 x 240 and 1920 x 1440. |

Brightness and contrast are display-only adjustments. They do not alter the Kinect's hardware exposure or the data saved in skeleton recordings.

The heatmaps are **not thermal imaging**. Xbox 360 Kinect cannot measure body temperature:

- motion heat shows depth change over time
- depth heat shows distance from the sensor

Use **Clear heat** to remove accumulated motion trails and **Reset camera** to restore the neutral camera view.

## Tracking behaviour

The enhanced skeleton uses a velocity-aware temporal filter:

- slow or stationary joints receive stronger smoothing to reduce jitter
- fast deliberate movement receives a quicker response to reduce visible lag
- briefly lost joints continue along a short, decaying velocity estimate
- predicted joints remain amber and time out after eight frames

The system does not claim that predicted joints were directly observed by the Kinect.

## 3D controls

Open the **3D POINT CLOUD** tab in the right-hand panel:

- drag with the left mouse button to orbit the live depth cloud
- use the mouse wheel to zoom
- use **3D detail** to trade performance for denser geometry
- use **Point size** to switch between fine particles and a more solid surface
- toggle **3D shading** to compare flat hologram colour against depth-derived surface lighting
- toggle **Body-only renders** to switch between the tracked person and the visible room
- use **Reset 3D** to restore the default view and quality settings

The renderer uses the Kinect SDK coordinate mapper to convert depth pixels into real camera-space `X/Y/Z` points. Local depth neighbours estimate surface orientation, adding stable shading that reveals more facial and torso form without inventing geometry.

## Recordings and output

The button labelled **Record tracking data** saves motion-capture data rather than video. Files use the `.krs.json` extension and contain raw joints, enhanced joints, confidence state and prediction state for each frame.

Actual RGB/depth video encoding is planned separately. The current **Output scale** control applies to clean camera PNG output and is intended to be reused by the future video recorder.

Runtime files are written beside the executable and ignored by Git:

```text
captures/    PNG interface and camera-output images
recordings/  timestamped .krs.json skeleton sessions
exports/     body or scene .ply point clouds
```

Analyse a recording with Python 3:

```powershell
python .\tools\analyse_recording.py .\recordings\session-YYYYMMDD-HHMMSS.krs.json
```

## Requirements

- Windows 10 or Windows 11
- Xbox 360 Kinect with USB and external power adapter
- Kinect for Windows SDK 1.8
- Visual Studio 2022 with the **.NET desktop development** workload
- .NET Framework 4.8 targeting pack

The project builds as **x86** for broad Kinect SDK 1.8 compatibility.

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
Kinect RGB stream ───────────────> live camera panel and scaled PNG output
Kinect depth + player index ─────> depth hologram renderer
                 ├───────────────> motion/depth heatmaps
                 └───────────────> camera-space X/Y/Z mapping
                                    ├────────> configurable shaded 3D point cloud
                                    └────────> PLY exporter
Kinect skeleton ─────────────────> raw skeleton
                  └──────────────> adaptive smoothing + short velocity prediction
                                   └────────> enhanced skeleton + JSON recorder
```

## Continuous integration

The Windows workflow compiles the WPF and XAML structure with a small Kinect API shim because GitHub-hosted runners do not include Kinect SDK 1.8. Normal builds use the real `Microsoft.Kinect.dll` installed on the development PC.

A successful CI compile does not prove that the physical sensor or Kinect runtime works. Hardware smoke testing remains required.

## Planned work

- replay and side-by-side tracking comparison
- RGB-aligned point-cloud colouring
- RGB and depth video recording
- Kinect depth mapped onto RGB pose landmarks
- modern pose model integration through ONNX Runtime
- confidence-weighted fusion between Kinect and AI estimates
- improved seated tracking around desks and partial occlusion
- motion trails and ghost replays

See [`docs/ROADMAP.md`](docs/ROADMAP.md) for the staged plan.

## Important limitation

Kinect Reframe cannot recover body parts that no sensor or camera can observe. Predicted joints must remain visibly distinct from measured joints. The project is intended as an experimental visual tracker, not a medical or safety system.
