# Decokee Quake Tray

A small Windows tray companion for the Decokee Quake desktop device.

The app replaces the vendor always-on companion software for day-to-day use. It
keeps the Quake display alive, exposes useful hardware controls, and adds simple
desktop window switching through the device knob without requiring
administrator privileges or network access.

![Decokee Quake Tray controls](assets/tray-controls.png)

## Features

- Display keepalive for the Quake transparent monitor mode.
- Screen luminance control.
- Knob RGB matrix brightness and color control.
- Rotary knob window switching on the Quake display.
- Optional knob modes for Quake luminance and system volume.
- Button actions for focusing the current Quake window or moving the active
  window to the Quake display.
- Automatic pause of keepalive while the primary display is off.
- Light and dark mode aware tray popup.
- Single-instance guard.
- No internet communication.
- No administrator privilege requirement.
- No vendor runtime dependency.

## Requirements

- Windows
- Decokee Quake device connected over USB
- .NET 10 SDK, only when building from source

## Install

Download the latest installer from the GitHub Releases page:

- `DecokeeTray-<version>-setup.exe`
- `DecokeeTray-<version>-win-x64.msi`

Both installers are per-user installers. They install the app under:

```text
%LOCALAPPDATA%\Programs\DecokeeTray
```

The installer also adds a startup shortcut for the current Windows user.

## Build From Source

```powershell
dotnet build .\DecokeeTray.slnx
dotnet run --project .\DecokeeTray
```

## Release Build

The repository includes a GitHub Actions workflow that builds both MSI and EXE
installers for tagged releases.

Create a tag and push it:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

The workflow publishes:

- `DecokeeTray-<version>-setup.exe`
- `DecokeeTray-<version>-win-x64.msi`

## Notes

This is an independent community utility. It is not affiliated with Decokee.

## License

MIT
