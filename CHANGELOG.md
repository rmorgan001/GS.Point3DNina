# Changelog

## Unreleased

- A message from the 3D viewer that cannot be handled is now logged instead of reaching NINA.
- One unusable value in the settings file no longer resets all your settings; only that setting returns to its default.
- The README now explains the model's lag behind the mount (the polling interval) and that the assembled model is German equatorial only.
- Line endings are now consistent (`.gitattributes`, `.editorconfig`).
- Settings saving and loading is now covered by tests.
- A mount that reports no position (NaN) no longer causes an error; the model stays where it was.
- Showing the panel again while the viewer is still starting no longer starts it twice.
- Options changed while the viewer was starting are no longer lost.
- Settings: a number that names no option is ignored, and the file is written safely (no half-written file after a crash).
- The viewer is freed when the plugin shuts down, cannot navigate away from its own page, and no longer redraws unchanged coordinates.
- Removed the unused `OptionsOpen` setting and `recentre` command.
- The build and release workflows now use GitHub Actions versions that run on Node.js 24 (the Node.js 20 deprecation warning is gone).

## 1.0.5.0

- Fixed a harmless "the 3D viewer could not start (E_ABORT)" error that 1.0.4.0 wrote to NINA's log at startup.
- More unit tests, covering the model file choice, pose, offsets and assembled-model logic.

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
