### Plugin Overview
* **Plugin Name:** GS.Point3DNina
* **Version:** 1.1.0.0
* **Release URL:** https://github.com/rmorgan001/GS.Point3DNina/releases/download/v1.1.0.0/GS.Point3DNina.1.1.0.0.zip
* **SHA256 Checksum:** `215FDE632A0212E4C9233941FF695BF6A52DAE28B83B787F5BCD72A1786C47A8`

Adds a **3D Model** panel to NINA's Imaging tab: a live 3D view of the telescope, mount and support that moves as NINA reports the mount's position. Source: https://github.com/rmorgan001/GS.Point3DNina (GPL-3.0). The manifest is the unedited `GS.Point3DNina.1.1.0.0.manifest.json` from that release. It includes a screenshot, tags and a changelog link.

### AI-Use Disclosure
* **Material AI Assistance:** Yes
* **Scope of AI Use:** Claude Code (Anthropic) was used to review and edit this repository, including: tooltips on the settings page; validation of loaded settings and the saved camera view; fixes found in code review (serialised and debounced settings saves, flushing on shutdown, migration of an old saved mount, logging and retry of a failed model load); unit tests; the README, changelog and plugin listing text; and build, deploy and packaging script changes. Most of the code was ported over from my other project GS.Point3DWeb which is a blazor app and the same code is also being used in a port to the new GS Alpaca server.
* **Human Verification & Accountability:** I confirm that as the accountable human maintainer, I have personally reviewed, understood, and tested all plugin code. I have verified its security, privacy, licensing, and provenance, and I am able to independently explain, debug, and maintain this implementation.

### Verification & Testing
* **Unit / Integration Tests:** 299 NUnit tests pass. They cover plugin metadata and composition, pose and axis maths, settings loading and saving, the viewer's asset and model names, and that every option's tooltip is on its own row. CI builds and tests on every push and for each release, and also compiles against the minimum supported NINA version (3.3.0.1060).
* **N.I.N.A. Smoke Test:** Deployed the release build into NINA's plugin folder. NINA loads it cleanly and shows version 1.1.0.0 under Plugins. The settings page, every option's tooltip and the 3D Model panel display correctly. This was tested using GS server using our new ascom conformance tester along with the ASCOM ConformU tool.
* **Privacy & Network Access:** The plugin makes no network calls of its own and sends no telemetry. The 3D viewer runs in a WebView2 that serves only the plugin's own `Assets` folder, through a private virtual host name, and cancels navigation to anywhere else. `viewer.html` and `model3d.js` contain no external addresses (apart from a licence URL in a comment). The plugin stores two things locally: its options in `%LocalAppData%\NINA\Plugins\GS.Point3DNina\settings.json`, and the WebView2 browser data in the same folder.
