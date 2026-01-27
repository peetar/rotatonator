using System;
using System.Collections.Generic;

namespace Rotatonator
{
    /// <summary>
    /// Tracks timing accuracy and perfect streaks for DDR mode
    /// </summary>
    public class DDRScoreTracker
    {
        private Dictionary<string, int> playerStreaks = new Dictionary<string, int>(); // good streaks
        private Dictionary<string, int> playerBadStreaks = new Dictionary<string, int>(); // bad streaks
        private Dictionary<string, int> playerScores = new Dictionary<string, int>();
        private const double PERFECT_THRESHOLD = 0.5; // ±0.5 seconds is "perfect"
        private const double LATE_THRESHOLD = 1.0; // >1 second is "late" (groan)

        // Scoring system
        private const int PERFECT_BASE_POINTS = 100;
        private const int EARLY_PENALTY = -25;
        private const int LATE_PENALTY = -50;
        
        // Combo multipliers (streak bonus points added to base)
        private static readonly Dictionary<int, int> ComboBonus = new Dictionary<int, int>
        {
            { 1, 0 },      // Great: 100 points
            { 2, 50 },     // Wow: 150 points
            { 3, 100 },    // Heating Up: 200 points
            { 4, 150 },    // Perfect: 250 points
            { 5, 200 },    // Perfect: 300 points
            { 6, 300 },    // On Fire: 400 points
            { 7, 500 },    // High Score: 600 points
        };

        /// <summary>
        /// Evaluate timing accuracy for a heal cast
        /// </summary>
        /// <param name="expectedTime">When the heal should have been cast</param>
        /// <param name="actualTime">When the heal was actually cast</param>
        /// <param name="healerName">Name of the healer</param>
        /// <param name="isPlayer">True if this is the current player</param>
        /// <returns>Timing result with accuracy and feedback type</returns>
        public DDRTimingResult EvaluateTiming(DateTime expectedTime, DateTime actualTime, string healerName, bool isPlayer)
        {
            var timeDifference = (actualTime - expectedTime).TotalSeconds;
            var result = new DDRTimingResult
            {
                TimeDifference = timeDifference,
                IsPlayer = isPlayer,
                HealerName = healerName
            };

            // Ensure healer exists in dictionaries
            if (!playerStreaks.ContainsKey(healerName))
            {
                playerStreaks[healerName] = 0;
                playerBadStreaks[healerName] = 0;
                playerScores[healerName] = 0;
            }

            // Check if early
            if (timeDifference < -PERFECT_THRESHOLD)
            {
                result.Accuracy = DDRAccuracy.Early;
                playerStreaks[healerName] = 0; // Break good streak
                playerBadStreaks[healerName]++;
                result.PointsAwarded = EARLY_PENALTY;
                playerScores[healerName] = Math.Max(0, playerScores[healerName] + EARLY_PENALTY);
                result.TotalScore = playerScores[healerName];
                return result;
            }

            // Check if late
            if (timeDifference > LATE_THRESHOLD)
            {
                result.Accuracy = DDRAccuracy.Late;
                playerStreaks[healerName] = 0; // Break good streak
                playerBadStreaks[healerName]++;
                result.PointsAwarded = LATE_PENALTY;
                playerScores[healerName] = Math.Max(0, playerScores[healerName] + LATE_PENALTY);
                result.TotalScore = playerScores[healerName];
                return result;
            }

            // Perfect timing (within ±0.5 seconds)
            result.Accuracy = DDRAccuracy.Perfect;
            playerStreaks[healerName]++;
            playerBadStreaks[healerName] = 0; // Reset bad streak
            result.PerfectStreak = playerStreaks[healerName];
            result.ComboLevel = GetComboLevel(playerStreaks[healerName]);
            
            // Calculate points with combo bonus
            int comboBonus = playerStreaks[healerName] <= 7 
                ? ComboBonus[playerStreaks[healerName]] 
                : 500; // 8+ streak = 600 points
            
            result.PointsAwarded = PERFECT_BASE_POINTS + comboBonus;
            playerScores[healerName] += result.PointsAwarded;
            result.TotalScore = playerScores[healerName];

            return result;
        }

        /// <summary>
        /// Get the current bad streak count for a healer
        /// </summary>
        public int GetCurrentBadStreak(string healerName)
        {
            return playerBadStreaks.ContainsKey(healerName) ? playerBadStreaks[healerName] : 0;
        }

        /// <summary>
        /// Get the combo level based on perfect streak count
        /// </summary>
        private DDRComboLevel GetComboLevel(int streak)
        {
            return streak switch
            {
                1 => DDRComboLevel.Great,           // "Great"
                2 => DDRComboLevel.Wow,             // "WOW!"
                3 => DDRComboLevel.HeatingUp,       // "You're heating up"
                4 => DDRComboLevel.Perfect,         // "Perfect"
                5 => DDRComboLevel.Perfect,         // "Perfect"
                6 => DDRComboLevel.OnFire,          // "You're on fire!"
                7 => DDRComboLevel.HighScore,       // "New high score!!!"
                _ => DDRComboLevel.Perfect          // "Perfect" for 8+
            };
        }

        /// <summary>
        /// Reset the player's perfect streak
        /// </summary>
        public void ResetStreak(string healerName)
        {
            if (playerStreaks.ContainsKey(healerName))
            {
                playerStreaks[healerName] = 0;
            }
        }

        /// <summary>
        /// Get the current perfect streak count for a healer
        /// </summary>
        public int GetCurrentStreak(string healerName)
        {
            return playerStreaks.ContainsKey(healerName) ? playerStreaks[healerName] : 0;
        }

        /// <summary>
        /// Get all healer scores sorted by score (highest first)
        /// </summary>
        public List<DDRHealerScore> GetLeaderboard()
        {
            var leaderboard = new List<DDRHealerScore>();
            foreach (var kvp in playerScores)
            {
                leaderboard.Add(new DDRHealerScore
                {
                    HealerName = kvp.Key,
                    Score = kvp.Value,
                    CurrentStreak = playerStreaks.ContainsKey(kvp.Key) ? playerStreaks[kvp.Key] : 0
                });
            }
            
            leaderboard.Sort((a, b) => b.Score.CompareTo(a.Score));
            return leaderboard;
        }

        /// <summary>
        /// Reset all scores and streaks
        /// </summary>
        public void ResetAll()
        {
            playerStreaks.Clear();
            playerBadStreaks.Clear();
            playerScores.Clear();
        }
    }

    public enum DDRAccuracy
    {
        Perfect,    // Within ±0.5 seconds
        Early,      // More than 0.5 seconds early
        Late        // More than 1 second late
    }

    public enum DDRComboLevel
    {
        None,
        Great,          // Streak 1
        Wow,            // Streak 2
        HeatingUp,      // Streak 3
        Perfect,        // Streak 4-5, 8+
        OnFire,         // Streak 6
        HighScore       // Streak 7
    }

    public class DDRTimingResult
    {
        public double TimeDifference { get; set; }  // Seconds (negative = early, positive = late)
        public DDRAccuracy Accuracy { get; set; }
        public bool IsPlayer { get; set; }
        public string HealerName { get; set; } = "";
        public int PerfectStreak { get; set; }
        public DDRComboLevel ComboLevel { get; set; }
        public int PointsAwarded { get; set; }  // Points earned/lost this cast
        public int TotalScore { get; set; }  // Total score for this healer
    }

    public class DDRHealerScore
    {
        public string HealerName { get; set; } = "";
        public int Score { get; set; }
        public int CurrentStreak { get; set; }
    }
}
