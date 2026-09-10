import { useEffect, useRef, useState } from 'react';
import { type RotationManager, type HealCastEventArgs, type ActiveHeal } from '../rotation/rotationManager';
import { type RotationConfig } from '../config/configTypes';
import { AudioAlertService } from '../audio/AudioAlertService';
import './RotationFeedback.css';

interface RotationFeedbackProps {
  rotationManager: RotationManager;
  config: RotationConfig;
}

export interface RotationState {
  currentHealer: string | null;
  nextHealer: string | null;
  timeUntilNextCast: number | null; // milliseconds
  playerPosition: number; // 1-based, or -1 if not in chain
  playerTurnCountdown: number | null; // milliseconds until player should cast
  playerCastNow: boolean; // true when player should cast RIGHT NOW
  lastCastEvents: HealCastEventArgs[]; // last N cast events
  activeHeals: ActiveHeal[]; // currently in-flight heals
  chainIntervalSeconds: number; // for calculating progress bar duration
  healersCount: number; // for displaying chain info
}

const KEEP_LAST_N_EVENTS = 10;

export function RotationFeedback({ rotationManager, config }: RotationFeedbackProps) {
  const audioServiceRef = useRef<AudioAlertService>(new AudioAlertService());
  const [state, setState] = useState<RotationState>({
    currentHealer: rotationManager.getLastCaster(),
    nextHealer: rotationManager.getNextHealer(),
    timeUntilNextCast: rotationManager.getTimeUntilNextCast(),
    playerPosition: rotationManager.getPlayerPosition(),
    playerTurnCountdown: null,
    playerCastNow: false,
    lastCastEvents: [],
    activeHeals: rotationManager.getActiveHeals(),
    chainIntervalSeconds: config.chainIntervalSeconds,
    healersCount: config.healers.length,
  });

  const [timerKey, setTimerKey] = useState(0);

  // Update player position when rotation manager config changes
  useEffect(() => {
    setState(prev => ({
      ...prev,
      playerPosition: rotationManager.getPlayerPosition(),
      currentHealer: rotationManager.getLastCaster(),
      nextHealer: rotationManager.getNextHealer(),
      timeUntilNextCast: rotationManager.getTimeUntilNextCast(),
      activeHeals: rotationManager.getActiveHeals(),
      chainIntervalSeconds: config.chainIntervalSeconds,
      healersCount: config.healers.length,
    }));
  }, [rotationManager, config]);

  // Set up event listeners
  useEffect(() => {
    const audioService = audioServiceRef.current;

    const handleHealCast = (args: HealCastEventArgs) => {
      audioService.onHealCast(args, config);
      setState(prev => {
        const newEvents = [args, ...prev.lastCastEvents].slice(0, KEEP_LAST_N_EVENTS);
        return {
          ...prev,
          currentHealer: args.healerName,
          nextHealer: rotationManager.getNextHealer(),
          lastCastEvents: newEvents,
          activeHeals: rotationManager.getActiveHeals(),
          playerCastNow: false, // Reset the "cast now" alert
        };
      });
      setTimerKey(k => k + 1); // Force timer re-render
    };

    const handlePlayerTurnStarting = (args: { timeUntilCast: number }) => {
      audioService.onPlayerTurnStarting(config);
      setState(prev => ({
        ...prev,
        playerTurnCountdown: args.timeUntilCast,
      }));
      setTimerKey(k => k + 1);
    };

    const handlePlayerTurnNow = () => {
      audioService.onPlayerTurnNow(config);
      setState(prev => ({
        ...prev,
        playerCastNow: true,
        playerTurnCountdown: null,
      }));
    };

    rotationManager.onHealCastDetected = handleHealCast;
    rotationManager.onPlayerTurnStarting = handlePlayerTurnStarting;
    rotationManager.onPlayerTurnNow = handlePlayerTurnNow;

    // Cleanup on unmount
    return () => {
      audioService.cleanup();
      rotationManager.cleanup();
    };
  }, [rotationManager, config]);

  // Sync rotation state to localStorage for overlay window
  useEffect(() => {
    try {
      localStorage.setItem('rotatonator-overlay-state', JSON.stringify(state));
    } catch (e) {
      // Silently fail if localStorage is unavailable
    }
  }, [state]);

  // Update state when config changes to ensure overlay gets latest values
  useEffect(() => {
    setState(prev => ({
      ...prev,
      chainIntervalSeconds: config.chainIntervalSeconds,
      healersCount: config.healers.length,
    }));
  }, [config.chainIntervalSeconds, config.healers.length]);

  // Timer to update time remaining until next cast and active heals
  useEffect(() => {
    const interval = setInterval(() => {
      setState(prev => {
        const timeRemaining = rotationManager.getTimeUntilNextCast();
        return {
          ...prev,
          timeUntilNextCast: timeRemaining,
          activeHeals: rotationManager.getActiveHeals(),
        };
      });
    }, 100);

    return () => clearInterval(interval);
  }, [rotationManager]);

  // Timer to update player turn countdown
  useEffect(() => {
    if (state.playerTurnCountdown === null) return;

    const interval = setInterval(() => {
      setState(prev => {
        if (!prev.playerTurnCountdown || prev.playerTurnCountdown <= 100) {
          return prev;
        }
        return {
          ...prev,
          playerTurnCountdown: prev.playerTurnCountdown - 100,
        };
      });
    }, 100);

    return () => clearInterval(interval);
  }, [state.playerTurnCountdown, timerKey]);

  const formatTime = (ms: number | null): string => {
    if (ms === null) return '—';
    return (ms / 1000).toFixed(1) + 's';
  };

  const playerIsInChain = state.playerPosition > 0;

  return (
    <div className="rotation-feedback panel">
      <h2>Rotation Status</h2>

      {state.currentHealer ? (
        <>
          <div className="rotation-row">
            <div className="rotation-item current-healer">
              <div className="rotation-label">Currently Healing</div>
              <div className="rotation-value">{state.currentHealer}</div>
              {state.lastCastEvents[0]?.targetName && (
                <div className="rotation-target">→ {state.lastCastEvents[0].targetName}</div>
              )}
            </div>

            <div className="rotation-item next-healer">
              <div className="rotation-label">Next in Rotation</div>
              <div className="rotation-value">{state.nextHealer || '—'}</div>
              <div className="rotation-timer">{formatTime(state.timeUntilNextCast)}</div>
            </div>
          </div>

          {playerIsInChain && (
            <div className="rotation-row player-status">
              <div className={`rotation-item player ${state.playerCastNow ? 'cast-now' : ''}`}>
                <div className="rotation-label">Your Position</div>
                <div className="rotation-value">#{state.playerPosition}</div>
                {state.playerTurnCountdown !== null && !state.playerCastNow && (
                  <div className="rotation-timer your-turn">
                    Your turn in {formatTime(state.playerTurnCountdown)}
                  </div>
                )}
                {state.playerCastNow && (
                  <div className="rotation-timer cast-now">🎯 CAST NOW! 🎯</div>
                )}
              </div>
            </div>
          )}

          {state.lastCastEvents.length > 0 && (
            <div className="recent-casts">
              <h3>Recent Heals</h3>
              <div className="cast-list">
                {state.lastCastEvents.slice(0, 3).map((evt, idx) => (
                  <div key={idx} className="cast-item">
                    <span className="cast-healer">{evt.healerName}</span>
                    <span className="cast-position">#{evt.healerPosition}</span>
                    {evt.targetName && <span className="cast-target">→ {evt.targetName}</span>}
                    <span className="cast-time">{evt.castTime.toLocaleTimeString()}</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </>
      ) : (
        <div className="no-data">No heals detected yet. Waiting for log file input...</div>
      )}
    </div>
  );
}
