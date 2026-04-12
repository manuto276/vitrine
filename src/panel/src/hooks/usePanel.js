import { useState, useEffect, useCallback } from 'react';

const api = window.vitrine?.panel;

export function useThemes() {
  const [themes, setThemes] = useState([]);
  const [activeTheme, setActiveTheme] = useState('');
  const [loading, setLoading] = useState(true);

  const refresh = useCallback(async () => {
    if (!api) return;
    const [list, active] = await Promise.all([
      api.getThemes(),
      api.getActiveTheme(),
    ]);
    setThemes(list);
    setActiveTheme(active.name);
    setLoading(false);
  }, []);

  useEffect(() => { refresh(); }, [refresh]);

  const switchTheme = useCallback(async (name) => {
    await api.setActiveTheme(name);
    setActiveTheme(name);
  }, []);

  return { themes, activeTheme, switchTheme, loading, refresh };
}

export function useThemeSettings(themeName) {
  const [settings, setSettings] = useState(null);
  const [definitions, setDefinitions] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!api || !themeName) return;
    setLoading(true);
    Promise.all([
      api.getSettings(themeName),
      api.getDefinitions(themeName),
    ]).then(([s, d]) => {
      setSettings(s);
      setDefinitions(d);
      setLoading(false);
    });
  }, [themeName]);

  const save = useCallback(async (newSettings) => {
    if (!api || !themeName) return;
    await api.saveSettings(themeName, newSettings);
    setSettings(newSettings);
  }, [themeName]);

  return { settings, definitions, loading, save };
}

export function reloadTheme() {
  return api?.reloadTheme();
}
