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
                // Parse chain data: "111 Name1, 222 Name2, 333 Name3"
                var healers = new List<string>();
                var parts = chainData.Split(',');
                
                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    // Match pattern like "111 Healer1" or "22 Healer2"
                    var match = Regex.Match(trimmed, @"^\d+\s+(.+)$");
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

        public void OnHealCast(string healerName)
        {
            var castTime = DateTime.Now;
            bool isPlayerCast = healerName.Equals(Config.PlayerName, StringComparison.OrdinalIgnoreCase);

            // Raise event for UI update
            HealCastDetected?.Invoke(this, new HealCastEventArgs
            {
                HealerName = healerName,
                CastTime = castTime,
                IsPlayerCast = isPlayerCast
            });

            lastCastTime = castTime;
            lastCaster = healerName;

            // Calculate when player should cast next
            CalculatePlayerTurn(healerName, castTime);
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
                // Show "YOU'RE NEXT" warning immediately
                PlayerTurnStarting?.Invoke(this, new PlayerTurnEventArgs
                {
                    TimeUntilCast = Config.ChainInterval
                });
                
                // Player is next! Beep after the chain interval
                var timeUntilPlayerTurn = Config.ChainInterval;
                
                System.Diagnostics.Debug.WriteLine($"Player is next! Will beep in {timeUntilPlayerTurn.TotalSeconds} seconds");
                
                playerTurnTimer?.Stop();
                playerTurnTimer = new DispatcherTimer
                {
                    Interval = timeUntilPlayerTurn
                };
                playerTurnTimer.Tick += (s, e) =>
                {
                    playerTurnTimer.Stop();
                    System.Diagnostics.Debug.WriteLine("Timer fired! Playing beep now...");
                    OnPlayerTurn();
                };
                playerTurnTimer.Start();
            }
        }

        private void OnPlayerTurn()
        {
            System.Diagnostics.Debug.WriteLine("OnPlayerTurn called!");
            
            // Trigger visual alert (red flash)
            PlayerTurnNow?.Invoke(this, EventArgs.Empty);

            // Auto-cast
            if (Config.EnableAutoCast)
            {
                SendKeystroke(Config.CastHotkey);
            }
        }

        private void SendKeystroke(string key)
        {
            try
            {
                KeyboardAutomation.SendKey(key);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending keystroke: {ex.Message}");
            }
        }
    }

    public class HealCastEventArgs : EventArgs
    {
        public string HealerName { get; set; } = "";
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
