import { type RotationConfig } from '../config/configTypes';
import { type HealCastEventArgs } from '../rotation/rotationManager';

export class AudioAlertService {
  private audioContext: AudioContext | null = null;

  onHealCast(args: HealCastEventArgs, config: RotationConfig) {
    const alerts = config.audioAlerts;

    if (alerts.enableAudioBeep || config.enableAudioBeep) {
      this.playBeep(600, 50, 0.25);
    }

    // NPC detection: if targetName contains a space, treat as NPC
    const isNpcTarget = args.targetName && args.targetName.includes(' ');
    if (isNpcTarget && alerts.alertOnNpcCompleteHeal) {
      // Announce "Bad target" to warn the user of an NPC target
      this.speak('Bad target');
      return;
    }

    const parts: string[] = [];

    if (alerts.announceHealerNumber && args.healerPosition > 0) {
      parts.push(`${args.healerPosition}`);
    }

    if (alerts.announceHealerName && args.healerName) {
      parts.push(args.healerName);
    }

    if (alerts.announceTargetName && args.targetName) {
      parts.push(args.targetName);
    }

    if (parts.length > 0) {
      this.speak(parts.join('. '));
    }
  }

  onPlayerTurnStarting(config: RotationConfig) {
    if (config.audioAlerts.announceYoureNext) {
      this.speak('next');
    }
  }

  onPlayerTurnNow(config: RotationConfig) {
    if (config.audioAlerts.announceCastNow) {
      this.speak('Go');
    }
  }

  cleanup() {
    if (typeof window !== 'undefined' && 'speechSynthesis' in window) {
      window.speechSynthesis.cancel();
    }

    if (this.audioContext) {
      this.audioContext.close().catch(() => {
        // Ignore cleanup errors
      });
      this.audioContext = null;
    }
  }

  private speak(text: string) {
    if (typeof window === 'undefined' || !('speechSynthesis' in window)) {
      return;
    }

    const utterance = new SpeechSynthesisUtterance(text);
    utterance.rate = 1;
    utterance.pitch = 1;
    utterance.volume = 1;

    window.speechSynthesis.speak(utterance);
  }

  private playBeep(frequency: number, durationMs: number, volume: number) {
    if (typeof window === 'undefined' || !('AudioContext' in window || 'webkitAudioContext' in window)) {
      return;
    }

    try {
      if (!this.audioContext) {
        const AudioContextCtor = window.AudioContext || (window as typeof window & { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
        if (!AudioContextCtor) {
          return;
        }
        this.audioContext = new AudioContextCtor();
      }

      if (this.audioContext.state === 'suspended') {
        this.audioContext.resume().catch(() => {
          // Browser may block until user gesture
        });
      }

      const oscillator = this.audioContext.createOscillator();
      const gainNode = this.audioContext.createGain();

      oscillator.type = 'sine';
      oscillator.frequency.value = frequency;
      gainNode.gain.value = Math.max(0, Math.min(1, volume));

      oscillator.connect(gainNode);
      gainNode.connect(this.audioContext.destination);

      const now = this.audioContext.currentTime;
      oscillator.start(now);
      oscillator.stop(now + durationMs / 1000);
    } catch {
      // Ignore audio errors (permissions/autoplay restrictions)
    }
  }
}
