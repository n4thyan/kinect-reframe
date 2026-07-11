# Studio UI redesign

Kinect Reframe is moving from a prototype control strip to a desktop studio workspace inspired by OBS and webcam software.

The goal is not to copy OBS visually. The goal is to adopt its useful information architecture:

- the preview is the centre of the application
- scene presets provide fast repeatable setups
- sources control visible layers
- properties are grouped by function
- recording has a dedicated control area
- status information remains visible without covering the preview

## First studio pass

The first redesign introduces:

- a conventional menu bar
- compact application header and connection state
- resizable Scenes and Sources dock
- central preview workspace
- Camera, Depth, 3D and Split preview modes
- resizable Properties dock
- grouped camera, tracking, heatmap, 3D and output settings
- separate Recording, Capture & Files, and Session Status docks
- persistent bottom status bar
- rectangular desktop controls rather than large pill buttons
- a restrained blue accent rather than neon turquoise

## Default scenes

The first scene presets are:

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

These are currently built-in presets. User-created scenes and persistence are planned later.

## Sources

The first Sources dock contains:

- Kinect RGB base source
- enhanced skeleton overlay
- raw skeleton overlay
- motion heat
- depth heat
- framing grid

The source list currently controls visibility. Later passes may add:

- source reordering
- opacity
- blend mode
- lock state
- per-source context menus
- custom names

## Preview modes

### Camera

Uses the full preview area for the composited RGB camera.

### Depth

Uses the full preview area for the player-indexed depth hologram.

### 3D

Uses the full preview area for the interactive point cloud.

### Split

Shows the camera and the selected render side by side.

Video recording automatically changes to the matching preview mode and locks scene or preview switching until the recording stops.

## Properties dock

Properties are grouped into expandable sections:

- Camera
- Tracking
- Heatmaps
- 3D Render
- Output

This removes the old horizontal wall of unrelated sliders and keeps the preview as the main visual focus.

## Visual direction

Current direction:

- dark charcoal workspace
- compact rectangular controls
- 2 px corner radii
- subtle panel dividers
- restrained blue selection state
- standard menu structure
- high information density
- no decorative gradients, glass cards or oversized dashboard spacing

## Next UI passes

1. Validate layout at common desktop resolutions.
2. Replace default WPF tab styling with custom studio tabs.
3. Add source-row visibility and lock icons.
4. Add scene creation, duplication, rename and persistence.
5. Add a real source inspector rather than showing every property group at once.
6. Add keyboard shortcuts.
7. Add toast notifications for saved files and failures.
8. Add a first-run hardware setup screen.
9. Add compact and fullscreen preview modes.
10. Create a proper application icon and identity after the layout is stable.
