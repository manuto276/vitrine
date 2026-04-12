import React from 'react';

export default function ThemesPage({ themes, activeTheme, onSwitch, onNavigate }) {
  return (
    <div>
      <h1 className="page-title">Themes</h1>
      <p className="page-subtitle">
        Select a theme to apply. Themes are loaded from %APPDATA%\Vitrine\themes\.
      </p>

      <div className="theme-grid">
        {themes.map((t) => {
          const isActive = t.id === activeTheme;
          return (
            <div key={t.id} className={`theme-card${isActive ? ' active' : ''}`}>
              <div className="theme-info">
                <div className="theme-name">
                  {t.name}
                  {isActive && <span className="theme-badge">Active</span>}
                </div>
                {t.description && (
                  <div className="theme-description">{t.description}</div>
                )}
              </div>
              <div className="theme-actions">
                {t.hasSettings && (
                  <button
                    className="btn btn-sm"
                    onClick={() => onNavigate('settings', t.id)}
                  >
                    Settings
                  </button>
                )}
                {!isActive && (
                  <button
                    className="btn btn-sm btn-primary"
                    onClick={() => onSwitch(t.id)}
                  >
                    Apply
                  </button>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
