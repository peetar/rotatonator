/**
 * Helper for converting between chain positions and their string representations
 * Positions 1-9 use numbers (111, 222, 333, etc.)
 * Positions 10-35 use letters (AAA, BBB, CCC, ... ZZZ)
 */

export class PositionHelper {
  /**
   * Converts a position number (1-35) to its 3-character string representation
   */
  static positionToString(position: number): string {
    if (position < 1 || position > 35) {
      throw new Error(`Position must be between 1 and 35, got ${position}`);
    }

    if (position <= 9) {
      // Positions 1-9: "111", "222", "333", etc.
      const digit = String.fromCharCode('0'.charCodeAt(0) + position);
      return digit.repeat(3);
    } else {
      // Positions 10-35: "AAA", "BBB", "CCC", ... "ZZZ"
      const letter = String.fromCharCode('A'.charCodeAt(0) + (position - 10));
      return letter.repeat(3);
    }
  }

  /**
   * Converts a 3-character string representation to a position number (1-35)
   * Returns -1 if the string is not a valid position
   */
  static stringToPosition(positionStr: string): number {
    if (!positionStr || positionStr.length < 1) {
      return -1;
    }

    const firstChar = positionStr[0];

    // Verify all characters are the same
    if (!positionStr.split('').every(c => c === firstChar)) {
      return -1;
    }

    // Check if it's a digit (1-9)
    if (/[0-9]/.test(firstChar)) {
      const digit = parseInt(firstChar, 10);
      if (digit >= 1 && digit <= 9) {
        return digit;
      }
      return -1;
    }

    // Check if it's a letter (A-Z for positions 10-35)
    if (/[A-Za-z]/.test(firstChar)) {
      const upperChar = firstChar.toUpperCase();
      if (upperChar >= 'A' && upperChar <= 'Z') {
        const position = 10 + (upperChar.charCodeAt(0) - 'A'.charCodeAt(0));
        return position <= 35 ? position : -1;
      }
    }

    return -1;
  }

  /**
   * Gets the display name for an invalid position (shows the repeated character)
   */
  static getInvalidPositionName(positionStr: string): string {
    if (!positionStr) {
      return 'Unknown';
    }
    return positionStr;
  }
}
