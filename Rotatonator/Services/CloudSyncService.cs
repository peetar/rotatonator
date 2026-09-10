using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Rotatonator
{
    public class CloudChainPayload
    {
        [JsonPropertyName("prefix")]
        public string Prefix { get; set; } = "";

        [JsonPropertyName("healers")]
        public List<string> Healers { get; set; } = new();

        [JsonPropertyName("interval")]
        public double Interval { get; set; } = 6.0;

        [JsonPropertyName("sender")]
        public string Sender { get; set; } = "";

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
    }

    public class CloudChainResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public CloudChainPayload? Data { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    /// <summary>
    /// Service for pushing and polling chain configurations to/from the web API.
    /// </summary>
    public class CloudSyncService : IDisposable
    {
        private readonly HttpClient httpClient;
        private CancellationTokenSource? cts;
        private bool isDisposed = false;

        public event EventHandler<ChainImportEventArgs>? ChainUpdatedFromCloud;
        public event EventHandler<string>? SyncStatusChanged;

        public bool IsEnabled { get; set; } = false;
        public string BaseUrl { get; set; } = "https://rotatonator-web.vercel.app/";
        public string CurrentPrefix { get; set; } = "D&D";
        public long LastAppliedTimestamp { get; set; } = 0;

        public CloudSyncService()
        {
            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        /// <summary>
        /// Starts the background polling loop (polls every 5 seconds).
        /// </summary>
        public void StartPolling()
        {
            StopPolling();
            cts = new CancellationTokenSource();
            _ = PollLoopAsync(cts.Token);
        }

        /// <summary>
        /// Stops the background polling loop.
        /// </summary>
        public void StopPolling()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        private async Task PollLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), token);

                    if (!IsEnabled || string.IsNullOrWhiteSpace(BaseUrl) || string.IsNullOrWhiteSpace(CurrentPrefix))
                    {
                        continue;
                    }

                    await PollLatestChainAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CloudSync] Poll loop error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Queries the cloud API for the latest chain configuration for the current prefix.
        /// </summary>
        public async Task<bool> PollLatestChainAsync()
        {
            if (string.IsNullOrWhiteSpace(BaseUrl) || string.IsNullOrWhiteSpace(CurrentPrefix))
            {
                return false;
            }

            try
            {
                string cleanBase = BaseUrl.Trim().TrimEnd('/');
                string url = $"{cleanBase}/api/chain?prefix={Uri.EscapeDataString(CurrentPrefix.Trim())}";

                using var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                string json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CloudChainResponse>(json);

                if (result?.Success == true && result.Data != null)
                {
                    var data = result.Data;

                    // Only apply if the incoming chain has a strictly newer timestamp
                    if (data.Timestamp > LastAppliedTimestamp && data.Healers.Count > 0)
                    {
                        LastAppliedTimestamp = data.Timestamp;

                        int delayInt = (int)Math.Round(data.Interval);
                        if (delayInt <= 0) delayInt = 6;

                        SyncStatusChanged?.Invoke(this, $"Cloud update received: {data.Healers.Count} healers");

                        ChainUpdatedFromCloud?.Invoke(this, new ChainImportEventArgs
                        {
                            Healers = data.Healers,
                            Delay = delayInt
                        });

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CloudSync] Poll error: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Pushes the local chain configuration to the cloud API.
        /// </summary>
        public async Task<bool> PushChainAsync(string prefix, List<string> healers, double interval, string sender)
        {
            if (string.IsNullOrWhiteSpace(BaseUrl) || string.IsNullOrWhiteSpace(prefix) || healers.Count == 0)
            {
                return false;
            }

            try
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var payload = new CloudChainPayload
                {
                    Prefix = prefix.Trim(),
                    Healers = healers,
                    Interval = interval,
                    Sender = sender.Trim(),
                    Timestamp = now
                };

                string cleanBase = BaseUrl.Trim().TrimEnd('/');
                string url = $"{cleanBase}/api/chain";

                string jsonPayload = JsonSerializer.Serialize(payload);
                using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var response = await httpClient.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    // Record timestamp so we don't re-import our own export
                    LastAppliedTimestamp = now;
                    SyncStatusChanged?.Invoke(this, "Chain synced to cloud successfully");
                    return true;
                }
                else
                {
                    string errorText = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[CloudSync] Push failed ({response.StatusCode}): {errorText}");
                    SyncStatusChanged?.Invoke(this, $"Cloud sync failed ({response.StatusCode})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CloudSync] Push error: {ex.Message}");
                SyncStatusChanged?.Invoke(this, "Cloud sync error: unable to reach server");
            }

            return false;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                StopPolling();
                httpClient.Dispose();
            }
        }
    }
}
