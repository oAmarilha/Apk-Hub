<h1 align="center">APK Hub</h1>

<p align="center">
  <strong>Python + Qt desktop tool for Android APK, ADB, Samsung Kids and Parental Care operations.</strong>
</p>

## Status

This repository was converted from the old WPF/.NET application to a Python/PySide6 Qt application.

## Main Features

- Detect connected Android devices with `adb devices` and show each device model.
- Select or drag and drop multiple APK files.
- Install queued APKs sequentially with `adb install -r -d`.
- Run manual ADB commands against the selected device.
- Clear or uninstall packages by package name.
- Capture full-device or package-filtered logcat, start/stop streaming, and save logs.
- Start/stop scrcpy screen sharing.
- Record the device screen and pull the recording into `Documents/ApkHub/Log/ScreenRecords`.
- Samsung Kids helpers: clear app data, uninstall selected Kids apps, clear logcat, view app logcat.
- Parental Care helpers: push/install Parental APKs, uninstall, remount, clear package data, and logcat.

## Requirements

- Python 3.10 or newer.
- `adb` available through Android SDK, `ANDROID_HOME`/`ANDROID_SDK_ROOT`, or `PATH`.
- `scrcpy` available through `PATH` for Linux. Windows builds can use the bundled Windows scrcpy/adb files in `apk_hub/vendor/adb/windows`.
- `aapt` is optional. If present, APK Hub uses it to show app labels during package uninstall/clear messages.

## Run On Linux

```bash
./ApkHub.sh
```

The launcher creates `.venv`, installs the package, and starts the Qt app. If a built binary exists at `dist/ApkHub`, the launcher runs that instead.

## Run From Source

```bash
python -m venv .venv
source .venv/bin/activate
python -m pip install -e .
python -m apk_hub
```

On Windows PowerShell:

```powershell
py -3 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -e .
.\.venv\Scripts\python.exe -m apk_hub
```

## Build Linux Binary And Launcher

```bash
./scripts/build_linux.sh
```

Output:

- `dist/ApkHub`
- `dist/ApkHub.sh`

## Build Windows EXE

Run this on Windows, not Linux:

```powershell
.\scripts\build_windows.ps1
```

Output:

- `dist\ApkHub.exe`

PyInstaller does not cross-compile a Windows `.exe` from Linux, so the Windows build script must run on a Windows machine.

## Runtime Files

APK Hub writes logs and generated files under:

```text
Documents/ApkHub/Log
```

Important subfolders:

- `Output/StatusOutputText.txt`
- `Logcat/<package-or-full_device>/<timestamp>/`
- `ScreenRecords/<device>/<timestamp>/`
- `Crashes/crash_log.txt`

## Notes

- The UI was redesigned as a dark Qt workbench with red action accents, matching the modern visual direction requested.
- The old WPF solution, Visual Studio installer project, and MSI/setup artifacts were removed from the active codebase.
- Windows ADB/scrcpy binaries are kept only as packaged vendor tools for Windows runs/builds.
