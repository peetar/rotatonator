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

        // Scoring system
        private const int PERFECT_BASE_POINTS = 100;
        private const int EARLY_PENALTY = -25;
        private const int LATE_PENALTY = -50;
        
        /// <summary>
        /// Calculate timing thresholds based on chain interval.
        /// For 7+ second intervals: ±0.5s perfect, >1.0s late
        /// For 2- second intervals: ±0.25s perfect, >0.5s late
        /// Linear scaling in between
        /// </summary>
        private (double perfectThreshold, double lateThreshold) GetTimingThresholds(TimeSpan chainInterval)
        {
            double intervalSeconds = chainInterval.TotalSeconds;
            
            // Clamp interval between 2 and 7 seconds for calculation
            double clampedInterval = Math.Max(2.0, Math.Min(7.0, intervalSeconds));
            
            // Linear interpolation between:
            // 2s interval -> 0.25s perfect, 0.5s late
            // 7s interval -> 0.5s perfect, 1.0s late
            double t = (clampedInterval - 2.0) / 5.0; // Normalize to 0-1 range
            
            double perfectThreshold = 0.25 + (t * 0.25); // 0.25 to 0.5
            double lateThreshold = 0.5 + (t * 0.5);      // 0.5 to 1.0
            
            return (perfectThreshold, lateThreshold);
        }
        
        // Combo multipliers (streak bonus points added to base 100)
        private static readonly Dictionary<int, int> ComboBonus = new Dictionary<int, int>
        {
            { 1, 0 },      // 1 heal: 100 points
            { 2, 50 },     // 2 heals: 150 points
            { 3, 100 },    // 3 heals: 200 points
            { 4, 150 },    // 4+ heals: 250 points (max)
        };

        /// <summary>
        /// Evaluate timing accuracy for a heal cast
        /// </summary>
        /// <param name="expectedTime">When the heal should have been cast</param>
        /// <param name="actualTime">When the heal was actually cast</param>
        /// <param name="healerName">Name of the healer</param>
        /// <param name="isPlayer">True if this is the current player</param>
        /// <param name="chainInterval">The chain heal interval (used to scale timing thresholds)</param>
        /// <returns>Timing result with accuracy and feedback type</returns>
        public DDRTimingResult EvaluateTiming(DateTime expectedTime, DateTime actualTime, string healerName, bool isPlayer, TimeSpan chainInterval)
        {
            var timeDifference = (actualTime - expectedTime).TotalSeconds;
            var (perfectThreshold, lateThreshold) = GetTimingThresholds(chainInterval);
            
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
            if (timeDifference < -perfectThreshold)
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
            if (timeDifference > lateThreshold)
            {
                result.Accuracy = DDRAccuracy.Late;
                playerStreaks[healerName] = 0; // Break good streak
                playerBadStreaks[healerName]++;
                result.PointsAwarded = LATE_PENALTY;
                playerScores[healerName] = Math.Max(0, playerScores[healerName] + LATE_PENALTY);
                result.TotalScore = playerScores[healerName];
                return result;
            }

            // Perfect timing (within threshold based on interval)
            result.Accuracy = DDRAccuracy.Perfect;
            playerStreaks[healerName]++;
            playerBadStreaks[healerName] = 0; // Reset bad streak
            result.PerfectStreak = playerStreaks[healerName];
            result.ComboLevel = GetComboLevel(playerStreaks[healerName]);
            
            // Calculate points with combo bonus (capped at 250 after 4 good heals)
            int comboBonus = playerStreaks[healerName] <= 4 
                ? ComboBonus[playerStreaks[healerName]] 
                : 150; // 5+ streak = still 250 points (capped)
            
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
