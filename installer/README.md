# Building the Installer

The Vitrine installer is built with [Inno Setup](https://jrsoftware.org/isinfo.php) 6.x.

## Prerequisites

- Inno Setup 6.x installed on Windows
- `iscc` (Inno Setup Compiler) available in PATH
- A release build already produced (`make release`)

## Build

```bash
# Build everything: themes + .NET release + installer
make installer
```

Or manually:

```bash
make release
iscc installer/vitrine.iss
```

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
