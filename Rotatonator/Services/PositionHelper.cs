using System;
using System.Linq;

namespace Rotatonator
{
    /// <summary>
    /// Helper class for converting between chain positions and their string representations
    /// Positions 1-9 use numbers (111, 222, 333, etc.)
    /// Positions 10-35 use letters (AAA, BBB, CCC, ... ZZZ)
    /// </summary>
    public static class PositionHelper
    {
        /// <summary>
        /// Converts a position number (1-35) to its 3-character string representation
        /// </summary>
        public static string PositionToString(int position)
        {
            if (position < 1 || position > 35)
                throw new ArgumentOutOfRangeException(nameof(position), "Position must be between 1 and 35");

            if (position <= 9)
            {
                // Positions 1-9: "111", "222", "333", etc.
                char digit = (char)('0' + position);
                return new string(digit, 3);
            }
            else
            {
                // Positions 10-35: "AAA", "BBB", "CCC", ... "ZZZ"
                char letter = (char)('A' + (position - 10));
                return new string(letter, 3);
            }
        }

        /// <summary>
        /// Converts a 3-character string representation to a position number (1-35)
        /// Returns -1 if the string is not a valid position
        /// </summary>
        public static int StringToPosition(string positionStr)
        {
            if (string.IsNullOrEmpty(positionStr) || positionStr.Length < 1)
                return -1;

            // Get the first character
            char firstChar = positionStr[0];

            // Verify all characters are the same
            if (!positionStr.All(c => c == firstChar))
                return -1;

            // Check if it's a digit (1-9)
            if (char.IsDigit(firstChar))
            {
                int digit = firstChar - '0';
                if (digit >= 1 && digit <= 9)
                    return digit;
                return -1;
            }

            // Check if it's a letter (A-Z for positions 10-35)
            if (char.IsLetter(firstChar))
            {
                char upperChar = char.ToUpper(firstChar);
                if (upperChar >= 'A' && upperChar <= 'Z')
                {
                    int position = 10 + (upperChar - 'A');
                    return position <= 35 ? position : -1;
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets the display name for an invalid position (shows the repeated character)
        /// </summary>
        public static string GetInvalidPositionName(string positionStr)
        {
            if (string.IsNullOrEmpty(positionStr))
                return "Unknown";

            // Return the position string itself (e.g., "555", "XXX")
            return positionStr;
        }
    }
}
