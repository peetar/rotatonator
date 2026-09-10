import { type RotationConfig } from '../config/configTypes';

/**
 * Active heal being tracked on the overlay
 */
export interface ActiveHeal {
  healerName: string;
  targetName: string;
  castTime: Date;
  durationMs: number;
  isPlayerCast: boolean;
}

/**
 * Event args for when a heal is detected
 */
export interface HealCastEventArgs {
  healerName: string;
  targetName: string;
  castTime: Date;
  isPlayerCast: boolean;
  healerPosition: number; // 1-based position in chain
  expectedCastTime: Date | undefined; // When this heal should have happened
  skipDDRScoring: boolean;
}

/**
 * Event args for when player's turn is approaching
 */
export interface PlayerTurnEventArgs {
  timeUntilCast: number; // milliseconds
}

/**
 * Event args for chain import
 */
export interface ChainImportEventArgs {
  healers: string[];
  delay: number; // seconds
}

/**
 * Manages rotation state, timing, and healing order
 * Ported from desktop RotationManager.cs
 */
export class RotationManager {
  private config: RotationConfig;
  private lastCastTime: Date | null = null;
  private lastCaster: string | null = null;
  private nextExpectedCastTime: Date | null = null;
  private playerTurnTimer: ReturnType<typeof setTimeout> | null = null;
  private activeHeals: ActiveHeal[] = [];
  private cleanupInterval: ReturnType<typeof setInterval> | null = null;

  // Event callbacks
  public onHealCastDetected: ((args: HealCastEventArgs) => void) | null = null;
  public onPlayerTurnStarting: ((args: PlayerTurnEventArgs) => void) | null = null;
  public onPlayerTurnNow: (() => void) | null = null;
  public onChainImported: ((args: ChainImportEventArgs) => void) | null = null;

  constructor(config: RotationConfig) {
    this.config = config;
    // Start cleanup interval to remove expired heals
    this.cleanupInterval = setInterval(() => this.cleanupExpiredHeals(), 100);
  }

  /**
   * Update config (for when user changes settings)
   * Note: When healers change, the lastCaster position may shift
   */
  updateConfig(config: RotationConfig) {
    const oldHealers = this.config.healers;
    this.config = config;
    
    // If healers array changed (order or members), verify lastCaster is still valid
    if (oldHealers.length !== config.healers.length ||
        oldHealers.some((h, i) => h !== config.healers[i])) {
      // Healers changed - lastCaster position may have shifted
      // Keep the lastCaster name but position will be recalculated on next cast
    }
  }

  /**
   * Get the 1-based position of the current player in the chain
   * Returns -1 if player is not in the chain
   */
  getPlayerPosition(): number {
    if (!this.config.playerName) return -1;

    const index = this.config.healers.findIndex(
      h => h.toLowerCase() === this.config.playerName.toLowerCase()
    );
    return index >= 0 ? index + 1 : -1;
  }

  /**
   * Get the next healer in the rotation after the current healer
   */
  getNextHealer(): string | null {
    if (this.config.healers.length === 0) return null;
    if (!this.lastCaster) return this.config.healers[0];

    const currentIndex = this.config.healers.findIndex(
      h => h.toLowerCase() === this.lastCaster!.toLowerCase()
    );

    if (currentIndex < 0) return this.config.healers[0];

    const nextIndex = (currentIndex + 1) % this.config.healers.length;
    return this.config.healers[nextIndex];
  }

  /**
   * Get the current last caster
   */
  getLastCaster(): string | null {
    return this.lastCaster;
  }

  /**
   * Get the last cast time
   */
  getLastCastTime(): Date | null {
    return this.lastCastTime;
  }

  /**
   * Get time remaining until next expected cast (in milliseconds)
   */
  getTimeUntilNextCast(): number | null {
    if (!this.lastCastTime) return null;

    const now = new Date();
    const nextTime = new Date(
      this.lastCastTime.getTime() + this.config.chainIntervalSeconds * 1000
    );
    const remaining = nextTime.getTime() - now.getTime();

    return remaining > 0 ? remaining : 0;
  }

  /**
   * Handle a heal cast detection
   */
  onHealCast(healerName: string, targetName: string = '', adjustedCastTime?: Date) {
    const castTime = adjustedCastTime || new Date();
    const isPlayerCast =
      !!this.config.playerName &&
      healerName.toLowerCase() === this.config.playerName.toLowerCase();

    // Find healer position in chain (1-based)
    const healerIndex = this.config.healers.findIndex(
      h => h.toLowerCase() === healerName.toLowerCase()
    );
    const healerPosition = healerIndex >= 0 ? healerIndex + 1 : -1;

    // Track expected cast time
    const expectedCastTime = this.nextExpectedCastTime ?? undefined;

    // Check if this heal should be skipped for DDR scoring
    let skipDDRScoring = false;
    if (this.lastCastTime) {
      const timeSinceLastHeal =
        (castTime.getTime() - this.lastCastTime.getTime()) / 1000;
      if (timeSinceLastHeal < 1.0 || timeSinceLastHeal > 10.0) {
        skipDDRScoring = true;
      }
    }

    // Update next expected cast time
    this.nextExpectedCastTime = new Date(
      castTime.getTime() + this.config.chainIntervalSeconds * 1000
    );

    // Track this heal as active ONLY if it's recent (within last 30 seconds)
    // This prevents old heals from log file history from showing on overlay
    const now = new Date();
    const ageMs = now.getTime() - castTime.getTime();
    const maxAgeMs = 30000; // 30 seconds
    
    if (ageMs < maxAgeMs && ageMs >= 0) {
      // Remove any existing heal from this healer (they cast a new one)
      this.activeHeals = this.activeHeals.filter(h => h.healerName !== healerName);
      
      // Complete Heal cast time is always ~10 seconds, NOT the chain interval
      const durationMs = 10000; // 10 seconds
      this.activeHeals.push({
        healerName,
        targetName,
        castTime,
        durationMs,
        isPlayerCast,
      });
    }

    // Raise event for UI update
    this.onHealCastDetected?.({
      healerName,
      targetName,
      castTime,
      isPlayerCast,
      healerPosition,
      expectedCastTime,
      skipDDRScoring,
    });

    this.lastCastTime = castTime;
    this.lastCaster = healerName;

    // Calculate when player should cast next
    if (this.config.playerName) {
      this.calculatePlayerTurn(healerName);
    }
  }

  /**
   * Handle chain import (set list of healers and interval)
   */
  onChainImport(chainData: string, delay: number) {
    try {
      // Parse chain data: "111 Name1, 222 Name2, 333 Name3, AAA Name10"
      const healers: string[] = [];
      const parts = chainData.split(',');

      for (const part of parts) {
        const trimmed = part.trim();
        // Match pattern like "111 Healer1" or "AAA Healer10"
        const match = trimmed.match(/^[\d\w]+\s+(.+)$/);
        if (match) {
          healers.push(match[1].trim());
        }
      }

      if (healers.length > 0) {
        // Update config
        this.config.healers = healers;
        this.config.chainIntervalSeconds = delay;

        // Notify that chain was imported
        this.onChainImported?.({
          healers,
          delay,
        });
      }
    } catch (e) {
      console.error('Error parsing chain import:', e);
    }
  }

  /**
   * Handle delay-only import (update interval, keep healers)
   */
  onDelayOnlyImport(delay: number) {
    this.config.chainIntervalSeconds = delay;

    this.onChainImported?.({
      healers: this.config.healers,
      delay,
    });
  }

  /**
   * Calculate if and when player should cast next
   */
  private calculatePlayerTurn(currentCaster: string) {
    const currentIndex = this.config.healers.findIndex(
      h => h.toLowerCase() === currentCaster.toLowerCase()
    );

    if (currentIndex < 0) return;

    const playerIndex = this.getPlayerPosition();
    if (playerIndex < 0) return;

    // Check if player is next in rotation
    const nextIndex = (currentIndex + 1) % this.config.healers.length;
    const playerIsNext = nextIndex === playerIndex - 1; // -1 because position is 1-based

    if (playerIsNext) {
      const timeUntilCastMs = this.config.chainIntervalSeconds * 1000;

      // Notify that player's turn is coming
      this.onPlayerTurnStarting?.({
        timeUntilCast: timeUntilCastMs,
      });

      // Clear any existing timer
      if (this.playerTurnTimer) {
        clearTimeout(this.playerTurnTimer);
      }

      // Set timer for when player should cast NOW
      this.playerTurnTimer = setTimeout(() => {
        this.playerTurnTimer = null;
        this.onPlayerTurnNow?.();
      }, timeUntilCastMs);
    }
  }

  /**
   * Get currently active heals (in-flight casts)
   */
  getActiveHeals(): ActiveHeal[] {
    return this.activeHeals;
  }

  /**
   * Remove expired heals that have passed their duration
   */
  private cleanupExpiredHeals() {
    const now = new Date();
    this.activeHeals = this.activeHeals.filter(heal => {
      const expireTime = new Date(heal.castTime.getTime() + heal.durationMs);
      return now < expireTime;
    });
  }

  /**
   * Clear any active timers (for cleanup)
   */
  cleanup() {
    if (this.playerTurnTimer) {
      clearTimeout(this.playerTurnTimer);
      this.playerTurnTimer = null;
    }
    if (this.cleanupInterval) {
      clearInterval(this.cleanupInterval);
      this.cleanupInterval = null;
    }
  }
}
