# Kinect Reframe

Kinect Reframe is an experimental tracking and rendering lab for the Xbox 360 Kinect.

The long-term goal is to combine the Kinect's RGB, depth and skeleton streams with modern pose estimation and temporal tracking so the old sensor can produce cleaner seated tracking and richer real-time body renders.

## Current prototype

The first WPF prototype includes:

- Kinect SDK 1.8 sensor discovery and startup
- RGB camera feed at 640 x 480
- depth feed at 320 x 240
- seated and full-body skeleton modes
- raw Kinect skeleton overlay
- custom temporally smoothed skeleton overlay
- short joint hold when tracking is briefly lost
- tracked, inferred and predicted joint colours
- body-only depth hologram render using the Kinect player mask
- adjustable smoothing responsiveness
- JSON recording of raw and smoothed joints
- PNG capture of the application window
- live FPS and joint tracking status

The prototype deliberately starts without a large AI dependency. It creates a measurable baseline before pose-model fusion is added.

## Tracking colours

| Colour | Meaning |
| --- | --- |
| Grey | Raw Kinect skeleton |
| Green | Directly tracked and smoothed |
| Blue | Kinect-inferred joint |
| Amber | Joint briefly held by Kinect Reframe after loss |

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

You can check the local environment from PowerShell:

```powershell
.\scripts\check-environment.ps1
```

You can also build from PowerShell after installing Visual Studio:

```powershell
.\scripts\build.ps1
```

## Output files

Runtime files are written beside the executable and ignored by Git:

```text
captures/
recordings/
```

Skeleton recordings use the `.krs.json` extension. Each frame stores the raw Kinect coordinates, the smoothed coordinates, tracking state and whether the enhanced joint was temporarily predicted.

## Architecture

```text
Kinect RGB stream ───────────────> live camera panel
Kinect depth + player index ─────> body-only hologram renderer
Kinect skeleton ─────────────────> raw skeleton
                  └──────────────> temporal smoother and short occlusion hold
                                  └────────> enhanced skeleton + JSON recorder
```

## Planned work

- velocity-aware One Euro or Kalman filtering
- replay and side-by-side tracking comparison
- 3D point-cloud body renderer
- Kinect depth mapped onto RGB pose landmarks
- modern pose model integration through ONNX Runtime
- confidence-weighted fusion between Kinect and AI estimates
- improved seated tracking around desks and partial occlusion
- motion trails and ghost replays

See [`docs/ROADMAP.md`](docs/ROADMAP.md) for the staged plan.

## Important limitation

Kinect Reframe cannot recover body parts that no sensor or camera can observe. Predicted joints must remain visibly distinct from measured joints. The project is intended as an experimental visual tracker, not a medical or safety system.
