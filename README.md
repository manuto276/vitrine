# Vitrine

A themeable desktop widget engine for Windows, inspired by [Conky](https://github.com/brndnmtthws/conky).

Vitrine renders a full-screen transparent overlay behind your desktop icons using WebView2, and loads themes built with React that display system information, custom widgets, or anything you can build with HTML/CSS/JS.

## Features

- Full-screen transparent overlay embedded behind desktop icons
- Themes are compiled React (JSX) bundles — full control over layout and style
- System info API (`window.vitrine.system`) exposes CPU, RAM, storage, top processes
- Theme switching from the system tray
- Configuration stored in `%APPDATA%\Vitrine\`
- Debug build with file logging for theme development

## Getting Started

### Prerequisites

- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 20+](https://nodejs.org/) (for building themes)
- [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (usually pre-installed on Windows 10/11)

### Build

```bash
# Build themes + .NET project
make build

# Publish self-contained release
make release

# Publish debug build (with file logging)
make debug
```

The output goes to `publish/release/` or `publish/debug/`.

### Run

Launch `Vitrine.exe` from the publish folder. A system tray icon appears — right-click it to switch themes or exit.

## Project Structure

```
├── src/
│   ├── Vitrine.Engine/          # .NET WinForms + WebView2 host
│   │   ├── Core/
│   │   │   ├── DesktopAttacher  # Win32 Progman/WorkerW embedding
│   │   │   ├── ThemeHost        # Lifecycle, tray, config, system info bridge
│   │   │   ├── ThemeWindow      # Full-screen WebView2 Form
│   │   │   ├── Configuration    # APPDATA config.json management
│   │   │   └── Log              # Debug-only file logger
│   │   ├── SystemInfo/
│   │   │   └── SystemInfoProvider  # CPU, RAM, drives, processes via P/Invoke
│   │   └── Themes/
│   │       └── ThemeManifest    # theme.json model
│   └── themes/
│       └── default/             # Default Conky-style React theme
│           ├── src/
│           │   ├── App.jsx      # Main UI (system info panels, bars)
│           │   ├── index.jsx    # React entry point
│           │   └── useSystemInfo.js  # Hook for vitrine.system API
│           ├── vite.config.js   # Builds to single IIFE bundle
│           └── package.json
├── Vitrine.sln
├── Makefile
└── Directory.Build.props        # Redirects bin/obj to .build/
```

## Creating a Theme

A theme is a React project that compiles to a single IIFE JavaScript bundle.

### 1. Scaffold

```bash
mkdir my-theme && cd my-theme
npm init -y
npm install react react-dom
npm install -D vite @vitejs/plugin-react
```

### 2. Configure Vite

```js
// vite.config.js
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  define: {
    'process.env.NODE_ENV': JSON.stringify('production'),
  },
  build: {
    lib: {
      entry: 'src/index.jsx',
      formats: ['iife'],
      name: 'VitrineTheme',
      fileName: () => 'theme.js',
    },
  },
});
```

### 3. Write your theme

```jsx
// src/index.jsx
import React from 'react';
import { createRoot } from 'react-dom/client';

function App() {
  const [info, setInfo] = React.useState(null);

  React.useEffect(() => {
    if (!window.vitrine) return;
    window.vitrine.system.onUpdate(setInfo);
    window.vitrine.system.getInfo().then(setInfo);
  }, []);

  if (!info) return null;

  return (
    <div style={{ color: 'white', padding: 20, fontFamily: 'monospace' }}>
      <p>CPU: {info.cpu.usage}%</p>
      <p>RAM: {(info.memory.used / 1073741824).toFixed(1)} GB</p>
    </div>
  );
}

createRoot(document.getElementById('root')).render(<App />);
```

### 4. Build and install

```bash
npm run build
```

Copy the output to your themes folder:

```
%APPDATA%\Vitrine\themes\my-theme\
├── theme.json    # { "name": "My Theme", "entry": "theme.js" }
└── theme.js      # compiled bundle
```

Right-click the tray icon → **Themes** → select your theme.

### System Info API

The `window.vitrine.system` API is injected into every theme:

```js
// Subscribe to periodic updates (every 2s)
window.vitrine.system.onUpdate((info) => { ... });

// One-shot request
const info = await window.vitrine.system.getInfo();
```

The `info` object:

```json
{
  "system": {
    "hostname": "DESKTOP-ABC",
    "os": "Microsoft Windows 10.0.22631",
    "uptime": 123456
  },
  "cpu": {
    "usage": 23.5,
    "cores": 8
  },
  "memory": {
    "total": 17179869184,
    "available": 8589934592,
    "used": 8589934592,
    "load": 50
  },
  "drives": [
    { "name": "C:\\", "label": "OS", "total": 500107862016, "free": 250053931008, "used": 250053931008 }
  ],
  "processes": [
    { "name": "chrome", "pid": 1234, "memory": 524288000 }
  ]
}
```

## Configuration

Stored in `%APPDATA%\Vitrine\`:

| File | Purpose |
|---|---|
| `config.json` | Active theme selection |
| `themes/` | Installed themes |
| `logs/` | Debug build logs (daily rotation) |

## Debug

Build with `make debug` to enable file logging to `%APPDATA%\Vitrine\logs\vitrine-YYYY-MM-DD.log`. Logs include the full startup sequence, WebView2 initialization, theme loading, and any JavaScript errors from the theme.

## License

[MIT](LICENSE)
