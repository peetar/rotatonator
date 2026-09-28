using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Rotatonator
{
    public class ActiveRespawnTimer
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string MobName { get; set; } = "";
        public string ZoneName { get; set; } = "";
        public string KillerName { get; set; } = "";
        public string KillerGuild { get; set; } = "";
        public DateTime KillTimeUtc { get; set; }
        public int BaseDurationSeconds { get; set; }
        public DateTime ExpirationTimeUtc { get; set; }
        public DateTime? PvpWindowCloseTimeUtc { get; set; }
        public bool IsPvP { get; set; }
        public bool NotificationSent { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public TimeSpan TimeRemaining => ExpirationTimeUtc > DateTime.UtcNow 
            ? ExpirationTimeUtc - DateTime.UtcNow 
            : TimeSpan.Zero;

        public bool IsExpired => DateTime.UtcNow >= ExpirationTimeUtc;

        public string FormattedRemaining
        {
            get
            {
                if (NotificationSent)
                    return "Completed";
                var rem = TimeRemaining;
                if (rem.TotalDays >= 1.0)
                    return $"{rem.Days}d {rem.Hours}h {rem.Minutes}m";
                if (rem.TotalHours >= 1.0)
                    return $"{rem.Hours}h {rem.Minutes}m {rem.Seconds}s";
                return $"{rem.Minutes}m {rem.Seconds}s";
            }
        }

        public string StatusDisplay => NotificationSent ? "⏰ Alert Sent" : (IsExpired ? "⏰ Expired" : "⏳ Active");
        public string ModeDisplay => IsPvP ? "PvP Respawn (±20%)" : "PvE Lockout";
        public string KillerDisplay => string.IsNullOrWhiteSpace(KillerGuild) ? KillerName : $"{KillerName} <{KillerGuild}>";
        public string ExpirationDisplay => ExpirationTimeUtc.ToLocalTime().ToString("g");
        public string BaseDurationDisplay
        {
            get
            {
                double hours = Math.Round(BaseDurationSeconds / 3600.0, 1);
                double days = Math.Round(BaseDurationSeconds / 86400.0, 2);
                return days >= 1.0 ? $"{days}d ({hours}h)" : $"{hours}h";
            }
        }
    }

    /// <summary>
    /// Manages persistent raid lockout and respawn window timers.
    /// Saves timers to local storage to survive app restarts and posts follow-up alerts to Discord.
    /// </summary>
    public class RespawnTimerService : IDisposable
    {
        private static readonly string TimersFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Rotatonator",
            "respawn_timers.json"
        );

        private readonly List<ActiveRespawnTimer> timers = new List<ActiveRespawnTimer>();
        private readonly object lockObj = new object();
        private Timer? evaluationTimer;
        private bool isDisposed = false;

        public event EventHandler<ActiveRespawnTimer>? TimerExpired;
        public event EventHandler? TimersChanged;

        public bool IsEnabled { get; set; } = true;
        public Func<string, Task>? DiscordPostCallback { get; set; }

        public RespawnTimerService()
        {
            LoadTimers();
            // Start background evaluation every 15 seconds
            evaluationTimer = new Timer(OnEvaluateTimers, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
        }

        public IReadOnlyList<ActiveRespawnTimer> GetActiveTimers()
        {
            lock (lockObj)
            {
                return timers.Where(t => !t.NotificationSent).OrderBy(t => t.ExpirationTimeUtc).ToList();
            }
        }

        public IReadOnlyList<ActiveRespawnTimer> GetAllTimers()
        {
            lock (lockObj)
            {
                return timers.OrderByDescending(t => t.KillTimeUtc).ToList();
            }
        }

        /// <summary>
        /// Schedules a new raid timer if the target is found in RaidTargetDatabase.
        /// </summary>
        public ActiveRespawnTimer? ScheduleRaidKillTimer(
            string mobName,
            string zoneName,
            string killerName,
            string killerGuild,
            bool isPvP,
            DateTime? killTimeUtc = null)
        {
            if (!IsEnabled)
                return null;

            if (!RaidTargetDatabase.TryFindTarget(mobName, out var targetInfo))
            {
                System.Diagnostics.Debug.WriteLine($"[RespawnTimer] Mob '{mobName}' not found in raid target database.");
                return null;
            }

            var killUtc = killTimeUtc ?? DateTime.UtcNow;
            // For PvP, the single warning triggers when the window opens at 80% (0.8x) of base timer.
            // For PvE, lockout expires at 100% (1.0x) of base timer.
            var expireUtc = isPvP 
                ? killUtc.AddSeconds(targetInfo.BaseTimerSeconds * 0.8)
                : killUtc.AddSeconds(targetInfo.BaseTimerSeconds);
            DateTime? pvpCloseUtc = isPvP ? killUtc.AddSeconds(targetInfo.BaseTimerSeconds * 1.2) : null;

            var timer = new ActiveRespawnTimer
            {
                MobName = targetInfo.Name,
                ZoneName = zoneName,
                KillerName = killerName,
                KillerGuild = killerGuild,
                KillTimeUtc = killUtc,
                BaseDurationSeconds = targetInfo.BaseTimerSeconds,
                ExpirationTimeUtc = expireUtc,
                PvpWindowCloseTimeUtc = pvpCloseUtc,
                IsPvP = isPvP,
                NotificationSent = false
            };

            lock (lockObj)
            {
                // Remove any existing active timer for the same mob and zone to avoid duplicates
                timers.RemoveAll(t => !t.NotificationSent && 
                    string.Equals(t.MobName, timer.MobName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(t.ZoneName, timer.ZoneName, StringComparison.OrdinalIgnoreCase));

                timers.Add(timer);
            }

            SaveTimers();
            TimersChanged?.Invoke(this, EventArgs.Empty);

            System.Diagnostics.Debug.WriteLine($"[RespawnTimer] Scheduled timer for '{timer.MobName}' in '{timer.ZoneName}' (Expires: {timer.ExpirationTimeUtc:u})");
            return timer;
        }

        private async void OnEvaluateTimers(object? state)
        {
            if (isDisposed || !IsEnabled)
                return;

            List<ActiveRespawnTimer> expiredToNotify = new List<ActiveRespawnTimer>();

            lock (lockObj)
            {
                var nowUtc = DateTime.UtcNow;
                foreach (var timer in timers)
                {
                    if (!timer.NotificationSent && nowUtc >= timer.ExpirationTimeUtc)
                    {
                        expiredToNotify.Add(timer);
                    }
                }
            }

            if (expiredToNotify.Count == 0)
                return;

            foreach (var timer in expiredToNotify)
            {
                try
                {
                    await SendFollowUpNotificationAsync(timer);
                    lock (lockObj)
                    {
                        timer.NotificationSent = true;
                    }
                    TimerExpired?.Invoke(this, timer);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[RespawnTimer] Error notifying expired timer: {ex.Message}");
                }
            }

            SaveTimers();
            TimersChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task SendFollowUpNotificationAsync(ActiveRespawnTimer timer)
        {
            if (DiscordPostCallback == null)
                return;

            string message = FormatExpirationMessage(timer);
            await DiscordPostCallback(message);
        }

        public static string FormatExpirationMessage(ActiveRespawnTimer timer)
        {
            string zone = timer.ZoneName.Replace(" (Instanced)", "").Trim();

            if (timer.IsPvP)
            {
                return $"[PVP] {timer.MobName} in {zone} is in respawn window";
            }
            else
            {
                return $"[Raid Lockout] {timer.MobName} in {zone} is off lockout";
            }
        }

        private void LoadTimers()
        {
            try
            {
                if (!File.Exists(TimersFilePath))
                    return;

                string json = File.ReadAllText(TimersFilePath);
                var loaded = JsonSerializer.Deserialize<List<ActiveRespawnTimer>>(json);
                if (loaded != null)
                {
                    lock (lockObj)
                    {
                        timers.Clear();
                        // Filter out timers completed more than 30 days ago to prevent indefinite growth
                        var cutoff = DateTime.UtcNow.AddDays(-30);
                        foreach (var item in loaded)
                        {
                            if (!item.NotificationSent || item.ExpirationTimeUtc > cutoff)
                            {
                                timers.Add(item);
                            }
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"[RespawnTimer] Loaded {timers.Count} timers from {TimersFilePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RespawnTimer] Error loading timers: {ex.Message}");
            }
        }

        private void SaveTimers()
        {
            try
            {
                string? dir = Path.GetDirectoryName(TimersFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                lock (lockObj)
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(timers, options);
                    File.WriteAllText(TimersFilePath, json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RespawnTimer] Error saving timers: {ex.Message}");
            }
        }

        public void RemoveTimer(string id)
        {
            lock (lockObj)
            {
                timers.RemoveAll(t => t.Id == id);
            }
            SaveTimers();
            TimersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ClearCompleted()
        {
            lock (lockObj)
            {
                timers.RemoveAll(t => t.NotificationSent);
            }
            SaveTimers();
            TimersChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task ForceExpireTimerAsync(string id)
        {
            ActiveRespawnTimer? target = null;
            lock (lockObj)
            {
                target = timers.FirstOrDefault(t => t.Id == id);
            }

            if (target != null)
            {
                await SendFollowUpNotificationAsync(target);
                lock (lockObj)
                {
                    target.NotificationSent = true;
                }
                TimerExpired?.Invoke(this, target);
                SaveTimers();
                TimersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                evaluationTimer?.Dispose();
                evaluationTimer = null;
            }
        }
    }
}
