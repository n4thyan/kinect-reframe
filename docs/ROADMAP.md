# Kinect Reframe roadmap

## Project scope

The current application uses Kinect SDK 1.8 data, deterministic temporal filtering and conventional 3D geometry. It does not currently contain an LLM or learned pose model.

Modern pose estimation remains an optional later research phase. Present-day features should be described accurately as tracking, signal processing, depth visualisation and 3D rendering.

## Phase 0: hardware baseline

Status: **validated on physical Xbox 360 Kinect hardware**

- confirm Kinect SDK 1.8 colour, depth and skeleton streams
- support seated and default tracking modes
- display player-index body segmentation
- report frame rate and tracked/inferred joint counts

Acceptance criteria met:

- the application starts with one Xbox 360 Kinect connected
- RGB, depth and skeleton data run together at interactive speed
- seated mode switches without restarting the application

## Phase 1: measurable tracking improvements

Status: **adaptive filter implemented; metrics and replay remain**

Completed:

- overlay raw Kinect joints and enhanced joints
- configurable temporal smoothing
- raise responsiveness during deliberate fast movement
- predict briefly missing joints with decaying measured velocity
- record raw and enhanced coordinates to JSON
- keep predictions visually distinct from measurements

Remaining:

- add a replay viewer
- calculate joint jitter while the user is still
- calculate added latency while the user moves
- store active smoothing parameters in each recording

Acceptance criteria:

- raw and enhanced output can be compared on the same recording
- smoothing settings and metrics are written into exported sessions
- predicted joints are visually distinct from measured joints
- predictions decay and time out rather than drifting indefinitely

## Phase 2: body and scene rendering

Status: **live renderer validated; interactive quality controls implemented**

Completed:

- map depth pixels into camera-space coordinates
- render a live body-only point cloud
- allow orbit, zoom and reset controls
- switch between body-only and complete-scene rendering
- export a captured frame as PLY
- use dense player-index sampling for smoother body renders
- add depth-aware surface shading
- add configurable point size and render-density controls
- validate body and room rendering on physical hardware

Observed limitation:

- the current renderer is a single-view **2.5D point cloud**
- rotating behind the subject reveals the reverse side of the front-facing depth shell
- facial relief may therefore appear on the back of the head
- this is expected and must not be presented as captured rear geometry

Remaining:

- map RGB colour onto camera-space points
- add point, voxel and trail render styles
- improve depth-hole and edge treatment
- add optional back-face culling or a front-view orbit limit
- evaluate temporal point accumulation for static reconstruction

Acceptance criteria:

- the user can rotate a body point cloud independently of the physical camera
- live rendering remains interactive on the development PC
- detail and point-size settings visibly alter quality and load
- unseen surfaces are never labelled as measured

## Phase 2.5: camera output and recording

Status: **MJPEG video recorder implemented; physical playback and performance validation pending**

Completed:

- save clean camera compositions at 0.5× to 3× output scale
- include RGB, skeleton, heatmap and grid layers in camera output
- keep tracking-data recording separate and clearly labelled
- record the camera composition to MJPEG AVI
- record the selected depth-hologram or point-cloud render to MJPEG AVI
- support 5–30 FPS output
- support adjustable JPEG quality
- use a bounded worker-thread encoding queue
- report elapsed time, written frames and dropped frames
- lock incompatible settings while a recording is active
- create a dedicated `videos` folder and output-folder shortcut

Validation remaining:

- compile the new recording code on the Kinect development PC
- verify camera, depth-render and point-cloud AVI playback
- confirm output dimensions at 0.5×, 1×, 2× and 3×
- measure dropped frames at several FPS, quality and render-detail combinations
- confirm overlays and mirror state appear correctly in camera recordings
- confirm stopping and closing the application finalise playable files

Later recording improvements:

- pause and resume
- optional audio capture from the Kinect microphone array
- automatic file splitting for long or very large recordings
- additional codecs or an optional FFmpeg backend

Acceptance criteria:

- generated output has the selected dimensions
- video recording does not block Kinect frame processing
- file names and controls clearly distinguish video from skeleton JSON
- stopping produces a playable AVI with correct duration and frame count

## Phase 3: replay, measurement and polish

Priority before adding learned models:

- replay `.krs.json` tracking sessions inside the app
- compare raw and enhanced skeletons from identical recorded frames
- graph per-joint jitter, inferred-frame count and prediction duration
- add presets for seated, responsive and maximum-smoothing modes
- redesign the current prototype UI into a deliberate production interface
- add keyboard shortcuts and clearer mode grouping
- add a first-run hardware and performance guide

## Phase 4: optional modern pose estimation

This phase is research work and is not part of the current feature claim.

- evaluate an ONNX-compatible pose model
- run inference over the Kinect RGB frame
- map 2D model landmarks onto Kinect depth
- convert valid landmarks to camera-space coordinates
- record model confidence beside Kinect tracking state
- measure whether it actually improves the Kinect baseline

Acceptance criteria:

- model landmarks remain aligned with colour and depth
- invalid depth samples are rejected rather than silently guessed
- inference speed and latency are displayed
- documentation distinguishes SDK tracking, filtering and learned inference

## Phase 5: optional confidence-weighted fusion

- compare Kinect and model landmarks in a shared coordinate system
- choose or blend estimates based on confidence and temporal consistency
- enforce basic limb-length and joint-angle constraints
- refine prediction during brief occlusion
- quantify improvement over the deterministic Phase 1 baseline

## Phase 6: reconstruction and experiments

- ghost replay
- hand and body motion trails
- room point-cloud capture
- labelled motion dataset collection
- trainable personal gestures
- multi-frame Kinect Fusion-style reconstruction
- multi-camera calibration experiments

## Non-goals

- claiming hidden limbs or rear surfaces are directly tracked
- describing deterministic filtering as AI
- medical diagnosis or rehabilitation scoring
- background surveillance
- cloud processing by default
- replacing the Kinect runtime before its baseline is measured
