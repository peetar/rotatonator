using System;
using System.IO;
using System.Text.Json;

namespace Rotatonator
{
    public class AppSettings
    {
        public string LogFilePath { get; set; } = "";
        public string PlayerName { get; set; } = "";
        public string ChainHealers { get; set; } = "";
        public string ChainPrefix { get; set; } = "D&D";
        public double ChainInterval { get; set; } = 6.0;
        public bool ShowOverlay { get; set; } = true;
        public bool EnableVisualAlerts { get; set; } = true;
        public bool EnableAudioBeep { get; set; } = false;
        public AudioAlertConfig? AudioAlerts { get; set; }
        public bool EnableDDRMode { get; set; } = false;
    }

    public static class SettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Rotatonator",
            "settings.json"
        );

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(SettingsPath, json);
                Console.WriteLine($"[Settings] Saved to {SettingsPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Settings] Error saving settings: {ex.Message}");
            }
        }

        public static AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    Console.WriteLine($"[Settings] Loaded from {SettingsPath}");
                    return settings ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Settings] Error loading settings: {ex.Message}");
            }

            return new AppSettings();
        }
    }
}
