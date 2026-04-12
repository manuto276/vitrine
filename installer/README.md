# Building the Installer

The Vitrine installer is built with [Inno Setup](https://jrsoftware.org/isinfo.php) 6.x.

## Prerequisites

- A release build already produced (`make release`)
- One of:
  - **Devcontainer**: Wine + Inno Setup are pre-installed (rebuild container if missing)
  - **Windows**: [Inno Setup 6.x](https://jrsoftware.org/isinfo.php) with `iscc` in PATH

## Build

```bash
# Build everything: themes + .NET release + installer
make installer
```

The build script (`installer/build.sh`) auto-detects the environment:
- **Linux/devcontainer**: uses Wine to run ISCC.exe
- **Windows**: uses native `iscc`

Output: `publish/installer/VitrineSetup-1.0.0.exe`

## What the installer does

### Install
- Installs to `%LOCALAPPDATA%\Programs\Vitrine` (per-user, no admin required)
- Creates Start Menu shortcuts
- Optional: desktop shortcut
- Optional: auto-start with Windows (HKCU registry key)
- Optional: launch Vitrine after install

### Uninstall
- Kills Vitrine process if running
- Removes all installed files
- Removes Start Menu and desktop shortcuts
- Removes auto-start registry key
- **Asks** whether to delete `%APPDATA%\Vitrine` (themes, settings, logs)
  - Default: No (preserves user data)
  - Yes: removes everything
