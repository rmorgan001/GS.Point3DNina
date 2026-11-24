<div align="center">

[![Green Swamp Software](assets/moon-half.jpg)](http://www.greenswamp.org/)

# GS.Point3DNina

**A live 3D view of your telescope mount, right inside [NINA](https://nighttime-imaging.eu/).**

[![Build](https://github.com/rmorgan001/GS.Point3DNina/actions/workflows/build.yml/badge.svg)](https://github.com/rmorgan001/GS.Point3DNina/actions/workflows/build.yml)
[![License: GPL v3](https://img.shields.io/badge/license-GPL--3.0-blue.svg)](LICENSE.txt)
[![NINA](https://img.shields.io/badge/NINA-3.3.0.1060%2B-informational.svg)](https://nighttime-imaging.eu/)
[![.NET](https://img.shields.io/badge/.NET-10-512bd4.svg)](https://dotnet.microsoft.com/)

</div>

GS.Point3DNina adds a **3D Model** panel to NINA's Imaging tab. It draws your telescope, mount and support in a real-time 3D scene and moves the model as NINA reports where the mount is pointing. At a glance you can see what the mount is doing, which side of the pier it is on, and whether the tube is about to swing into the tripod.

The scene is the same viewer used by Green Swamp Software's Point3D for GS Server, brought into NINA as a dockable panel.

## Features

- **Follows your mount.** The model turns with the RA and Dec axes of a German equatorial mount. It uses the mount's site latitude, so pier tilt and compass match your location in either hemisphere.
- **Build your rig from parts.**
  - *Mount:* Sky-Watcher EQ6-R Pro, HEQ5 Pro, EQ8-R Pro, CQ350 Pro, EQM-35 Pro and Wave 150i Pro, ZWO AM5, iOptron CEM40 and CEM70.
  - *Support:* a tripod or pier in several sizes.
  - *Telescope:* refractor, reflector, Schmidt-Cassegrain, Ritchey-Chretien (solid or truss), Newtonian or Maksutov-Cassegrain, scaled from 50% to 200% of the mount's own size.
- **Ready-made models.** Turn the assembled model off to use simpler telescope models, including Alt-Az and polar (fork) mounts as well as German polar. The assembled model is always drawn as a German equatorial mount, so turn it off for an Alt-Az or fork mount.
- **Your look.** Choose the model colour, a background (nebulae and galaxies, a generated star field, or a plain dark sky), a ground grid, and N/E/S/W letters in four sizes.
- **Colour themes.** Dark, Day, Dusk or Night. Dusk is amber only and Night is red only, so the view keeps your eyes dark adapted at the telescope.
- **Compass heading.** An optional readout in the corner of the view with a needle pointing north and the bearing the camera faces. Click it to turn the view to face the pole: north, or south when your latitude is south. It is off by default.
- **Coordinates.** Chips along the top of the view show the mount's RA, Dec, Az, Alt, local hour angle and local sidereal time. Choose which ones show, and double-click RA to switch between hours and degrees.
- **Save and reset the view.** Orbit, zoom and pan to the angle you like, then use Save view on the Options page to keep it for next time. Reset view returns to it, and Use default goes back to the standard view.
- **Polling time.** How often the model follows the mount. The default, Match NINA, follows every report NINA makes (its Device polling interval). Choose a slower time (3 to 10 seconds, in steps of 1) to update the model less often; it can never be faster than NINA's own interval. The model glides across the whole time, so it still moves smoothly.
- **Fine adjustment.** RA and Dec offsets of up to 90 degrees line the model up with how your own mount is set up.
- **Remembered settings.** Changes made on the Options page show in the 3D Model panel straight away, even while you are on another NINA tab, and persist between sessions.

## Requirements

- Windows
- NINA 3.3.0.1060 or later

## Usage

1. Open NINA's **Imaging** tab and add the **3D Model** panel from the panel list.
2. Connect your telescope under **Equipment, Mount**. The status line under the model shows which mount is connected.
3. Open **Options, Plugins**, select GS.Point3DNina, and choose the mount, support, telescope, colour and background you like.

With no telescope connected the model sits in its home position, so you can set up the look before you head out.

### Smooth movement and lag

NINA reports the mount's position once per **Device polling interval** (Options, General). The model glides from one reported position to the next over about one and a half polling intervals, so it moves smoothly but trails the real mount by about one interval. For a closer match, lower the polling interval in NINA. To follow the mount less often than NINA reports it, set the plugin's Polling time option to a slower time. The glide length follows the setting each time the panel is shown, so after changing it just switch to the Imaging tab again.

## Development

### Prerequisites

- .NET 10.0.4xx SDK (see `global.json`)
- PowerShell 7
- Visual Studio 2026 (optional): open `GS.Point3DNina.slnx`

### Build and test

```powershell
dotnet build
dotnet test
```

Tests cover metadata, composition and selected feature behaviour. WPF tests run on an STA thread with isolated host data under their output directory.

### Deploy to a local NINA

1. Copy `local.props.example` to `local.props` (git-ignored) and set `NinaExecutable` to your NINA install. Set `PluginDeployDirectory` to use a separate test installation.
2. Close NINA.
3. Run the `DeployPlugin` target on the project under `src`.

Normal builds never deploy.

### Project layout

| Path | Contents |
| --- | --- |
| `src/GS.Point3DNina` | The plugin: lifecycle (`Plugin.cs`), dockable panel, options, WPF views and the bundled Babylon.js viewer, models and backgrounds under `Assets/` |
| `tests/GS.Point3DNina.Test` | Metadata, composition and plugin tests |
| `build/` | Packaging targets and `Build-Release.ps1` |
| `Plugin.props` | Plugin identity and manifest metadata. The plugin GUID is permanent, so never change it. |
| `assets/` | Icon and images used by the plugin listing |

### Package a release

```powershell
./build/Build-Release.ps1 -Tag v1.0.0.0 -Repository rmorgan001/GS.Point3DNina
```

This builds, tests and packages without uploading. The ZIP and manifest are written to `artifacts/release`. Versions have four numeric components, each from 0 to 65534. Do not include NINA assemblies in the package.

Pushing a tag such as `v1.0.1.0` runs the Release workflow, which builds, tests, packages and publishes the release. The tag becomes the plugin version.

See the [NINA plugin developer handbook](https://github.com/isbeorn/nina.plugin.template/blob/master/docs/README.md) for [publishing](https://github.com/isbeorn/nina.plugin.template/blob/master/docs/publishing.md) and [testing and debugging](https://github.com/isbeorn/nina.plugin.template/blob/master/docs/testing-and-debugging.md).

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## License

GS.Point3DNina is free software, released under the [GNU General Public License v3.0](LICENSE.txt). It is part of [Green Swamp Software](http://www.greenswamp.org/), which makes astronomy tools for telescope owners, including GS Server and the Point3D 3D mount views. Visit [greenswamp.org](http://www.greenswamp.org/) for downloads, guides and screenshots.
