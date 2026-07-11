# Kinect Reframe roadmap

## Phase 0: hardware baseline

Status: implementation complete, physical validation pending

- confirm Kinect SDK 1.8 colour, depth and skeleton streams
- support seated and default tracking modes
- display player-index body segmentation
- report frame rate and tracked/inferred joint counts
- provide a clean RGB camera view with optional overlays
- add mirror, freeze, grid, focus, brightness and contrast controls

Acceptance criteria:

- the application starts with one Xbox 360 Kinect connected
- RGB, depth and skeleton data run together at interactive speed
- seated mode can be switched without restarting the application
- skeleton and heatmap overlays start hidden
- every visual layer has an obvious on/off state

## Phase 1: measurable tracking improvements

Status: initial implementation included

- overlay raw Kinect joints and enhanced joints
- configurable temporal smoothing
- preserve a joint briefly through momentary tracking loss
- record raw and enhanced coordinates to JSON
- add a replay viewer
- calculate joint jitter while the user is still
- calculate added latency while the user moves

Acceptance criteria:

- raw and enhanced output can be compared on the same recording
- smoothing settings and metrics are written into exported sessions
- predicted joints are visually distinct from measured joints

## Phase 2: body rendering

Status: initial point-cloud renderer included, hardware validation pending

- replace the 2D depth hologram with a real 3D point cloud
- map depth pixels into camera-space coordinates
- allow orbit, zoom and reset controls
- switch between body-only and complete-scene rendering
- add point, voxel and trail render styles
- export a captured frame as PLY

Acceptance criteria:

- the user can rotate a body point cloud independently of the physical camera
- exported point clouds open correctly in Blender or CloudCompare

## Phase 3: modern pose estimation

- evaluate an ONNX-compatible pose model
- run inference over the Kinect RGB frame
- map 2D model landmarks onto Kinect depth
- convert valid landmarks to camera-space coordinates
- record model confidence beside Kinect tracking state

Acceptance criteria:

- model landmarks remain aligned with the colour image and depth map
- invalid and missing depth samples are rejected rather than silently guessed
- inference speed is displayed in the application

## Phase 4: confidence-weighted fusion

- compare Kinect and model landmarks in a shared coordinate system
- choose or blend estimates based on confidence and temporal consistency
- enforce basic limb-length and joint-angle constraints
- add velocity-aware prediction during brief occlusion
- quantify improvement over the Phase 1 baseline

Acceptance criteria:

- a test suite covers coordinate transforms and filtering logic
- seated arm tracking shows lower measured jitter without unacceptable latency
- occlusion predictions time out and never appear as directly measured data

## Phase 5: experiments

Status: motion and depth heatmaps implemented; remaining experiments planned

- ghost replay
- hand and body motion trails
- motion-history heatmap
- depth-distance heatmap
- room point-cloud capture
- labelled motion dataset collection
- trainable personal gestures

## Non-goals for the early versions

- medical diagnosis or rehabilitation scoring
- claiming hidden limbs are directly tracked
- thermal-imaging claims from the Kinect infrared/depth sensor
- background surveillance
- cloud processing by default
- replacing the Kinect runtime before the baseline is understood
