# Studio UI redesign

Kinect Reframe has moved from a prototype control strip to a desktop studio workspace inspired by OBS and webcam software.

The goal is not to copy OBS visually. The application uses the same useful information architecture while retaining its own Kinect-specific workflow:

- the preview is the centre of the application
- scenes provide fast repeatable setups
- sources control visible layers
- the properties inspector follows the selected source or scene
- recording has a dedicated control area
- status information remains visible without covering the preview

## Current Windows application shell

The current studio shell includes:

- conventional File, View, Tools and Help menus
- compact application header and connection state
- resizable Scenes and Sources dock
- central Camera, Depth, 3D and Split preview modes
- resizable Properties inspector
- separate Recording, Capture & Files, and Session Status docks
- persistent bottom status bar
- compact rectangular desktop controls
- custom dark tabs, scene rows, source rows and property sections
- restrained blue selection and focus states
- standard keyboard focus outlines
- Windows taskbar recording state
- toast notifications for saves, recordings and failures
- fullscreen preview with F11 or Escape
- persistent window position, size, scene and working settings
- crash reports under `%LOCALAPPDATA%\KinectReframe\Logs`

## Scenes

Built-in scenes:

| Scene | Result |
| --- | --- |
| Clean Camera | RGB camera with overlays disabled |
| Skeleton Tracking | Enhanced skeleton over RGB |
| Motion Heatmap | Motion-history overlay over RGB |
| Depth Heatmap | Depth-distance overlay over RGB |
| Depth Hologram | Full preview of the player-indexed depth render |
| 3D Point Cloud | Full interactive camera-space point cloud |
| Camera + 3D | Split RGB and point-cloud preview |
| Tracking Debug | Split camera/depth view with raw and enhanced skeletons |

The Scenes dock now also supports persistent custom scenes. Right-click the scene list to:

- save the current layout as a new scene
- duplicate the selected setup
- rename a custom scene
- delete a custom scene

Custom scenes preserve the preview mode, visible overlays, camera settings, tracking settings, heatmap settings, 3D settings and recording output settings. They are stored in the user's local application settings rather than the repository.

## Sources and inspector

The Sources dock contains:

- Kinect RGB base source
- enhanced skeleton overlay
- raw skeleton overlay
- motion heat
- depth heat
- framing grid

Source rows use a compact visibility-state treatment. Selecting or changing a source opens the relevant inspector group:

- camera source and framing controls open **Camera**
- skeleton sources open **Tracking**
- heatmap sources open **Heatmaps**
- depth and point-cloud controls open **3D Render**

This keeps the right dock contextual instead of leaving every property section expanded at once.

Possible later source work:

- source reordering
- opacity and blend modes
- genuine lock state
- custom source names
- additional capture devices

## Preview modes

### Camera

Uses the full preview area for the composited RGB camera.

### Depth

Uses the full preview area for the player-indexed depth hologram.

### 3D

Uses the full preview area for the interactive point cloud.

### Split

Shows the camera and selected renderer side by side.

Video recording automatically changes to the matching preview and locks incompatible scene or preview changes until recording stops.

## Keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| Ctrl+1 | Camera preview |
| Ctrl+2 | Depth preview |
| Ctrl+3 | 3D preview |
| Ctrl+4 | Split preview |
| Ctrl+R | Start or stop camera recording |
| Ctrl+Shift+R | Start or stop render recording |
| Ctrl+S | Save camera frame |
| Ctrl+Shift+S | Save application snapshot |
| Ctrl+O | Open output folder |
| S | Toggle skeleton |
| G | Toggle framing grid |
| M | Toggle mirror |
| Space | Freeze frame |
| F11 | Toggle fullscreen preview |
| Esc | Exit fullscreen preview |
| F1 | About dialog |

The Help menu contains the same shortcut reference.

## Persistence

The application stores per-user desktop state under:

```text
%LOCALAPPDATA%\KinectReframe\settings.xml
```

Stored values include:

- window position, size and maximised state
- selected scene and preview mode
- camera, tracking, heatmap and 3D controls
- output scale, video frame rate and quality
- custom scenes

Repository files and generated captures are not used for desktop preferences.

## Visual direction

The current direction is deliberately closer to a native production tool than a web dashboard:

- dark charcoal workspace
- compact rectangular controls
- 2–3 px corner radii
- subtle panel dividers
- restrained blue selection state
- normal Windows menus and dialogs
- high information density
- no decorative gradients, glass cards or oversized dashboard spacing
- no neon accent applied to every control

## Remaining polish

1. Validate and adjust the layout at 1180×760, 1366×768, 1920×1080 and ultrawide resolutions.
2. Add a genuine source lock and reorder model.
3. Add per-source opacity and blend controls where useful.
4. Add projector-style preview on a second monitor.
5. Add an optional first-run Kinect setup guide.
6. Add a packaged application icon and installer/release build.
7. Add automatic update checking only if the project later publishes signed releases.
