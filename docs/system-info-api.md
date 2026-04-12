# System Info API

Vitrine injects a `window.vitrine.system` API into every theme. It provides real-time system information collected via native Win32 APIs.

## Usage

```js
// Subscribe to periodic updates (every 2s)
window.vitrine.system.onUpdate((info) => {
  console.log(info.cpu.usage);
});

// One-shot request (returns a Promise)
const info = await window.vitrine.system.getInfo();
```

Both methods return the same `info` object described below.

## Info Object

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
  "battery": {
    "hasBattery": true,
    "charging": false,
    "level": 72,
    "remainingSeconds": 5400,
    "powerSource": "battery"
  },
  "drives": [
    {
      "name": "C:\\",
      "label": "OS",
      "total": 500107862016,
      "free": 250053931008,
      "used": 250053931008
    }
  ],
  "processes": [
    { "name": "chrome", "pid": 1234, "memory": 524288000 }
  ]
}
```

## Fields

### system

| Field | Type | Description |
|---|---|---|
| `hostname` | string | Machine name |
| `os` | string | OS description (e.g. `Microsoft Windows 10.0.22631`) |
| `uptime` | number | System uptime in seconds |

### cpu

| Field | Type | Description |
|---|---|---|
| `usage` | number | Total CPU usage percentage (0.0–100.0) |
| `cores` | number | Number of logical processors |

Collected via `GetSystemTimes` — compares idle/kernel/user time between samples.

### memory

| Field | Type | Description |
|---|---|---|
| `total` | number | Total physical memory in bytes |
| `available` | number | Available physical memory in bytes |
| `used` | number | Used physical memory in bytes (`total - available`) |
| `load` | number | Memory load percentage (0–100) |

Collected via `GlobalMemoryStatusEx`.

### battery

| Field | Type | Description |
|---|---|---|
| `hasBattery` | boolean | `false` on desktops, `true` on laptops |
| `charging` | boolean | Whether the device is plugged in (only when `hasBattery` is `true`) |
| `level` | number | Battery percentage (0–100), `-1` if unknown |
| `remainingSeconds` | number | Estimated seconds remaining, `-1` if unknown |
| `powerSource` | string | `"ac"` or `"battery"` |

On desktops without a battery, returns `{ "hasBattery": false }` only.

Collected via `GetSystemPowerStatus`.

### drives

Array of fixed drives. Each entry:

| Field | Type | Description |
|---|---|---|
| `name` | string | Drive root (e.g. `C:\`) |
| `label` | string | Volume label |
| `total` | number | Total size in bytes |
| `free` | number | Free space in bytes |
| `used` | number | Used space in bytes (`total - free`) |

Collected via `DriveInfo.GetDrives()`.

### processes

Array of top 5 processes sorted by memory usage:

| Field | Type | Description |
|---|---|---|
| `name` | string | Process name |
| `pid` | number | Process ID |
| `memory` | number | Working set size in bytes |

## React Hook Example

```js
import { useState, useEffect } from 'react';

export function useSystemInfo() {
  const [info, setInfo] = useState(null);

  useEffect(() => {
    if (!window.vitrine) return;
    window.vitrine.system.onUpdate(setInfo);
    window.vitrine.system.getInfo().then(setInfo);
  }, []);

  return info;
}
```

## Helper Functions

Common formatting helpers for themes:

```js
function formatBytes(bytes) {
  if (bytes >= 1073741824) return (bytes / 1073741824).toFixed(1) + ' GiB';
  if (bytes >= 1048576) return (bytes / 1048576).toFixed(0) + ' MiB';
  return (bytes / 1024).toFixed(0) + ' KiB';
}

function formatUptime(seconds) {
  const d = Math.floor(seconds / 86400);
  const h = Math.floor((seconds % 86400) / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  return [d && `${d}d`, h && `${h}h`, `${m}m`].filter(Boolean).join(' ');
}

function pct(used, total) {
  return total > 0 ? Math.round((used / total) * 100) : 0;
}
```

## Update Interval

System info is broadcast every **2 seconds**. The `onUpdate` callback fires each time new data is available. The `getInfo()` promise resolves immediately with the latest collected data.
