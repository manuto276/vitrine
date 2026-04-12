import React from 'react';
import { reloadTheme } from '../hooks/usePanel';

export default function HomePage({ activeTheme, onNavigate }) {
  return (
    <div>
      <h1 className="page-title">Home</h1>

      <div className="card">
        <div className="card-row">
          <div>
            <div className="card-label">Active Theme</div>
            <div className="card-description">{activeTheme || 'None'}</div>
          </div>
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btn btn-sm" onClick={() => reloadTheme()}>
              Reload
            </button>
            <button className="btn btn-sm btn-primary" onClick={() => onNavigate('themes')}>
              Change Theme
            </button>
          </div>
        </div>
      </div>

      <div className="card-group-title">Quick Actions</div>

      <div className="card">
        <div className="card-row">
          <div>
            <div className="card-label">Reload Theme</div>
            <div className="card-description">Re-read theme files from disk and refresh the display</div>
          </div>
          <button className="btn btn-sm" onClick={() => reloadTheme()}>Reload</button>
        </div>
      </div>

      <div className="card">
        <div className="card-row">
          <div>
            <div className="card-label">Theme Settings</div>
            <div className="card-description">Customize the active theme's appearance and behavior</div>
          </div>
          <button className="btn btn-sm" onClick={() => onNavigate('settings')}>Open</button>
        </div>
      </div>
    </div>
  );
}
