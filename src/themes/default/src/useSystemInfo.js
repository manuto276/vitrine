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
