using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rotatonator
{
    public class LogMonitor
    {
        // Buffer for recent log lines (timestamp, line)
        private readonly List<(DateTime timestamp, string line)> recentLogLines = new List<(DateTime, string)>();

        // Debug logging helper
        private void DebugLog(string msg)
        {
            System.Diagnostics.Debug.WriteLine($"[LogMonitor] {msg}");
        }
        private readonly string logFilePath;
        private readonly RotationManager rotationManager;
        private FileSystemWatcher? fileWatcher;
        private StreamReader? logReader;
        private long lastPosition = 0;
        private Regex? chainMessageRegex;
        private Regex? chainImportRegex;
        private Regex? appendMacroRegex;

        // EQ log format: [Day Mon DD HH:MM:SS YYYY] Message
        // Custom CH rotation format: [timestamp] CharacterName says, 'PREFIX ### CH ...'
        // Example: [Mon Jan 19 14:30:45 2026] Healer1 says, 'D&D 333 CH - %t - %n'
        // The position in chain is determined by how many times the digit repeats (1, 22, 333, 4444, etc.)
        // 
        // Import format: [timestamp] Someone tells the raid, 'Rotatonator set_chain: 111 Name1, 222 Name2, set_delay: 3'

        public LogMonitor(string logPath, RotationManager manager)
        {
            logFilePath = logPath;
            rotationManager = manager;
        }

        public void Start()
        {
            // Build regex pattern for detecting CH rotation messages using configured chain prefix.
            // This is tolerant to sanitized separators (e.g., "D&D" showing up as "D D" in some logs).
            string configuredPrefix = rotationManager.Config.ChainPrefix ?? string.Empty;
            string prefixPattern = BuildTolerantPrefixPattern(configuredPrefix);
            string pattern = $@"^\[.*?\]\s+.+?,\s+'{prefixPattern}\s+(\d+|[A-Za-z]+)\s+CH(?:\s+-\s+([^-]+))?";
            chainMessageRegex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            
            // Build regex for detecting chain import messages
            // Pattern: Rotatonator set_chain: 111 Name1, 222 Name2, set_delay: 3
            // Also matches delay-only format: Rotatonator set_delay: 3
            chainImportRegex = new Regex(
                @"Rotatonator\s+(?:set_chain:\s*(.+?)\s*,\s*)?set_delay:\s*(\d+)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase
            );

            appendMacroRegex = new Regex(
                @"rotat:(\d+),\s*(\S+)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase
            );
            
            // Open log file and seek to end
            var fileStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            lastPosition = fileStream.Length;
            fileStream.Seek(lastPosition, SeekOrigin.Begin);
            logReader = new StreamReader(fileStream);

            // Watch for changes
            fileWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(logFilePath) ?? "",
                Filter = Path.GetFileName(logFilePath),
                NotifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite
            };

            fileWatcher.Changed += OnLogFileChanged;
            fileWatcher.EnableRaisingEvents = true;
        }

        private static string BuildTolerantPrefixPattern(string configuredPrefix)
        {
            string trimmedPrefix = configuredPrefix.Trim();
            if (string.IsNullOrEmpty(trimmedPrefix))
            {
                return Regex.Escape(trimmedPrefix);
            }

            var chunks = Regex
                .Split(trimmedPrefix, @"[^A-Za-z0-9]+")
                .Where(chunk => !string.IsNullOrEmpty(chunk))
                .Select(Regex.Escape)
                .ToList();

            if (chunks.Count == 0)
            {
                return Regex.Escape(trimmedPrefix);
            }

            return string.Join(@"[^A-Za-z0-9]*", chunks);
        }

        public void Stop()
        {
            if (fileWatcher != null)
            {
                fileWatcher.EnableRaisingEvents = false;
                fileWatcher.Dispose();
                fileWatcher = null;
            }

            logReader?.Close();
            logReader = null;
        }

        private void OnLogFileChanged(object sender, FileSystemEventArgs e)
        {
            if (logReader == null) return;

            try
            {
                var newLines = new List<(string line, DateTime? logTime)>();
                string? line;
                while ((line = logReader.ReadLine()) != null)
                {
                    // Parse timestamp from log line (format: [Day Mon DD HH:MM:SS YYYY])
                    DateTime? logTime = null;
                    int tsEnd = line.IndexOf(']');
                    if (line.StartsWith("[") && tsEnd > 0)
                    {
                        string ts = line.Substring(1, tsEnd - 1);
                        if (DateTime.TryParse(ts, out var dt))
                            logTime = dt;
                    }
                    newLines.Add((line, logTime));
                }
                // Add all new lines to buffer first
                foreach (var entry in newLines)
                {
                    // Always add the line to the buffer, use DateTime.Now if timestamp is missing
                    var ts = entry.logTime ?? DateTime.Now;
                    recentLogLines.Add((ts, entry.line));
                    // Only keep the last 1000 lines
                    if (recentLogLines.Count > 1000)
                    {
                        recentLogLines.RemoveRange(0, recentLogLines.Count - 1000);
                    }
                }
                // Now process all new lines
                foreach (var entry in newLines)
                {
                    ProcessLogLine(entry.line);
                }
                lastPosition = logReader.BaseStream.Position;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading log: {ex.Message}");
            }
        }

        private void ProcessLogLine(string line)
        {
            // Check for chain import message first
            if (chainImportRegex != null)
            {
                var importMatch = chainImportRegex.Match(line);
                if (importMatch.Success)
                {
                    string? chainData = importMatch.Groups[1].Success ? importMatch.Groups[1].Value : null;
                    string delayStr = importMatch.Groups[2].Value;
                    
                    if (int.TryParse(delayStr, out int delay))
                    {
                        if (!string.IsNullOrEmpty(chainData))
                        {
                            // Full chain import with both chain and delay
                            rotationManager.OnChainImport(chainData, delay);
                        }
                        else
                        {
                            // Delay-only import - just update the delay
                            rotationManager.OnDelayOnlyImport(delay);
                        }
                        return;
                    }
                }
            }

            // Check for append macro format anywhere in the line: rotat:<number_in_chain>, <target>
            // If found, synthesize a normal CH macro line so it follows the exact same code path below.
            if (appendMacroRegex != null)
            {
                var appendMatch = appendMacroRegex.Match(line);
                if (appendMatch.Success)
                {
                    string positionStr = appendMatch.Groups[1].Value;
                    string targetName = appendMatch.Groups[2].Value;

                    if (int.TryParse(positionStr, out int position) && position > 0)
                    {
                        string repeatedPosition = PositionHelper.PositionToString(position);

                        string timestampPrefix = "";
                        int tsEnd = line.IndexOf(']');
                        if (line.StartsWith("[") && tsEnd > 0)
                        {
                            timestampPrefix = line.Substring(0, tsEnd + 1) + " ";
                        }

                        string chainPrefix = rotationManager.Config.ChainPrefix ?? string.Empty;
                        line = $"{timestampPrefix}You say, '{chainPrefix} {repeatedPosition} CH - {targetName} - %n'";
                    }
                }
            }
            
            if (chainMessageRegex == null) return;
            // Check for CH rotation message (e.g., "D&D 333 CH - Crunchzilla - 100%" or "D&D AAA CH - Target - 100%")
            var match = chainMessageRegex.Match(line);
            if (match.Success)
            {
                // Extract the position string (111, 222, AAA, BBB, etc.)
                string positionStr = match.Groups[1].Value;
                string targetName = match.Groups.Count > 2 && match.Groups[2].Success 
                    ? match.Groups[2].Value.Trim() 
                    : "";

                int position = PositionHelper.StringToPosition(positionStr);

                string healerName;
                if (position > 0)
                {
                    // Valid position - look up healer name
                    int healerIndex = position - 1;
                    if (healerIndex >= 0 && healerIndex < rotationManager.Config.Healers.Count)
                    {
                        healerName = rotationManager.Config.Healers[healerIndex];
                    }
                    else
                    {
                        // Invalid position - show the position string itself
                        healerName = PositionHelper.GetInvalidPositionName(positionStr);
                    }
                }
                else
                {
                    // Invalid format - show the position string itself
                    healerName = PositionHelper.GetInvalidPositionName(positionStr);
                }

                // --- Out-of-sync macro adjustment logic ---
                DateTime macroTime = DateTime.Now;
                DateTime? presumedStart = null;
                // Try to parse macroTime from log line timestamp
                int tsEnd = line.IndexOf(']');
                if (line.StartsWith("[") && tsEnd > 0)
                {
                    string ts = line.Substring(1, tsEnd - 1);
                    if (DateTime.TryParse(ts, out var dt))
                        macroTime = dt;
                }
                // Only adjust for healers in the chain
                if (rotationManager.Config.Healers.Contains(healerName))
                {
                    // Look back for "<HealerName> begins to cast a spell" or "You begin casting" if current player, only before macroTime
                    var castLine = recentLogLines
                        .Where(x => x.timestamp < macroTime)
                        .LastOrDefault(x =>
                            x.line.Contains($"{healerName} begins to cast a spell") ||
                            (rotationManager.Config.PlayerName.Trim().Equals(healerName.Trim(), StringComparison.OrdinalIgnoreCase) && x.line.Contains("You begin casting", StringComparison.OrdinalIgnoreCase)));

                    if (castLine.timestamp != default)
                    {
                        // Only adjust if macro is after presumed start and castLine is within 4 seconds
                        var nextFullSecond = castLine.timestamp.AddSeconds(1);
                        presumedStart = new DateTime(nextFullSecond.Year, nextFullSecond.Month, nextFullSecond.Day, nextFullSecond.Hour, nextFullSecond.Minute, nextFullSecond.Second, 0);
                        var lookbackSeconds = (macroTime - castLine.timestamp).TotalSeconds;
                        if (macroTime > presumedStart && lookbackSeconds <= 4.0)
                        {
                            DebugLog($"Adjusting macro time from {macroTime:HH:mm:ss.fff} to presumed start {presumedStart:HH:mm:ss.fff}");
                            macroTime = (DateTime)presumedStart;
                        }
                    }
                    else
                    {
                    }
                }

                // NPC detection: if targetName contains a space, treat as NPC
                bool isNpcTarget = !string.IsNullOrWhiteSpace(targetName) && targetName.Contains(' ');
                var config = rotationManager.Config.AudioAlerts;
                if (isNpcTarget && config != null && config.AlertOnNpcCompleteHeal)
                {
                    // Use TTS to say "Bad target"
                    try
                    {
                        System.Speech.Synthesis.SpeechSynthesizer synth = new System.Speech.Synthesis.SpeechSynthesizer();
                        synth.SpeakAsync("Bad target");
                    }
                    catch { /* Ignore TTS errors */ }
                }

                // Pass adjusted macroTime to OnHealCast (add overload if needed)
                // Only use adjusted macroTime if it was changed (not default)
                if (presumedStart.HasValue && macroTime == presumedStart.Value)
                {
                    rotationManager.OnHealCast(healerName, targetName, macroTime);
                }
                else
                {
                    rotationManager.OnHealCast(healerName, targetName);
                }
            }
        }
    }
}
