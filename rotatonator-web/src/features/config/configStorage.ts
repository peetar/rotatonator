import { type RotationConfig, createDefaultConfig } from './configTypes';

const CONFIG_STORAGE_KEY = 'rotatonator-config';

/**
 * Load configuration from localStorage, or return default if not found
 */
export function loadConfig(): RotationConfig {
  try {
    const stored = localStorage.getItem(CONFIG_STORAGE_KEY);
    if (stored) {
      // Merge with defaults in case new fields were added
      const loaded = JSON.parse(stored);
      return {
        ...createDefaultConfig(),
        ...loaded,
        audioAlerts: {
          ...createDefaultConfig().audioAlerts,
          ...(loaded.audioAlerts || {}),
        },
      };
    }
  } catch (e) {
    console.error('Failed to load config from localStorage', e);
  }

  return createDefaultConfig();
}

/**
 * Save configuration to localStorage
 */
export function saveConfig(config: RotationConfig): void {
  try {
    localStorage.setItem(CONFIG_STORAGE_KEY, JSON.stringify(config));
  } catch (e) {
    console.error('Failed to save config to localStorage', e);
  }
}

/**
 * Clear configuration from localStorage (reset to defaults)
 */
export function clearConfig(): void {
  try {
    localStorage.removeItem(CONFIG_STORAGE_KEY);
  } catch (e) {
    console.error('Failed to clear config from localStorage', e);
  }
}
