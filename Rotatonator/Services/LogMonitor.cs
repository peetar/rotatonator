using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rotatonator
{
    public class LogMonitor
    {
        private readonly string logFilePath;
        private readonly RotationManager rotationManager;
        private FileSystemWatcher? fileWatcher;
        private StreamReader? logReader;
        private long lastPosition = 0;
        private Regex? chainMessageRegex;
        private Regex? chainImportRegex;

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
            // Build regex pattern for detecting CH rotation messages
            // Pattern: [timestamp] <any text>, 'PREFIX (\d)\1* CH'
            // Matches: says, tells the raid, shouts, tells the guild, auctions, etc.
            // Example: "D&D 333 CH" where 3 repeated 3 times = position 3
            string escapedPrefix = Regex.Escape(rotationManager.Config.ChainPrefix);
            string pattern = $@"^\[.*?\]\s+.+?,\s+'" + escapedPrefix + @"\s+(\d)\1*\s+CH";
            chainMessageRegex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            
            // Build regex for detecting chain import messages
            // Pattern: Rotatonator set_chain: 111 Name1, 222 Name2, set_delay: 3
            chainImportRegex = new Regex(
                @"Rotatonator\s+set_chain:\s*(.+?)\s*,\s*set_delay:\s*(\d+)",
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
                string? line;
                while ((line = logReader.ReadLine()) != null)
                {
                    ProcessLogLine(line);
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
                    string chainData = importMatch.Groups[1].Value;
                    string delayStr = importMatch.Groups[2].Value;
                    
                    if (int.TryParse(delayStr, out int delay))
                    {
                        rotationManager.OnChainImport(chainData, delay);
                        return;
                    }
                }
            }
            
            if (chainMessageRegex == null) return;
            
            // Check for CH rotation message (e.g., "D&D 333 CH")
            var match = chainMessageRegex.Match(line);
            if (match.Success)
            {
                // Extract the repeated digit to determine position
                string digitStr = match.Groups[1].Value;
                if (int.TryParse(digitStr, out int position))
                {
                    // Position is 1-indexed, but list is 0-indexed
                    int healerIndex = position - 1;
                    
                    if (healerIndex >= 0 && healerIndex < rotationManager.Config.Healers.Count)
                    {
                        string healerName = rotationManager.Config.Healers[healerIndex];
                        rotationManager.OnHealCast(healerName);
                    }
                }
            }
        }
    }
}
