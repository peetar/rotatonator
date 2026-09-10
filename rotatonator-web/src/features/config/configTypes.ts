/**
 * Configuration types matching desktop app RotationConfig.cs and AudioAlertConfig.cs
 */

export interface AudioAlertConfig {
  // On heal cast alerts
  announceHealerNumber: boolean;
  announceHealerName: boolean;
  announceTargetName: boolean;

  // Alert if an NPC is the target
  alertOnNpcCompleteHeal: boolean;

  // On my turn alerts
  announceYoureNext: boolean;
  announceCastNow: boolean;

  // Audio beeps
  enableAudioBeep: boolean;
}

export interface RotationConfig {
  healers: string[];
  playerName: string;
  chainPrefix: string;
  chainIntervalSeconds: number;
  enableVisualAlerts: boolean;
  enableAudioBeep: boolean;
  audioAlerts: AudioAlertConfig;
  enableDDRMode: boolean;
  enableDDRSillyMode: boolean;
}

/**
 * Default configuration matching desktop app defaults
 */
export function createDefaultConfig(): RotationConfig {
  return {
    healers: [],
    playerName: '',
    chainPrefix: 'D&D',
    chainIntervalSeconds: 6,
    enableVisualAlerts: true,
    enableAudioBeep: false,
    audioAlerts: {
      announceHealerNumber: false,
      announceHealerName: false,
      announceTargetName: false,
      alertOnNpcCompleteHeal: true,
      announceYoureNext: false,
      announceCastNow: false,
      enableAudioBeep: false,
    },
    enableDDRMode: false,
    enableDDRSillyMode: false,
  };
}
