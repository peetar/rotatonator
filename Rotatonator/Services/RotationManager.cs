using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Threading;

namespace Rotatonator
{
    public class RotationManager
    {
        public RotationConfig Config { get; private set; }
        
        private DateTime? lastCastTime;
        private string? lastCaster;
        private DispatcherTimer? playerTurnTimer;

        public event EventHandler<HealCastEventArgs>? HealCastDetected;
        public event EventHandler<PlayerTurnEventArgs>? PlayerTurnStarting;
        public event EventHandler? PlayerTurnNow;
        public event EventHandler<ChainImportEventArgs>? ChainImported;

        public RotationManager(RotationConfig config)
        {
            Config = config;
        }

        public int GetPlayerPosition()
        {
            return Config.Healers.FindIndex(h => 
                h.Equals(Config.PlayerName, StringComparison.OrdinalIgnoreCase));
        }

        public void OnChainImport(string chainData, int delay)
        {
            try
            {
                // Parse chain data: "111 Name1, 222 Name2, 333 Name3, AAA Name10, BBB Name11"
                var healers = new List<string>();
                var parts = chainData.Split(',');
                
                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    // Match pattern like "111 Healer1" or "AAA Healer10"
                    var match = Regex.Match(trimmed, @"^[\d\w]+\s+(.+)$");
                    if (match.Success)
                    {
                        healers.Add(match.Groups[1].Value.Trim());
                    }
                }

                if (healers.Count > 0)
                {
                    // Update config
                    Config.Healers = healers;
                    Config.ChainInterval = TimeSpan.FromSeconds(delay);
                    
                    // Notify that chain was imported
                    ChainImported?.Invoke(this, new ChainImportEventArgs
                    {
                        Healers = healers,
                        Delay = delay
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing chain import: {ex.Message}");
            }
        }

        public void OnHealCast(string healerName, string targetName = "")
        {
            var castTime = DateTime.Now;
            bool isPlayerCast = !string.IsNullOrWhiteSpace(Config.PlayerName) && 
                               healerName.Equals(Config.PlayerName, StringComparison.OrdinalIgnoreCase);

            // Raise event for UI update
            HealCastDetected?.Invoke(this, new HealCastEventArgs
            {
                HealerName = healerName,
                TargetName = targetName,
                CastTime = castTime,
                IsPlayerCast = isPlayerCast
            });

            lastCastTime = castTime;
            lastCaster = healerName;

            // Calculate when player should cast next (only if player name is set)
            if (!string.IsNullOrWhiteSpace(Config.PlayerName))
            {
                CalculatePlayerTurn(healerName, castTime);
            }
        }

        private void CalculatePlayerTurn(string currentCaster, DateTime castTime)
        {
            var currentIndex = Config.Healers.FindIndex(h => 
                h.Equals(currentCaster, StringComparison.OrdinalIgnoreCase));
            
            if (currentIndex < 0) return;

            var playerIndex = GetPlayerPosition();
            if (playerIndex < 0) return;

            // Check if the healer who just cast is directly before the player in rotation
            int nextIndex = (currentIndex + 1) % Config.Healers.Count;
            bool playerIsNext = nextIndex == playerIndex;

            if (playerIsNext)
            {
                // Player is next! Beep after the chain interval
                var timeUntilPlayerTurn = Config.ChainInterval;
                
                Console.WriteLine($"[RotationManager] YOU'RE NEXT! Time until your cast: {timeUntilPlayerTurn.TotalSeconds:F1}s");
                
                // Show "YOU'RE NEXT" warning immediately
                PlayerTurnStarting?.Invoke(this, new PlayerTurnEventArgs
                {
                    TimeUntilCast = Config.ChainInterval
                });
                
                Console.WriteLine($"[RotationManager] Starting timer for {timeUntilPlayerTurn.TotalSeconds}s");
                
                // Ensure timer is created on the UI thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    playerTurnTimer?.Stop();
                    playerTurnTimer = new DispatcherTimer
                    {
                        Interval = timeUntilPlayerTurn
                    };
                    playerTurnTimer.Tick += (s, e) =>
                    {
                        playerTurnTimer.Stop();
                        Console.WriteLine("[RotationManager] Timer fired! Your turn to cast now!");
                        OnPlayerTurn();
                    };
                    Console.WriteLine("[RotationManager] Timer created and starting...");
                    playerTurnTimer.Start();
                    Console.WriteLine($"[RotationManager] Timer started. IsEnabled: {playerTurnTimer.IsEnabled}");
                });
            }
        }

        private void OnPlayerTurn()
        {
            Console.WriteLine("[RotationManager] OnPlayerTurn called!");
            
            // Trigger visual alert (red flash)
            PlayerTurnNow?.Invoke(this, EventArgs.Empty);

            // Audio alert when it's your turn to cast
            if (Config.EnableAudioBeep)
            {
                Console.WriteLine("[RotationManager] Playing audio alert for your turn");
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        Console.Beep(800, 100); // Higher pitch, 100ms duration for "your turn" alert
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[RotationManager] Beep failed: {ex.Message}");
                    }
                });
            }

            // Auto-cast
            if (Config.EnableAutoCast)
            {
                Console.WriteLine($"[RotationManager] Auto-cast enabled, sending hotkey: {Config.CastHotkey}");
                SendKeystroke(Config.CastHotkey);
            }
            else
            {
                Console.WriteLine("[RotationManager] Auto-cast is disabled");
            }
        }

        private void SendKeystroke(string key)
        {
            try
            {
                Console.WriteLine($"[RotationManager] Calling KeyboardAutomation.SendKey('{key}')");
                KeyboardAutomation.SendKey(key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RotationManager] Error sending keystroke: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error sending keystroke: {ex.Message}");
            }
        }
    }

    public class HealCastEventArgs : EventArgs
    {
        public string HealerName { get; set; } = "";
        public string TargetName { get; set; } = "";
        public DateTime CastTime { get; set; }
        public bool IsPlayerCast { get; set; }
    }

    public class PlayerTurnEventArgs : EventArgs
    {
        public TimeSpan TimeUntilCast { get; set; }
    }

    public class ChainImportEventArgs : EventArgs
    {
        public List<string> Healers { get; set; } = new();
        public int Delay { get; set; }
    }
}
