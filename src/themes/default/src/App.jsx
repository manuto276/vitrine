import React from 'react';
import { useSystemInfo } from './useSystemInfo';

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
    <div style={styles.barOuter}>
      <div
        style={{
          ...styles.barInner,
          width: `${Math.min(value, 100)}%`,
          background: color,
        }}
      />
    </div>
  );
}

/* ── section header ──────────────────────────────────────────────── */

function Section({ title, children }) {
  return (
    <div style={{ marginBottom: 14 }}>
      <div style={styles.sectionTitle}>
        {title} <span style={styles.rule} />
      </div>
      {children}
    </div>
  );
}

/* ── main ────────────────────────────────────────────────────────── */

export default function App() {
  const info = useSystemInfo();

  if (!info) {
    return (
      <div style={styles.container}>
        <div style={styles.panel}>
          <span style={styles.dim}>Waiting for system data…</span>
        </div>
      </div>
    );
  }

  const { system, cpu, memory, drives, processes } = info;
  const memPct = pct(memory.used, memory.total);

  return (
    <div style={styles.container}>
      <div style={styles.panel}>
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
        <Section title="STORAGE">
          {drives.map((d, i) => {
            const usedPct = pct(d.used, d.total);
            return (
              <div key={i} style={{ marginBottom: 6 }}>
                <Row
                  label={d.name}
                  value={`${formatBytes(d.used)} / ${formatBytes(d.total)}`}
                />
                <Bar value={usedPct} color={usedPct > 90 ? '#ff5555' : '#f1fa8c'} />
              </div>
            );
          })}
        </Section>

        {/* PROCESSES */}
        <Section title="PROCESSES">
          <div style={styles.procHeader}>
            <span style={{ flex: 2 }}>Name</span>
            <span style={{ flex: 1, textAlign: 'right' }}>PID</span>
            <span style={{ flex: 1, textAlign: 'right' }}>Mem</span>
          </div>
          {processes.map((p, i) => (
            <div key={i} style={styles.procRow}>
              <span style={{ flex: 2 }}>{p.name}</span>
              <span style={{ flex: 1, textAlign: 'right', ...styles.dim }}>{p.pid}</span>
              <span style={{ flex: 1, textAlign: 'right' }}>{formatBytes(p.memory)}</span>
            </div>
          ))}
        </Section>
      </div>
    </div>
  );
}

/* ── row helper ──────────────────────────────────────────────────── */

function Row({ label, value }) {
  return (
    <div style={styles.row}>
      <span style={styles.dim}>{label}</span>
      <span>{value}</span>
    </div>
  );
}

/* ── styles (Conky-inspired) ─────────────────────────────────────── */

const styles = {
  container: {
    position: 'fixed',
    top: 24,
    right: 24,
    bottom: 24,
    display: 'flex',
    alignItems: 'flex-start',
    justifyContent: 'flex-end',
    pointerEvents: 'none',
    fontFamily: "'Cascadia Code', 'Consolas', 'Courier New', monospace",
    fontSize: 12,
    color: '#f8f8f2',
    userSelect: 'none',
  },
  panel: {
    background: 'rgba(40, 42, 54, 0.78)',
    backdropFilter: 'blur(8px)',
    WebkitBackdropFilter: 'blur(8px)',
    border: '1px solid rgba(255,255,255,0.06)',
    borderRadius: 10,
    padding: '18px 22px',
    minWidth: 280,
    maxWidth: 320,
  },
  sectionTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: 8,
    fontSize: 11,
    fontWeight: 700,
    letterSpacing: '0.12em',
    color: '#bd93f9',
    marginBottom: 6,
  },
  rule: {
    flex: 1,
    height: 1,
    background: 'rgba(189,147,249,0.25)',
  },
  row: {
    display: 'flex',
    justifyContent: 'space-between',
    lineHeight: '20px',
  },
  dim: {
    opacity: 0.5,
  },
  barOuter: {
    height: 4,
    borderRadius: 2,
    background: 'rgba(255,255,255,0.08)',
    margin: '3px 0 4px',
    overflow: 'hidden',
  },
  barInner: {
    height: '100%',
    borderRadius: 2,
    transition: 'width 0.4s ease',
  },
  procHeader: {
    display: 'flex',
    fontSize: 10,
    opacity: 0.4,
    marginBottom: 2,
    letterSpacing: '0.06em',
    textTransform: 'uppercase',
  },
  procRow: {
    display: 'flex',
    lineHeight: '18px',
  },
};
