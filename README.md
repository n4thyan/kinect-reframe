# Kinect Reframe

Kinect Reframe is an experimental Xbox 360 Kinect tracking, depth-visualisation and real-time 3D rendering lab.

The current build uses the Kinect for Windows SDK 1.8, deterministic filtering and conventional geometry. It does **not** currently embed an LLM or a learned AI model. Modern pose-model fusion remains an optional future research direction rather than a claim about the present prototype.

## Current status

The prototype has now been built and tested against a physical Xbox 360 Kinect on Windows.

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
| Output scale | Saves the clean camera composition between 320 × 240 and 1920 × 1440. |

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
- toggle **Body-only renders** to switch between the tracked person and the visible room
- use **Reset 3D** to restore the default view and quality settings

The renderer converts each valid Kinect depth sample into a camera-space `X/Y/Z` point. Local depth neighbours estimate surface orientation for shading.

### Why the back of the model can look like another face

A single Kinect only measures surfaces visible from its own viewpoint. The live renderer is therefore a **single-view 2.5D point cloud**, not a complete scan of every side of the body.

When the view is rotated behind the subject, the renderer is looking at the reverse side of the same front-facing depth shell. Facial relief can therefore appear mirrored or embossed on the back of the head. This is expected single-camera behaviour, not hidden rear geometry.

A true all-round model would require one of these approaches:

- moving the sensor around a mostly static subject and registering many frames
- rotating the subject while accumulating a reconstruction
- using multiple calibrated depth cameras
- explicitly generating unmeasured geometry, which must be labelled as inferred

Kinect Reframe currently renders measured points only and does not pretend the unseen rear surface was captured.

## Recordings and output

**Record tracking data** saves motion-capture data, not video. Files use `.krs.json` and contain raw joints, enhanced joints, tracking state and prediction state for each frame.

Actual RGB/depth video encoding is planned separately. The **Output scale** control currently applies to clean PNG camera output.

Runtime files are written beside the executable:

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
Kinect RGB stream ───────────────> live camera panel and scaled PNG output
Kinect depth + player index ─────> depth hologram renderer
                 ├───────────────> motion/depth heatmaps
                 └───────────────> camera-space X/Y/Z mapping
                                    ├────────> configurable shaded point cloud
                                    └────────> PLY exporter
Kinect skeleton ─────────────────> raw skeleton
                  └──────────────> adaptive smoothing + short velocity prediction
                                   └────────> enhanced skeleton + JSON recorder
```

## Continuous integration

The Windows workflow compiles the WPF and XAML structure with a small Kinect API shim because GitHub-hosted runners do not include Kinect SDK 1.8. Normal builds use the real `Microsoft.Kinect.dll` installed on the development PC.

CI compilation does not replace physical Kinect testing.

## Planned work

Near-term:

- replay and side-by-side tracking comparison
- tracking jitter and latency metrics
- RGB-aligned point-cloud colouring
- RGB and depth video recording
- additional point-cloud render styles
- better treatment of depth holes and occlusion edges

Optional later research:

- modern pose estimation through ONNX Runtime
- Kinect depth mapped onto RGB pose landmarks
- confidence-weighted fusion between Kinect and model estimates
- multi-frame or multi-camera reconstruction
- ghost replay and motion trails

See [`docs/ROADMAP.md`](docs/ROADMAP.md) for the staged plan.

## Important limitation

Kinect Reframe cannot recover body parts or surfaces that no sensor observed. Predicted joints and any future inferred geometry must remain visibly distinct from measured data. The project is an experimental visual tracker, not a medical or safety system.
