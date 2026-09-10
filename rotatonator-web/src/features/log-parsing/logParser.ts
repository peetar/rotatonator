import { PositionHelper } from './positionHelper';

/**
 * Represents a Rotatonator config command detected in chat
 */
export interface RotatonatorConfigCommand {
  healers?: string[];
  chainIntervalSeconds: number;
}

/**
 * Represents a single parsed log entry
 */
export interface LogEntry {
  timestamp: Date;
  rawLine: string;
  detectedAsChMessage: boolean;
  chData?: {
    positionStr: string;
    position: number;
    healer?: string;
    targetName?: string;
  };
  configCommand?: RotatonatorConfigCommand;
}

/**
 * Parses log text and detects CH rotation messages
 * Ported from desktop app's LogMonitor.cs
 */
export class LogParser {
  private chainMessageRegex: RegExp;
  private appendMacroRegex: RegExp;
  private rotatonatorConfigRegex: RegExp;

  constructor(chainPrefix: string = 'D&D') {
    this.chainMessageRegex = this.buildChainMessageRegex(chainPrefix);
    this.appendMacroRegex = /rotat:(\d+),\s*(\S+)/i;
    // Matches desktop pattern: Rotatonator set_chain: 111 Name1, 222 Name2, set_delay: X
    // or delay-only: Rotatonator set_delay: X
    // set_delay is REQUIRED, set_chain is optional
    this.rotatonatorConfigRegex = /Rotatonator\s+(?:set_chain:\s*(.+?)\s*,\s*)?set_delay:\s*(\d+)/i;
  }

  /**
   * Build the chain message regex pattern with the configured prefix
   * Tolerant to sanitized separators (e.g., "D&D" showing up as "D D")
   */
  private buildChainMessageRegex(configuredPrefix: string): RegExp {
    const prefixPattern = this.buildTolerantPrefixPattern(configuredPrefix);
    const pattern = `^\\[.*?\\]\\s+.+?,\\s+'${prefixPattern}\\s+(\\d+|[A-Za-z]+)\\s+CH(?:\\s+-\\s+([^-]+))?`;
    return new RegExp(pattern, 'i');
  }

  /**
   * Build a pattern that matches the prefix with optional sanitized separators
   * E.g., "D&D" matches "D&D" or "D D"
   */
  private buildTolerantPrefixPattern(configuredPrefix: string): string {
    const trimmedPrefix = configuredPrefix.trim();
    if (!trimmedPrefix) {
      return this.escapeRegex(trimmedPrefix);
    }

    const chunks = trimmedPrefix
      .split(/[^A-Za-z0-9]+/)
      .filter(chunk => chunk.length > 0)
      .map(chunk => this.escapeRegex(chunk));

    if (chunks.length === 0) {
      return this.escapeRegex(trimmedPrefix);
    }

    return chunks.join('[^A-Za-z0-9]*');
  }

  /**
   * Simple regex escape
   */
  private escapeRegex(str: string): string {
    return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  }

  /**
   * Parse log text and return array of entries
   */
  parseLogText(logText: string): LogEntry[] {
    const lines = logText.split('\n');
    const entries: LogEntry[] = [];

    for (const line of lines) {
      if (!line.trim()) continue;

      const entry = this.parseLine(line);
      if (entry) {
        entries.push(entry);
      }
    }

    return entries;
  }

  /**
   * Parse a single log line
   */
  private parseLine(line: string): LogEntry | null {
    // Extract timestamp from log line format: [Day Mon DD HH:MM:SS YYYY]
    const timestamp = this.extractTimestamp(line);

    // Extract healer name from log format: [timestamp] CharacterName says, 'message'
    const healerNameMatch = line.match(/\]\s+(\S+)\s+says,/);
    const healerName = healerNameMatch ? healerNameMatch[1] : undefined;

    // Check for Rotatonator config command first (highest priority)
    const configCommand = this.parseRotatonatorConfig(line);
    if (configCommand) {
      return {
        timestamp,
        rawLine: line,
        detectedAsChMessage: false,
        configCommand,
      };
    }

    // Check for CH message
    const chMatch = this.chainMessageRegex.exec(line);
    if (chMatch) {
      const positionStr = chMatch[1];
      const targetName = chMatch[2] ? chMatch[2].trim() : '';
      const position = PositionHelper.stringToPosition(positionStr);

      return {
        timestamp,
        rawLine: line,
        detectedAsChMessage: true,
        chData: {
          positionStr,
          position,
          healer: healerName,
          targetName,
        },
      };
    }

    // Check for append macro format: rotat:<number>, <target>
    const appendMatch = this.appendMacroRegex.exec(line);
    if (appendMatch) {
      const positionStr = appendMatch[1];
      const targetName = appendMatch[2];
      const position = parseInt(positionStr, 10);

      if (position > 0 && position <= 35) {
        const repeatedPosition = PositionHelper.positionToString(position);
        return {
          timestamp,
          rawLine: line,
          detectedAsChMessage: true,
          chData: {
            positionStr: repeatedPosition,
            position,
            healer: healerName,
            targetName,
          },
        };
      }
    }

    // Regular log entry (not a CH message)
    return {
      timestamp,
      rawLine: line,
      detectedAsChMessage: false,
    };
  }

  /**
   * Extract timestamp from log line format: [Day Mon DD HH:MM:SS YYYY]
   */
  private extractTimestamp(line: string): Date {
    const tsEnd = line.indexOf(']');
    if (line.startsWith('[') && tsEnd > 0) {
      const tsStr = line.substring(1, tsEnd);
      try {
        // Parse EQ log timestamp format
        const parsed = new Date(tsStr);
        if (!isNaN(parsed.getTime())) {
          return parsed;
        }
      } catch {
        // Fall through to default
      }
    }
    return new Date();
  }

  /**
   * Parse Rotatonator config command from a log line
   * Desktop pattern: Rotatonator set_chain: 111 Name1, 222 Name2, set_delay: X
   * Or delay-only: Rotatonator set_delay: X
   * set_delay is REQUIRED (matches desktop behavior)
   */
  private parseRotatonatorConfig(line: string): RotatonatorConfigCommand | null {
    const match = this.rotatonatorConfigRegex.exec(line);
    if (!match) {
      return null;
    }

    // Group 1: optional chain data, Group 2: required delay
    const chainData = match[1];
    const delayStr = match[2];
    const delay = parseInt(delayStr, 10);

    if (isNaN(delay)) {
      return null;
    }

    const result: RotatonatorConfigCommand = {
      chainIntervalSeconds: delay
    };

    // Parse chain data if present
    if (chainData) {
      const healers: string[] = [];
      const parts = chainData.split(',');
      
      for (const part of parts) {
        const trimmed = part.trim();
        // Match pattern like "111 Healer1" or "AAA Healer10"
        // Extract just the healer name, ignore position code
        const entryMatch = /^[\d\w]+\s+(.+)$/.exec(trimmed);
        if (entryMatch) {
          healers.push(entryMatch[1].trim());
        }
      }
      
      if (healers.length > 0) {
        result.healers = healers;
      }
    }

    return result;
  }

  /**
   * Update the chain prefix (for when user changes config)
   */
  updateChainPrefix(prefix: string) {
    this.chainMessageRegex = this.buildChainMessageRegex(prefix);
  }
}
