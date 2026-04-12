# Theme Settings

Themes can expose configurable settings that users edit from the Control Panel. Settings are defined in two files:

- `settings.json` — Current values
- `settings.definitions.json` — Schema describing the settings (types, defaults, UI structure)

Both files live in the theme folder alongside `theme.json` and `theme.js`.

## settings.json

A flat key-value object with the current setting values:

```json
{
  "showProcessesList": true,
  "processCount": 5,
  "panelPosition": "top-right",
  "panelOpacity": 0.78
}
```

This file is read by Vitrine and injected into the theme as `window.vitrine.settings`. The theme can access any value directly:

```js
const settings = window.vitrine.settings;
if (settings.showProcessesList) {
  // render processes
}
```

When the user modifies settings from the Control Panel, this file is overwritten and the theme is reloaded.

## settings.definitions.json

An **array of sections**, where each section groups related settings:

```json
[
  {
    "title": "Appearance",
    "settings": {
      "panelPosition": {
        "type": "string",
        "default": "top-right",
        "label": "Panel Position",
        "description": "Where to place the panel on screen",
        "options": [
          { "value": "top-left", "label": "Top Left" },
          { "value": "top-right", "label": "Top Right" },
          { "value": "bottom-left", "label": "Bottom Left" },
          { "value": "bottom-right", "label": "Bottom Right" }
        ]
      },
      "panelOpacity": {
        "type": "number",
        "default": 0.78,
        "label": "Panel Opacity",
        "description": "Background opacity of the panel (0.0 to 1.0)"
      }
    }
  },
  {
    "title": "Sections",
    "settings": {
      "showProcessesList": {
        "type": "boolean",
        "default": true,
        "label": "Show Processes",
        "description": "Display the top processes section"
      },
      "processCount": {
        "type": "number",
        "default": 5,
        "label": "Process Count",
        "description": "Number of top processes to display",
        "visibleWhen": { "key": "showProcessesList", "value": true },
        "options": [
          { "value": 3, "label": "3" },
          { "value": 5, "label": "5" },
          { "value": 10, "label": "10" }
        ]
      }
    }
  }
]
```

## Structure

### Section

| Field | Type | Description |
|---|---|---|
| `title` | string | Section header displayed in the Control Panel |
| `settings` | object | Dictionary of setting key → definition |

### Setting Definition

| Field | Type | Required | Description |
|---|---|---|---|
| `type` | string | Yes | `"boolean"`, `"number"`, or `"string"` |
| `default` | any | No | Default value |
| `label` | string | No | Display name (falls back to the key) |
| `description` | string | No | Help text shown below the label |
| `options` | array | No | List of allowed values (renders as dropdown) |
| `visibleWhen` | object | No | Conditional visibility |

### Options

When `options` is present, the Control Panel renders a dropdown instead of a free-form input:

```json
"options": [
  { "value": "top-left", "label": "Top Left" },
  { "value": "top-right", "label": "Top Right" }
]
```

Works with both `"string"` and `"number"` types.

### Conditional Visibility (visibleWhen)

A setting can be shown or hidden based on the value of another setting:

```json
"processCount": {
  "type": "number",
  "visibleWhen": { "key": "showProcessesList", "value": true }
}
```

| Field | Description |
|---|---|
| `key` | The setting key to observe |
| `value` | The value that makes this setting visible |

Supports:
- **boolean**: `true` / `false`
- **string**: exact match
- **number**: exact match

The UI updates immediately when the controlling setting changes — no save required.

## Control Mapping

The Control Panel generates UI controls based on the `type` and `options`:

| Type | Options | Control |
|---|---|---|
| `boolean` | — | Toggle switch |
| `string` | with options | Dropdown (ComboBox) |
| `string` | without options | Text input |
| `number` | with options | Dropdown (ComboBox) |
| `number` | without options | Number input (NumberBox) |

## Accessing Settings in React

Settings are available synchronously via `window.vitrine.settings`:

```jsx
function useSettings(defaults) {
  return { ...defaults, ...(window.vitrine?.settings || {}) };
}

function App() {
  const settings = useSettings({
    showProcessesList: true,
    processCount: 5,
    panelPosition: 'top-right',
    panelOpacity: 0.78,
  });

  return (
    <div style={{ opacity: settings.panelOpacity }}>
      {settings.showProcessesList && <ProcessList count={settings.processCount} />}
    </div>
  );
}
```

Always provide defaults in your theme code — `settings.json` may not exist if the user hasn't configured anything yet.

## Example

See the default theme for a complete example:
- [`src/themes/default/src/settings.json`](../src/themes/default/src/settings.json)
- [`src/themes/default/src/settings.definitions.json`](../src/themes/default/src/settings.definitions.json)
- [`src/themes/default/src/useSettings.js`](../src/themes/default/src/useSettings.js)
