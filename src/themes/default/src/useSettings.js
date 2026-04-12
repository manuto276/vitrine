const defaults = {
  showProcessesList: true,
  showStorageSection: true,
  processCount: 5,
  panelPosition: 'top-right',
  panelOpacity: 0.78,
};

export function useSettings() {
  const loaded = (window.vitrine && window.vitrine.settings) || {};
  return { ...defaults, ...loaded };
}
