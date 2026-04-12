import React, { useState, useCallback } from 'react';
import { useThemes } from './hooks/usePanel';
import HomePage from './pages/HomePage';
import ThemesPage from './pages/ThemesPage';
import SettingsPage from './pages/SettingsPage';
import AboutPage from './pages/AboutPage';

const NAV_ITEMS = [
  { id: 'home', label: 'Home', icon: '⌂' },
  { id: 'themes', label: 'Themes', icon: '◐' },
  { id: 'settings', label: 'Settings', icon: '⚙' },
  { id: 'about', label: 'About', icon: 'ℹ' },
];

export default function App() {
  const [page, setPage] = useState('home');
  const [settingsTheme, setSettingsTheme] = useState(null);
  const { themes, activeTheme, switchTheme, refresh } = useThemes();

  const navigate = useCallback((target, themeId) => {
    if (target === 'settings' && themeId) {
      setSettingsTheme(themeId);
    } else {
      setSettingsTheme(null);
    }
    setPage(target);
  }, []);

  const handleSwitch = useCallback(async (name) => {
    await switchTheme(name);
    await refresh();
  }, [switchTheme, refresh]);

  return (
    <div className="app">
      <nav className="nav">
        <div className="nav-title">Vitrine</div>
        {NAV_ITEMS.map((item) => (
          <button
            key={item.id}
            className={`nav-item${page === item.id ? ' active' : ''}`}
            onClick={() => navigate(item.id)}
          >
            <span className="nav-icon">{item.icon}</span>
            {item.label}
          </button>
        ))}
      </nav>

      <main className="content">
        {page === 'home' && (
          <HomePage activeTheme={activeTheme} onNavigate={navigate} />
        )}
        {page === 'themes' && (
          <ThemesPage
            themes={themes}
            activeTheme={activeTheme}
            onSwitch={handleSwitch}
            onNavigate={navigate}
          />
        )}
        {page === 'settings' && (
          <SettingsPage themeName={settingsTheme} activeTheme={activeTheme} />
        )}
        {page === 'about' && <AboutPage />}
      </main>
    </div>
  );
}
