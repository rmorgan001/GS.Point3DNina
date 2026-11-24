# Changelog

## 1.0.4.0

- The 3D viewer of a panel that NINA has replaced is now freed, instead of staying alive until NINA closes.
- NINA's plugin manager now links to this changelog, and each release's notes are taken from it.
- Every build is now also compiled against the oldest supported NINA version (3.3.0.1060).
- README: Save view and the options that apply while the panel is hidden are described as they now work.

## 1.0.3.0

- Fixed the coordinate labels (RA, Dec, Az, Alt, LHA, LST) being too dark to read.
- Fixed options changed on the Options page (theme, colour, background, coordinates and the rest) not showing in the 3D Model panel straight away, and Save view not working while the panel was hidden.

## 1.0.2.0

- Colour themes: Dark, Day, Dusk (amber only) and Night (red only).
- Optional compass heading readout. Click it to turn the view to face north.
- Coordinate chips along the top of the view: RA, Dec, Az, Alt, local hour angle and local sidereal time. Choose which show, and double-click RA to switch between hours and degrees.
- Save view, Reset view and Use default for the camera.
- The plugin assembly is now named GS.Point3DNina.dll (it was GS_Point3DNina.dll). The plugin GUID is unchanged.
- Rewritten README, and unit tests for the maths and the viewer assets.

## 1.0.0.0

- Initial release.
