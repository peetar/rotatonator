using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rotatonator
{
    /// <summary>
    /// Service for sending formatted PVP events and Raid Kills to a Discord Webhook.
    /// </summary>
    public class DiscordWebhookService : IDisposable
    {
        private readonly HttpClient httpClient;
        private bool isDisposed = false;

        public string WebhookUrl { get; set; } = "";
        public bool PublishPvp { get; set; } = false;
        public bool PublishRaidKills { get; set; } = false;
        public RespawnTimerService? RespawnTimerService { get; set; }

        public DiscordWebhookService()
        {
            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(6)
            };
            try
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Rotatonator/1.6.1 (Windows; EQ Healer Assistant)");
            }
            catch { }
        }

        /// <summary>
        /// Sends a test message to the configured Discord Webhook URL.
        /// </summary>
        public async Task<(bool success, string error)> SendTestMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(WebhookUrl))
            {
                return (false, "Webhook URL is empty. Please enter your Discord Webhook URL first.");
            }

            try
            {
                var payload = new { content = "🎮 **[Rotatonator]** Discord Webhook connection successful! Ready to receive PVP and Raid Kill alerts. ⚔️🏆" };
                string json = JsonSerializer.Serialize(payload);
                using var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await httpClient.PostAsync(WebhookUrl.Trim(), stringContent);
                if (response.IsSuccessStatusCode)
                {
                    return (true, "");
                }
                else
                {
                    string body = await response.Content.ReadAsStringAsync();
                    return (false, $"HTTP {(int)response.StatusCode} {response.StatusCode}: {body}");
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Sends a PVP event to Discord if enabled and webhook URL is configured.
        /// </summary>
        public async Task SendPvpEventAsync(string rawLine)
        {
            if (!PublishPvp || string.IsNullOrWhiteSpace(WebhookUrl) || string.IsNullOrWhiteSpace(rawLine))
            {
                return;
            }

            try
            {
                string cleanLine = StripTimestamp(rawLine);
                string formattedMessage = FormatPvpMessage(cleanLine, RespawnTimerService);
                await PostToDiscordAsync(formattedMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DiscordWebhook] Error sending PVP event: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a Raid Kill event to Discord if enabled and webhook URL is configured.
        /// </summary>
        public async Task SendRaidKillEventAsync(string rawLine)
        {
            if (!PublishRaidKills || string.IsNullOrWhiteSpace(WebhookUrl) || string.IsNullOrWhiteSpace(rawLine))
            {
                return;
            }

            try
            {
                string cleanLine = StripTimestamp(rawLine);
                string formattedMessage = FormatRaidKillMessage(cleanLine, RespawnTimerService);
                await PostToDiscordAsync(formattedMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DiscordWebhook] Error sending Raid Kill event: {ex.Message}");
            }
        }

        private static string StripTimestamp(string line)
        {
            var match = Regex.Match(line, @"^\[.*?\]\s*(.*)$");
            return match.Success ? match.Groups[1].Value.Trim() : line.Trim();
        }

        public static string FormatPvpMessage(string rawLine, RespawnTimerService? timerService = null)
        {
            string line = rawLine.Trim();

            // Locate where "[PVP]" starts in the string (handles direct broadcast or simulated /say lines)
            int pvpIdx = line.IndexOf("[PVP]", StringComparison.OrdinalIgnoreCase);
            if (pvpIdx >= 0)
            {
                line = line.Substring(pvpIdx).Trim();
                // Strip trailing quote if typed in chat: e.g. You say, '[PVP] test'
                if (line.EndsWith("'"))
                {
                    line = line.Substring(0, line.Length - 1).Trim();
                }
            }

            // Try matching standard PVP death/kill patterns
            // Example: [PVP] Caedis of <Haven> has died to a grimling deathbringer in combat in Acrylia Caverns!
            // Example: [PVP] Player of <Guild> has been slain by Opponent in Zone!
            // Example: [PVP] Caedis of <Haven> has killed Lady Vox in Permafrost Caverns!
            var match = Regex.Match(line,
                @"^\[PVP\]\s+(?<player>[^\s<]+)(?:\s+of\s+<(?<guild>[^>]+)>)?\s+(?<action>has died to|has been slain by|was killed by|has killed)\s+(?<killer>.+?)(?:\s+in combat)?\s+in\s+(?<zone>[^!.]+)(?:!|\.)?$",
                RegexOptions.IgnoreCase);

            if (match.Success)
            {
                string player = match.Groups["player"].Value;
                string guild = match.Groups["guild"].Success ? $" `<{match.Groups["guild"].Value}>`" : "";
                string guildRaw = match.Groups["guild"].Success ? match.Groups["guild"].Value : "";
                string action = match.Groups["action"].Value;
                string killer = match.Groups["killer"].Value.Trim();
                string zone = match.Groups["zone"].Value.Trim();

                bool isKill = action.Equals("has killed", StringComparison.OrdinalIgnoreCase);

                // Check if this PvP action is actually a raid boss kill in PvP!
                if (isKill && timerService != null && RaidTargetDatabase.TryFindTarget(killer, out var raidTarget))
                {
                    var timer = timerService.ScheduleRaidKillTimer(raidTarget.Name, zone, player, guildRaw, isPvP: true);
                    if (timer != null)
                    {
                        var openLocal = timer.ExpirationTimeUtc.ToLocalTime();
                        var closeLocal = timer.PvpWindowCloseTimeUtc?.ToLocalTime();
                        string openStr = openLocal.ToString("M-d H:mm");
                        string closePart = closeLocal.HasValue ? $" | Closes {closeLocal.Value:M-d H:mm}" : "";

                        return $"⚔️ **[PVP Raid Kill]** **{player}**{guild} has killed **{raidTarget.Name}** in *{zone}*! 👑🐉 PvP Respawn: {openStr}{closePart}";
                    }
                }

                string actionEmoji = isKill ? "⚔️" : "💀";
                return $"{actionEmoji} **[PVP]** **{player}**{guild} *{action}* **{killer}** in *{zone}*!";
            }

            // Fallback for custom or unconventional PVP lines like "[PVP] test" or "[PVP] BLARG"
            string content = line.StartsWith("[PVP]", StringComparison.OrdinalIgnoreCase)
                ? line.Substring(5).Trim()
                : line;

            return $"⚔️ **[PVP]** {content}";
        }

        public static string FormatRaidKillMessage(string rawLine, RespawnTimerService? timerService = null)
        {
            string line = rawLine.Trim();
            string innerText = line;

            // Locate "tells the guild" (handles server broadcast or simulated /say lines)
            int guildIdx = line.IndexOf("tells the guild", StringComparison.OrdinalIgnoreCase);
            if (guildIdx >= 0)
            {
                string after = line.Substring(guildIdx + "tells the guild".Length);
                int q1 = after.IndexOf('\'');
                int q2 = after.LastIndexOf('\'');
                if (q1 >= 0 && q2 > q1)
                {
                    innerText = after.Substring(q1 + 1, q2 - q1 - 1).Trim();
                }
                else if (q1 >= 0)
                {
                    innerText = after.Substring(q1 + 1).Trim().TrimEnd('\'');
                }
            }
            else
            {
                int firstQuote = line.IndexOf('\'');
                int lastQuote = line.LastIndexOf('\'');
                if (firstQuote >= 0 && lastQuote > firstQuote)
                {
                    innerText = line.Substring(firstQuote + 1, lastQuote - firstQuote - 1).Trim();
                }
            }

            // Regex match for: Marosu of <Dungeons and Dragons> has killed Lady Vox in Permafrost Caverns!
            var match = Regex.Match(innerText,
                @"^(?<player>[^\s<]+)(?:\s+of\s+<(?<guild>[^>]+)>)?\s+has killed\s+(?<boss>.+?)\s+in\s+(?<zone>[^!.]+)(?:!|\.)?$",
                RegexOptions.IgnoreCase);

            if (match.Success)
            {
                string player = match.Groups["player"].Value;
                string guild = match.Groups["guild"].Success ? $" `<{match.Groups["guild"].Value}>`" : "";
                string guildRaw = match.Groups["guild"].Success ? match.Groups["guild"].Value : "";
                string boss = match.Groups["boss"].Value.Trim();
                string zone = match.Groups["zone"].Value.Trim();

                string msg = $"🏆 **[Raid Kill]** **{player}**{guild} has slain **{boss}** in *{zone}*! 👑🐉";

                // Schedule lockout timer if enabled
                if (timerService != null)
                {
                    var timer = timerService.ScheduleRaidKillTimer(boss, zone, player, guildRaw, isPvP: false);
                    if (timer != null)
                    {
                        var expireLocal = timer.ExpirationTimeUtc.ToLocalTime();
                        msg += $" Off Lockout: {expireLocal:M-d H:mm}";
                    }
                }

                return msg;
            }

            // Fallback: strip outer quotes if present and post clean achievement
            return $"🏆 **[Raid Kill]** {innerText}";
        }

        public async Task PostRawToDiscordAsync(string content)
        {
            await PostToDiscordAsync(content);
        }

        private async Task PostToDiscordAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(WebhookUrl))
            {
                return;
            }

            var payload = new { content };
            string json = JsonSerializer.Serialize(payload);
            using var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await httpClient.PostAsync(WebhookUrl.Trim(), stringContent);
            if (!response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[DiscordWebhook] Webhook failed ({response.StatusCode}): {responseBody}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DiscordWebhook] Successfully posted: {content}");
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                httpClient.Dispose();
            }
        }
    }
}
