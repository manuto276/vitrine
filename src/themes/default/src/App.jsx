import React from 'react';
import { useSystemInfo } from './useSystemInfo';
import { useSettings } from './useSettings';

/* ── helpers ─────────────────────────────────────────────────────── */

function formatBytes(b) {
  if (b >= 1073741824) return (b / 1073741824).toFixed(1) + ' GiB';
  if (b >= 1048576) return (b / 1048576).toFixed(0) + ' MiB';
  return (b / 1024).toFixed(0) + ' KiB';
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

/* ── bar component ───────────────────────────────────────────────── */

function Bar({ value, color = '#8be9fd' }) {
  return (
    <div className="vitrine-bar">
      <div
        className="vitrine-bar-fill"
        style={{ width: `${Math.min(value, 100)}%`, background: color }}
      />
    </div>
  );
}

/* ── section header ──────────────────────────────────────────────── */

function Section({ title, children }) {
  return (
    <div className="vitrine-section">
      <div className="vitrine-section-title">
        {title} <span className="vitrine-section-rule" />
      </div>
      {children}
    </div>
  );
}

/* ── row helper ──────────────────────────────────────────────────── */

function Row({ label, value }) {
  return (
    <div className="vitrine-row">
      <span className="vitrine-dim">{label}</span>
      <span>{value}</span>
    </div>
  );
}

/* ── main ────────────────────────────────────────────────────────── */

export default function App() {
  const info = useSystemInfo();
  const settings = useSettings();

  const positionStyle = {
    'top-right': { top: 24, right: 24 },
    'top-left': { top: 24, left: 24 },
    'bottom-right': { bottom: 24, right: 24 },
    'bottom-left': { bottom: 24, left: 24 },
  }[settings.panelPosition] || { top: 24, right: 24 };

  const panelStyle = {
    background: `rgba(40, 42, 54, ${settings.panelOpacity})`,
  };

  if (!info) {
    return (
      <div className="vitrine-container" style={positionStyle}>
        <div className="vitrine-panel" style={panelStyle}>
          <span className="vitrine-dim">Waiting for system data…</span>
        </div>
      </div>
    );
  }

  const { system, cpu, memory, drives, processes } = info;
  const memPct = pct(memory.used, memory.total);

  return (
    <div className="vitrine-container" style={positionStyle}>
      <div className="vitrine-panel" style={panelStyle}>
        {/* SYSTEM */}
        <Section title="SYSTEM">
          <Row label="Hostname" value={system.hostname} />
          <Row label="OS" value={system.os} />
          <Row label="Uptime" value={formatUptime(system.uptime)} />
        </Section>

        {/* CPU */}
        <Section title="CPU">
          <Row label="Usage" value={`${cpu.usage}%`} />
          <Bar value={cpu.usage} color={cpu.usage > 80 ? '#ff5555' : '#8be9fd'} />
          <Row label="Cores" value={cpu.cores} />
        </Section>

        {/* MEMORY */}
        <Section title="MEMORY">
          <Row
            label="RAM"
            value={`${formatBytes(memory.used)} / ${formatBytes(memory.total)}`}
          />
          <Bar value={memPct} color={memPct > 80 ? '#ff5555' : '#50fa7b'} />
        </Section>

        {/* STORAGE */}
        {settings.showStorageSection && (
          <Section title="STORAGE">
            {drives.map((d, i) => {
              const usedPct = pct(d.used, d.total);
              return (
                <div key={i} className="vitrine-mb-6">
                  <Row
                    label={d.name}
                    value={`${formatBytes(d.used)} / ${formatBytes(d.total)}`}
                  />
                  <Bar value={usedPct} color={usedPct > 90 ? '#ff5555' : '#f1fa8c'} />
                </div>
              );
            })}
          </Section>
        )}

        {/* PROCESSES */}
        {settings.showProcessesList && (
          <Section title="PROCESSES">
            <div className="vitrine-proc-header">
              <span className="vitrine-col-name">Name</span>
              <span className="vitrine-col-right">PID</span>
              <span className="vitrine-col-right">Mem</span>
            </div>
            {processes.slice(0, settings.processCount).map((p, i) => (
              <div key={i} className="vitrine-proc-row">
                <span className="vitrine-col-name">{p.name}</span>
                <span className="vitrine-col-right vitrine-dim">{p.pid}</span>
                <span className="vitrine-col-right">{formatBytes(p.memory)}</span>
              </div>
            ))}
          </Section>
        )}
      </div>
    </div>
  );
}
