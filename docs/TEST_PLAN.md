# Kinect Reframe hardware test plan

Run this after pulling the latest `main` branch. Close Kinect Explorer, Skeleton Basics and Kinect Studio first because only one application should own the sensor.

## 1. Environment and build

```powershell
.\scripts\check-environment.ps1
.\scripts\build.ps1
```

Expected:

- Kinect SDK 1.8 is found
- `Microsoft.Kinect.dll` is found
- the solution builds as `Debug | x86`
- the app opens without a XAML initialization error

## 2. Clean startup

Expected initial visual state:

- RGB camera visible
- mirror enabled
- skeleton hidden
- raw skeleton disabled
- motion heat hidden
- depth heat hidden
- grid hidden
- seated tracking enabled
- body-only rendering enabled
- 3D detail set to 4
- point size set to 2
- 3D shading enabled
- output scale set to 1.0x / 640 x 480

Confirm the camera and depth panels update at close to 30 FPS.

## 3. Webcam quality-of-life controls

Test each control independently:

- **Mirror** flips the camera, skeleton and heatmap together
- **Freeze** holds all current streams and displays `FRAME FROZEN`
- **Grid** shows rule-of-thirds lines and a centre marker
- **Focus camera** hides the right panel and expands the camera
- **Brightness** adjusts the displayed RGB image without affecting depth
- **Contrast** adjusts the displayed RGB image without affecting depth
- **Save camera output** creates a clean PNG containing RGB plus enabled overlays
- **Save app snapshot** creates a PNG containing the complete interface
- **Reset camera** returns to the neutral display state and output scale 1.0x

Test camera-output scaling at:

- 0.5x = 320 x 240
- 1.0x = 640 x 480
- 2.0x = 1280 x 960
- 3.0x = 1920 x 1440

Confirm the dimensions appear in the generated filename and image properties.

## 4. Seated skeleton tracking

1. Leave **Seated tracking** enabled.
2. Turn **Skeleton** on.
3. Move the head, shoulders, elbows, wrists and hands.
4. Turn **Raw skeleton** on to compare grey raw joints against the enhanced overlay.
5. Hold both hands still and look for reduced enhanced-joint jitter.
6. Move a hand quickly and check that the enhanced skeleton does not lag excessively.
7. Move a hand behind the desk or torso briefly and look for an amber joint continuing along a short decaying path.
8. Keep the hand hidden and confirm prediction times out rather than drifting indefinitely.
9. Turn **Skeleton** off and confirm the camera becomes clean again.

Then disable **Seated tracking**, stand farther back and confirm full-body tracking.

## 5. Motion heatmap

1. Turn **Motion heat** on.
2. Wave one hand while keeping the torso still.
3. Confirm moving depth regions create a coloured fading trail.
4. Increase **Motion threshold** if stationary regions flicker.
5. Increase **Trail** for longer persistence or reduce it for faster fading.
6. Toggle **Body-only renders** and compare person-only against room-inclusive motion.
7. Press **Clear heat** and confirm the trail disappears.

The motion heatmap represents depth change, not temperature.

## 6. Depth heatmap

1. Turn **Depth heat** on.
2. Move closer to and farther from the Kinect.
3. Confirm colours change with measured distance.
4. Confirm enabling depth heat disables motion heat and vice versa.
5. Test body-only and full-scene modes.

The depth and RGB cameras have different fields of view, so the first overlay may not align perfectly at the edges. RGB/depth registration is a later refinement.

## 7. Depth hologram and 3D point cloud

- confirm the body-only depth hologram follows the player mask
- open **3D POINT CLOUD**
- drag to orbit
- use the mouse wheel to zoom
- compare 3D detail levels 1 through 4
- compare point sizes 1 through 4
- toggle **3D shading** and confirm shaded mode reveals more surface form
- monitor FPS at detail 4 and point size 4
- press **Reset 3D** and confirm default quality and camera angle return
- disable body-only rendering to include the room

Export testing is optional during normal development.

## 8. Tracking-data recording

1. Press **Record tracking data**.
2. Move for approximately ten seconds.
3. Press **Stop and save tracking**.
4. Locate the `.krs.json` file under `recordings`.
5. Run:

```powershell
python .\tools\analyse_recording.py .\recordings\<file>.krs.json
```

Confirm the report contains duration, frame rate and correction metrics.

This records skeleton data, not RGB or depth video.

## 9. Report useful failures

Capture these details for any failure:

- exact compiler or runtime message
- screenshot of the app
- whether Kinect Explorer still works
- current control state
- FPS shown in the app
- whether the failure happens in seated and full-body modes
