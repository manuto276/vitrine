import React, { useState, useEffect, useCallback } from 'react';
import { useThemeSettings, reloadTheme } from '../hooks/usePanel';

function Toggle({ checked, onChange }) {
  return (
    <label className="toggle">
      <input type="checkbox" checked={checked} onChange={(e) => onChange(e.target.checked)} />
      <span className="toggle-track" />
      <span className="toggle-thumb" />
    </label>
  );
}

function SettingControl({ definition, value, onChange }) {
  const { type, options } = definition;

  if (type === 'boolean') {
    return <Toggle checked={!!value} onChange={onChange} />;
  }

  if (options && options.length > 0) {
    return (
      <select className="select" value={value ?? ''} onChange={(e) => {
        const v = type === 'number' ? Number(e.target.value) : e.target.value;
        onChange(v);
      }}>
        {options.map((opt) => (
          <option key={String(opt.value)} value={opt.value}>{opt.label}</option>
        ))}
      </select>
    );
  }

  if (type === 'number') {
    return (
      <input
        className="input-number"
        type="number"
        value={value ?? ''}
        step={definition.default != null && definition.default % 1 !== 0 ? '0.01' : '1'}
        onChange={(e) => onChange(Number(e.target.value))}
      />
    );
  }

  return (
    <input
      className="select"
      type="text"
      value={value ?? ''}
      onChange={(e) => onChange(e.target.value)}
    />
  );
}

export default function SettingsPage({ themeName, activeTheme }) {
  const target = themeName || activeTheme;
  const { settings, definitions, loading, save } = useThemeSettings(target);
  const [local, setLocal] = useState(null);
  const [dirty, setDirty] = useState(false);

  useEffect(() => {
    if (settings) {
      setLocal({ ...settings });
      setDirty(false);
    }
  }, [settings]);

  const handleChange = useCallback((key, value) => {
    setLocal((prev) => ({ ...prev, [key]: value }));
    setDirty(true);
  }, []);

  const handleSave = useCallback(async () => {
    if (!local) return;
    await save(local);
    setDirty(false);
    await reloadTheme();
  }, [local, save]);

  const handleReset = useCallback(() => {
    if (!definitions) return;
    const defaults = {};
    for (const [key, def] of Object.entries(definitions)) {
      if (def.default !== undefined) defaults[key] = def.default;
    }
    setLocal(defaults);
    setDirty(true);
  }, [definitions]);

  if (loading || !local || !definitions) {
    return (
      <div>
        <h1 className="page-title">Theme Settings</h1>
        <div className="card">
          <div className="card-row">
            <span className="card-label" style={{ color: 'var(--text-secondary)' }}>
              {loading ? 'Loading…' : 'This theme has no configurable settings.'}
            </span>
          </div>
        </div>
      </div>
    );
  }

  const entries = Object.entries(definitions);

  if (entries.length === 0) {
    return (
      <div>
        <h1 className="page-title">Theme Settings</h1>
        <div className="card">
          <div className="card-row">
            <span className="card-label" style={{ color: 'var(--text-secondary)' }}>
              This theme has no configurable settings.
            </span>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div>
      <h1 className="page-title">Theme Settings</h1>
      <p className="page-subtitle">Configure "{target}" theme</p>

      {entries.map(([key, def]) => (
        <div key={key} className="card">
          <div className="card-row">
            <div>
              <div className="card-label">{def.label || key}</div>
              {def.description && (
                <div className="card-description">{def.description}</div>
              )}
            </div>
            <SettingControl
              definition={def}
              value={local[key]}
              onChange={(v) => handleChange(key, v)}
            />
          </div>
        </div>
      ))}

      <div style={{ marginTop: 20, display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
        <button className="btn btn-sm" onClick={handleReset}>Reset Defaults</button>
        <button className="btn btn-sm btn-primary" onClick={handleSave} disabled={!dirty}>
          {dirty ? 'Save & Apply' : 'Saved'}
        </button>
      </div>
    </div>
  );
}
