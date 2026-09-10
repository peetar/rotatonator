/**
 * Classic overlay window - matches desktop version
 * Shows: Chain info, active heals with progress bars, YOU'RE NEXT warning
 */

import { useEffect, useState } from 'react';
import { type RotationState } from '../rotation/RotationFeedback';
import { type ActiveHeal } from '../rotation/rotationManager';
import './OverlayWindow.css';

const OVERLAY_STATE_KEY = 'rotatonator-overlay-state';
const POLL_INTERVAL = 100; // ms

interface HealDisplayItem {
  heal: ActiveHeal;
  displayName: string;
  timeRemainingMs: number;
  progressPercent: number;
}

export function OverlayWindow() {
  const [state, setState] = useState<RotationState | null>(null);
  const [healDisplays, setHealDisplays] = useState<HealDisplayItem[]>([]);

  // Poll localStorage for state updates from main window
  useEffect(() => {
    const interval = setInterval(() => {
      try {
        const stored = localStorage.getItem(OVERLAY_STATE_KEY);
        if (stored) {
          const newState = JSON.parse(stored) as RotationState;
          
          // Filter out expired heals before setting state
          if (newState.activeHeals && newState.activeHeals.length > 0) {
            const now = new Date();
            const validHeals = newState.activeHeals.filter(heal => {
              const elapsedMs = now.getTime() - new Date(heal.castTime).getTime();
              const timeRemainingMs = heal.durationMs - elapsedMs;
              return timeRemainingMs > 0;
            });
            newState.activeHeals = validHeals;
          }
          
          setState(newState);
        }
      } catch (e) {
        console.error('Error reading overlay state:', e);
      }
    }, POLL_INTERVAL);

    return () => clearInterval(interval);
  }, []);

  // Calculate heal display items whenever state or time changes
  useEffect(() => {
    if (!state || !state.activeHeals || state.activeHeals.length === 0) {
      setHealDisplays([]);
      return;
    }

    const now = new Date();
    const displays: HealDisplayItem[] = [];
    
    for (const heal of state.activeHeals) {
      const elapsedMs = now.getTime() - new Date(heal.castTime).getTime();
      const timeRemainingMs = heal.durationMs - elapsedMs;
      
      if (timeRemainingMs <= 0) {
        continue;
      }
      
      const progressPercent = (elapsedMs / heal.durationMs) * 100;
      const targetStr = heal.targetName ? ` → ${heal.targetName}` : '';
      const displayName = `${heal.healerName}${targetStr}`;

      displays.push({
        heal,
        displayName,
        timeRemainingMs,
        progressPercent: Math.min(100, progressPercent),
      });
    }

    setHealDisplays(displays);
  }, [state]);

  // Update timers frequently
  useEffect(() => {
    const interval = setInterval(() => {
      if (!state || !state.activeHeals || state.activeHeals.length === 0) {
        setHealDisplays([]);
        return;
      }

      const now = new Date();
      const displays: HealDisplayItem[] = [];
      
      for (const heal of state.activeHeals) {
        const elapsedMs = now.getTime() - new Date(heal.castTime).getTime();
        const timeRemainingMs = heal.durationMs - elapsedMs;
        
        if (timeRemainingMs <= 0) {
          continue;
        }
        
        const progressPercent = (elapsedMs / heal.durationMs) * 100;
        const targetStr = heal.targetName ? ` → ${heal.targetName}` : '';
        const displayName = `${heal.healerName}${targetStr}`;

        displays.push({
          heal,
          displayName,
          timeRemainingMs,
          progressPercent: Math.min(100, progressPercent),
        });
      }

      setHealDisplays(displays);
    }, 100);

    return () => clearInterval(interval);
  }, [state]);

  const formatTime = (ms: number): string => {
    return (ms / 1000).toFixed(1) + 's';
  };

  const getHealerColor = (heal: ActiveHeal): string => {
    return heal.isPlayerCast ? '#4CAF50' : '#FFFFFF'; // Green for player, white for others
  };

  const getBarColor = (heal: ActiveHeal): string => {
    return heal.isPlayerCast ? '#4CAF50' : '#2196F3'; // Green for player, blue for others
  };

  const getWarningPulseDurationSeconds = (): number => {
    if (!state) {
      return 1;
    }

    if (state.playerCastNow) {
      return 0.1;
    }

    if (state.playerTurnCountdown === null) {
      return 1;
    }

    const totalCountdownMs = Math.max(100, state.chainIntervalSeconds * 1000);
    const remainingMs = Math.max(0, Math.min(totalCountdownMs, state.playerTurnCountdown));
    const progress = 1 - (remainingMs / totalCountdownMs);

    // Match desktop behavior: pulse interval speeds up from ~1000ms to ~100ms
    return Math.max(0.1, 1 - (progress * 0.9));
  };

  const playerIsInChain = state && state.playerPosition > 0;
  const showYoureNext = !!state && (state.playerCastNow || state.playerTurnCountdown !== null);

  return (
    <div className="overlay-window">
      <div className="overlay-border">
        <div className="overlay-content">
          {/* Chain Info - always show when state is available */}
          {state && (
            <div className="chain-info">
              Chain: {state.healersCount} healers | Your position: {playerIsInChain ? state.playerPosition : 'Not in chain'} | Interval: {state.chainIntervalSeconds}s
            </div>
          )}

          {/* Show empty state when no heals but still show chain info above */}
          {(!state || healDisplays.length === 0) && (
            <div className="overlay-empty">Waiting for heals...</div>
          )}

          {/* Only show these sections when we have active heals */}
          {state && healDisplays.length > 0 && (
            <>
              {/* YOU'RE NEXT Warning */}
              {showYoureNext && (
                <div
                  className="youre-next-warning"
                  style={{ animationDuration: `${getWarningPulseDurationSeconds()}s` }}
                >
                  YOU'RE NEXT!
                  {!!state.playerTurnCountdown && !state.playerCastNow && ` (${formatTime(state.playerTurnCountdown)})`}
                </div>
              )}

              {/* Active Heals List */}
              <div className="heals-list">
                {healDisplays.map((item, idx) => (
                  <div key={idx} className="heal-item">
                    <div className="heal-header">
                      <span
                        className="heal-name"
                        style={{ color: getHealerColor(item.heal) }}
                      >
                        {item.displayName}
                      </span>
                      <span
                        className="heal-timer"
                        style={{ color: getHealerColor(item.heal) }}
                      >
                        {formatTime(item.timeRemainingMs)}
                      </span>
                    </div>
                    <div className="heal-progress-container">
                      <div
                        className="heal-progress-bar"
                        style={{
                          width: `${item.progressPercent}%`,
                          backgroundColor: getBarColor(item.heal),
                        }}
                      />
                    </div>
                  </div>
                ))}
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
