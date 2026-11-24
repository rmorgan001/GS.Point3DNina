# Changelog

## 1.0.8.0

- A saved view with numbers that can't be used (a missing or zero zoom distance, or values that aren't finite) is no longer kept or loaded; the default view applies instead.
- Settings read from the file are kept in range: RA and Dec offsets within -90 to 90, a polling time that is Match NINA or 3 to 10 whole seconds, and a `#rrggbb` colour. Anything else returns to its default.
- A model the viewer cannot load is now reported in NINA's log, and is tried again when the panel is shown again or an option changes.
- A settings file saved with the old fork mount now moves to the default mount together with its support and telescope, so choosing another mount afterwards still suggests the right parts.
- Connecting or disconnecting the mount now shows in the model and coordinates at once, even with a slower polling time.
- The model is no longer reloaded when the mount reports its latitude with tiny differences from one poll to the next.
- Choosing a mount now keeps a support or telescope you changed yourself, instead of replacing them.
- The local hour angle chip now shows in the same style as RA and local sidereal time.
- Fixed the Mount type and Background tooltips.
- Options are saved a moment after the last change instead of on every change, and shutting down no longer waits on the UI thread.
- Removed unused code left over from the web app.
- A local deploy now reports the version of the newest changelog entry, and clears `Assets` files that a newer build no longer ships.

## 1.0.7.0

- Every option on the plugin's settings page now has a tooltip explaining what it does.

## 1.0.6.0

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
- The model's glide length now follows NINA's Device polling interval each time the panel is shown, so a restart is no longer needed after changing it.
- Clicking the compass heading now turns the view to face south instead of north when the mount's latitude is south, matching the pole its polar axis points to. It is set at startup and follows a latitude change.
- New "Polling time" option: Match NINA (the default) or a slower time (3 to 10 s in 1 s steps), for following the mount less often than NINA polls it. The glide spans the chosen time.
- The 3D view is now drawn as soon as the Imaging tab is shown, instead of staying blank until clicked.
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
