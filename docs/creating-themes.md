# Creating a Vitrine Theme

A Vitrine theme is a React project compiled to a single IIFE JavaScript bundle that renders onto a full-screen transparent overlay behind your desktop icons.

## Theme Files

A theme folder contains:

```
my-theme/
├── theme.json                     # Required — manifest
├── theme.js                       # Required — compiled React bundle (IIFE)
├── theme.css                      # Optional — external stylesheet
├── preview.png                    # Optional — preview image for Control Panel
├── settings.json                  # Optional — user configuration values
└── settings.definitions.json      # Optional — settings schema (see docs/theme-settings.md)
```

## theme.json (Manifest)

Every theme must have a `theme.json`:

```json
{
  "name": "My Theme",
  "description": "A short description of what this theme shows",
  "author": "Your Name",
  "version": "1.0.0",
  "entry": "theme.js"
}
```

| Field | Required | Description |
|---|---|---|
| `name` | Yes | Display name in the Control Panel |
| `description` | No | Short description shown below the name |
| `author` | No | Theme author (default: "Unknown") |
| `version` | No | Theme version (default: "Unknown") |
| `entry` | Yes | JavaScript entry file (usually `theme.js`) |

## Step-by-Step

### 1. Scaffold the project

```bash
mkdir my-theme && cd my-theme
npm init -y
npm install react react-dom
npm install -D vite @vitejs/plugin-react
```

### 2. Configure Vite

Create `vite.config.js`:

```js
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  define: {
    'process.env.NODE_ENV': JSON.stringify('production'),
  },
  build: {
    outDir: 'dist',
    lib: {
      entry: 'src/index.jsx',
      formats: ['iife'],
      name: 'VitrineTheme',
      fileName: () => 'theme.js',
    },
    cssCodeSplit: false,
    rollupOptions: {
      output: {
        assetFileNames: 'theme.[ext]',
      },
    },
  },
});
```

This produces:
- `dist/theme.js` — single IIFE bundle with React included
- `dist/theme.css` — external CSS (if you import `.css` files)

### 3. Write your theme

Create `src/index.jsx`:

```jsx
import React from 'react';
import { createRoot } from 'react-dom/client';
import './theme.css';  // optional
import App from './App';

createRoot(document.getElementById('root')).render(<App />);
```

Create `src/App.jsx`:

```jsx
import React from 'react';

export default function App() {
  const [info, setInfo] = React.useState(null);

  React.useEffect(() => {
    if (!window.vitrine) return;
    window.vitrine.system.onUpdate(setInfo);
    window.vitrine.system.getInfo().then(setInfo);
  }, []);

  if (!info) return null;

  return (
    <div style={{
      position: 'fixed', top: 24, right: 24,
      color: 'white', fontFamily: 'monospace',
      background: 'rgba(0,0,0,0.5)', borderRadius: 12,
      padding: 20,
    }}>
      <h2>My Theme</h2>
      <p>CPU: {info.cpu.usage}%</p>
      <p>RAM: {(info.memory.used / 1073741824).toFixed(1)} GB</p>
      <p>Uptime: {Math.floor(info.system.uptime / 3600)}h</p>
    </div>
  );
}
```

### 4. Add the manifest

Create `theme.json` at the project root:

```json
{
  "name": "My Theme",
  "description": "A minimal system monitor",
  "author": "Your Name",
  "version": "1.0.0",
  "entry": "theme.js"
}
```

### 5. Build

```bash
npm run build
```

Output: `dist/theme.js` (and optionally `dist/theme.css`).

### 6. Install

**Option A: Via Control Panel**

Package as a `.zip` containing `theme.json`, `theme.js`, and optionally `theme.css`, `preview.png`, `settings.json`, `settings.definitions.json`. Open the Control Panel, go to Themes, click **Install Theme**.

**Option B: Manual**

Copy the files to:

```
%APPDATA%\Vitrine\themes\my-theme\
├── theme.json
├── theme.js
└── theme.css        # optional
```

Open the Control Panel and select the theme from the Themes page.

**Option C: Bundled with build**

Place the theme source in `src/themes/my-theme/` and run `make build-themes`. It will be compiled and bundled with the application automatically.

## Preview Image

Add a `preview.png` (or `.jpg`, `.jpeg`, `.webp`) to your theme folder. It appears as the card image in the Control Panel's Themes page. Recommended size: 520x280px.

## CSS

Vitrine loads `theme.css` automatically if it exists alongside `theme.js`. You can use standard CSS with `@font-face`, animations, media queries, etc. Vite bundles all imported CSS into a single `theme.css`.

For fonts and images, use inline styles or Vite's asset handling with `build.assetsInlineLimit` set high to embed everything in the bundle:

```js
// vite.config.js
build: {
  assetsInlineLimit: 1024 * 1024, // inline everything under 1MB
}
```

## System Info API

Every theme has access to `window.vitrine.system`:

```js
// Periodic updates (every 2s)
window.vitrine.system.onUpdate((info) => { ... });

// One-shot request
const info = await window.vitrine.system.getInfo();
```

See the main [README](../README.md) for the full `info` object schema.

## Settings

Themes can optionally expose configurable settings. See [Theme Settings](theme-settings.md) for details.

## Tips

- The overlay is **full-screen** and **transparent** — your theme controls the entire desktop surface
- Use `pointer-events: none` on containers to let clicks pass through to the desktop
- `window.vitrine.settings` contains the user's saved settings (from `settings.json`)
- Test with `make debug` to see JavaScript errors in the log file
- Use `src/themes/default/` as a reference implementation
