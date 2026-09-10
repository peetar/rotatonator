import { type RotationConfig, type AudioAlertConfig } from './configTypes';
import './ConfigPanel.css';

interface AudioConfigPanelProps {
  config: RotationConfig;
  onConfigChange: (config: RotationConfig) => void;
}

export function AudioConfigPanel({ config, onConfigChange }: AudioConfigPanelProps) {
  const updateAudioAlert = <K extends keyof AudioAlertConfig>(
    key: K,
    value: AudioAlertConfig[K],
  ) => {
    onConfigChange({
      ...config,
      audioAlerts: {
        ...config.audioAlerts,
        [key]: value,
      },
    });
  };

  return (
    <div className="config-panel">
      <div className="audio-alerts">
        <div className="audio-alert-group">
          <h4>On Heal Cast</h4>
          <label>
            <input
              type="checkbox"
              checked={config.audioAlerts.announceHealerNumber}
              onChange={(e) => updateAudioAlert('announceHealerNumber', e.target.checked)}
            />
            Announce Healer Number
          </label>
          <label>
            <input
              type="checkbox"
              checked={config.audioAlerts.announceHealerName}
              onChange={(e) => updateAudioAlert('announceHealerName', e.target.checked)}
            />
            Announce Healer Name
          </label>
          <label>
            <input
              type="checkbox"
              checked={config.audioAlerts.announceTargetName}
              onChange={(e) => updateAudioAlert('announceTargetName', e.target.checked)}
            />
            Announce Target Name
          </label>
        </div>

        <div className="audio-alert-group">
          <h4>On Your Turn</h4>
          <label>
            <input
              type="checkbox"
              checked={config.audioAlerts.announceYoureNext}
              onChange={(e) => updateAudioAlert('announceYoureNext', e.target.checked)}
            />
            Announce "You're Next"
          </label>
          <label>
            <input
              type="checkbox"
              checked={config.audioAlerts.announceCastNow}
              onChange={(e) => updateAudioAlert('announceCastNow', e.target.checked)}
            />
            Announce "Cast Now"
          </label>
        </div>

        <div className="audio-alert-group">
          <h4>Special Alerts</h4>
          <label>
            <input
              type="checkbox"
              checked={config.audioAlerts.alertOnNpcCompleteHeal}
              onChange={(e) => updateAudioAlert('alertOnNpcCompleteHeal', e.target.checked)}
            />
            Alert on NPC Target (Complete Heal)
          </label>
          <label>
            <input
              type="checkbox"
              checked={config.audioAlerts.enableAudioBeep}
              onChange={(e) => updateAudioAlert('enableAudioBeep', e.target.checked)}
            />
            Enable Audio Beep
          </label>
        </div>
      </div>
    </div>
  );
}
