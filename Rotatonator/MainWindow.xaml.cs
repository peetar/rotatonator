using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace Rotatonator
{
    public partial class MainWindow : Window
    {

        private OverlayWindow? overlayWindow;
        private OverlayAnchor? overlayAnchor;
        private DDRGraphicalOverlay? ddrGraphicalOverlay;
        private LogMonitor? logMonitor;
        private RotationManager? rotationManager;
        private Point overlayPosition = new Point(100, 100);
        private AudioAlertConfig audioAlertConfig = new AudioAlertConfig();
        private int currentChainInterval = 6;
        private System.Windows.Threading.DispatcherTimer? autoDetectTimer;
        private readonly CloudSyncService cloudSyncService = new CloudSyncService();
        private bool isInitializing = true;

        private void UpdateLogMonitorUI(bool isMonitoring)
        {
            LogMonitorIndicator.Visibility = isMonitoring ? Visibility.Visible : Visibility.Collapsed;
            UpdateLogFileSize();
        }

        private void UpdateLogFileSize()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(LogFilePathTextBox.Text) && File.Exists(LogFilePathTextBox.Text))
                {
                    var fileInfo = new FileInfo(LogFilePathTextBox.Text);
                    double sizeMB = fileInfo.Length / (1024.0 * 1024.0);
                    LogFileSizeTextBlock.Text = $"{sizeMB:F2} MB";
                }
                else
                {
                    LogFileSizeTextBlock.Text = "";
                }
            }
            catch { LogFileSizeTextBlock.Text = ""; }
        }

        private void ArchiveLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(LogFilePathTextBox.Text) || !File.Exists(LogFilePathTextBox.Text))
                {
                    MessageBox.Show("No valid log file selected.", "Archive Log", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                string logPath = LogFilePathTextBox.Text;
                string dir = Path.GetDirectoryName(logPath) ?? "";
                string baseName = Path.GetFileNameWithoutExtension(logPath);
                string ext = Path.GetExtension(logPath);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string archiveName = $"{baseName}_archive_{timestamp}{ext}";
                string archivePath = Path.Combine(dir, archiveName);
                File.Copy(logPath, archivePath, true);
                // Truncate the original log
                using (var fs = new FileStream(logPath, FileMode.Truncate)) { }
                MessageBox.Show($"Log archived as:\n{archiveName}\nLog file truncated.", "Archive Log", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateLogFileSize();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to archive log: {ex.Message}", "Archive Log", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            // Load saved settings
            LoadSavedSettings();

            // Check if there is a more recently modified log file in the directory
            CheckForActiveCharacterChange();

            // Update log file size display on startup
            UpdateLogFileSize();

            // Listen for log file path changes to update size
            LogFilePathTextBox.TextChanged += (s, e) => UpdateLogFileSize();

            // Show anchor by default when overlay checkbox is checked (after main window is loaded)
            Loaded += (s, e) => ShowHideAnchor();

            // Start auto-detect timer to monitor directory for character switches (runs every 3 seconds)
            autoDetectTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            autoDetectTimer.Tick += (s, e) => CheckForActiveCharacterChange();
            autoDetectTimer.Start();

            // Wire up cloud sync service events
            cloudSyncService.ChainUpdatedFromCloud += OnChainUpdatedFromCloud;
            cloudSyncService.SyncStatusChanged += OnCloudSyncStatusChanged;
            ChainPrefixTextBox.TextChanged += (s, e) => { cloudSyncService.CurrentPrefix = ChainPrefixTextBox.Text.Trim(); };

            isInitializing = false;
        }

        private void LoadSavedSettings()
        {
            var settings = SettingsManager.LoadSettings();
            
            // Load log file path
            if (!string.IsNullOrEmpty(settings.LogFilePath) && File.Exists(settings.LogFilePath))
            {
                LogFilePathTextBox.Text = settings.LogFilePath;
            }
            else
            {
                // Try to find EQ log directory if no saved path
                string? defaultLogDir = FindEQLogDirectoryPath();
                if (!string.IsNullOrEmpty(defaultLogDir))
                {
                    string? latest = GetLatestLogFileInDirectory(defaultLogDir);
                    if (!string.IsNullOrEmpty(latest))
                    {
                        LogFilePathTextBox.Text = latest;
                    }
                }
            }
            
            // Auto-detect character name from log filename
            if (!string.IsNullOrEmpty(LogFilePathTextBox.Text))
            {
                string? detectedName = ExtractCharacterNameFromLogPath(LogFilePathTextBox.Text);
                if (!string.IsNullOrEmpty(detectedName))
                {
                    PlayerNameTextBox.Text = detectedName;
                }
                else if (!string.IsNullOrEmpty(settings.PlayerName))
                {
                    PlayerNameTextBox.Text = settings.PlayerName;
                }
            }
            else if (!string.IsNullOrEmpty(settings.PlayerName))
            {
                PlayerNameTextBox.Text = settings.PlayerName;
            }
            
            // Load other settings
            if (!string.IsNullOrEmpty(settings.ChainHealers))
                ChainHealersTextBox.Text = settings.ChainHealers;
            
            ChainPrefixTextBox.Text = settings.ChainPrefix;
            currentChainInterval = (int)settings.ChainInterval;
            UpdateChainIntervalDisplay();
            VisualAlertsCheckBox.IsChecked = settings.EnableVisualAlerts;
            AudioBeepCheckBox.IsChecked = settings.EnableAudioBeep;
            DDRModeCheckBox.IsChecked = settings.EnableDDRMode;
            DDRSillyModeCheckBox.IsChecked = settings.EnableDDRSillyMode;

            // Load cloud sync and chain updated sound settings
            CloudSyncCheckBox.IsChecked = settings.EnableCloudSync;
            cloudSyncService.IsEnabled = settings.EnableCloudSync;
            if (!string.IsNullOrEmpty(settings.CloudSyncUrl) && !settings.CloudSyncUrl.StartsWith("https://rotatonator.vercel.app", StringComparison.OrdinalIgnoreCase))
            {
                CloudSyncUrlTextBox.Text = settings.CloudSyncUrl;
                cloudSyncService.BaseUrl = settings.CloudSyncUrl;
            }
            else
            {
                CloudSyncUrlTextBox.Text = "https://rotatonator-web.vercel.app/";
                cloudSyncService.BaseUrl = "https://rotatonator-web.vercel.app/";
            }
            PlaySoundOnChainUpdateCheckBox.IsChecked = settings.PlaySoundOnChainUpdate;
            SoundService.PlaySoundOnChainUpdate = settings.PlaySoundOnChainUpdate;
            cloudSyncService.CurrentPrefix = settings.ChainPrefix;
            if (settings.EnableCloudSync)
            {
                cloudSyncService.StartPolling();
            }
            
            // Load audio alert config
            if (settings.AudioAlerts != null)
            {
                audioAlertConfig = settings.AudioAlerts;
            }
            
            // Sync the old audio beep checkbox with the new config location
            if (settings.AudioAlerts != null)
            {
                AudioBeepCheckBox.IsChecked = settings.AudioAlerts.EnableAudioBeep;
            }
            else if (settings.EnableAudioBeep)
            {
                // Migrate old setting to new location
                audioAlertConfig.EnableAudioBeep = settings.EnableAudioBeep;
                AudioBeepCheckBox.IsChecked = settings.EnableAudioBeep;
            }
        }

        private static string? ExtractCharacterNameFromLogPath(string logPath)
        {
            if (string.IsNullOrWhiteSpace(logPath))
                return null;

            if (!logPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                return null;

            string filename = Path.GetFileNameWithoutExtension(logPath);
            if (!filename.StartsWith("eqlog_", StringComparison.OrdinalIgnoreCase))
                return null;

            // Exclude archive and backup files
            if (filename.Contains("_archive_", StringComparison.OrdinalIgnoreCase) ||
                filename.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                return null;

            string withoutPrefix = filename.Substring(6);
            string[] parts = withoutPrefix.Split('_');
            if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
            {
                return parts[0];
            }

            return null;
        }

        private static string? FindEQLogDirectoryPath()
        {
            string[] possiblePaths = new[]
            {
                @"C:\EverQuest\Logs",
                @"C:\Program Files (x86)\Sony\EverQuest\Logs",
                @"C:\Program Files\Sony\EverQuest\Logs",
                @"C:\Games\EverQuest\Logs",
                @"C:\Sony\EverQuest\Logs",
                @"D:\EverQuest\Logs",
                @"D:\Games\EverQuest\Logs"
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private string? GetCurrentLogDirectory()
        {
            string currentText = LogFilePathTextBox.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(currentText))
            {
                string? dir = Path.GetDirectoryName(currentText);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    return dir;
            }

            var settings = SettingsManager.LoadSettings();
            if (!string.IsNullOrEmpty(settings.LogFilePath))
            {
                string? dir = Path.GetDirectoryName(settings.LogFilePath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    return dir;
            }

            return FindEQLogDirectoryPath();
        }

        private static string? GetLatestLogFileInDirectory(string directoryPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
                    return null;

                var dirInfo = new DirectoryInfo(directoryPath);
                var files = dirInfo.GetFiles("eqlog_*.txt");

                var latestFile = files
                    .Where(f => !f.Name.Contains("_archive_", StringComparison.OrdinalIgnoreCase) &&
                                !f.Name.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) &&
                                !string.IsNullOrEmpty(ExtractCharacterNameFromLogPath(f.FullName)))
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .FirstOrDefault();

                return latestFile?.FullName;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutoDetect] Error finding latest log: {ex.Message}");
                return null;
            }
        }

        private void CheckForActiveCharacterChange()
        {
            string? logDir = GetCurrentLogDirectory();
            if (string.IsNullOrEmpty(logDir))
                return;

            string? latestLogFile = GetLatestLogFileInDirectory(logDir);
            if (string.IsNullOrEmpty(latestLogFile))
                return;

            string currentLogFile = LogFilePathTextBox.Text?.Trim() ?? "";

            if (!string.Equals(latestLogFile, currentLogFile, StringComparison.OrdinalIgnoreCase))
            {
                SwitchActiveCharacter(latestLogFile);
            }
        }

        private void SwitchActiveCharacter(string newLogPath)
        {
            string? newCharName = ExtractCharacterNameFromLogPath(newLogPath);
            if (string.IsNullOrWhiteSpace(newCharName))
                return;

            System.Diagnostics.Debug.WriteLine($"[AutoDetect] Switching active character to {newCharName} ({newLogPath})");

            LogFilePathTextBox.Text = newLogPath;
            PlayerNameTextBox.Text = newCharName;
            UpdateLogFileSize();

            if (logMonitor != null && rotationManager != null)
            {
                // Stop previous log monitoring
                logMonitor.Stop();

                // Update rotation manager player name and reset any active turn alerts
                rotationManager.Config.PlayerName = newCharName;
                rotationManager.ResetPlayerTurn();

                // Start new log monitor for the new character's log file
                logMonitor = new LogMonitor(newLogPath, rotationManager);
                logMonitor.Start();

                // Reset overlay alerts and update overlay title/position
                overlayWindow?.ResetAlerts();
                overlayWindow?.UpdateChainInfo();

                // Save updated settings
                SaveCurrentSettings();

                int playerPos = rotationManager.GetPlayerPosition();
                if (playerPos >= 0)
                {
                    StatusTextBlock.Text = $"Active character changed to {newCharName}. Monitoring active (Position: {playerPos + 1} of {rotationManager.Config.Healers.Count}).";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.LimeGreen;
                }
                else
                {
                    StatusTextBlock.Text = $"Active character changed to {newCharName}. Monitoring active (Not in healer chain).";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.LightSkyBlue;
                }
            }
            else
            {
                SaveCurrentSettings();
                StatusTextBlock.Text = $"Active character detected: {newCharName}. Ready to start.";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.LimeGreen;
            }
        }

        private void BrowseLogButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "EverQuest Log Files (eqlog_*.txt)|eqlog_*.txt|All Files (*.*)|*.*",
                Title = "Select EverQuest Log File"
            };

            if (dialog.ShowDialog() == true)
            {
                SwitchActiveCharacter(dialog.FileName);
            }
        }

        private void UpdateChainIntervalDisplay()
        {
            if (ChainIntervalLabel != null)
            {
                ChainIntervalLabel.Text = $"{currentChainInterval}s";
            }
        }

        private void DecreaseDelayButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentChainInterval > 1)
            {
                currentChainInterval--;
                UpdateChainIntervalDisplay();
                ExportDelayOnly();
            }
        }

        private void IncreaseDelayButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentChainInterval < 10)
            {
                currentChainInterval++;
                UpdateChainIntervalDisplay();
                ExportDelayOnly();
            }
        }

        private void ExportDelayOnly()

        {
            try
            {
                int delay = currentChainInterval;
                string exportText = $"/rs Rotatonator set_delay: {delay}";
                Clipboard.SetText(exportText);
            }
            catch
            {
            }
        }

        private void ExportChainButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var healers = ChainHealersTextBox.Text
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(h => h.Trim())
                    .Where(h => !string.IsNullOrWhiteSpace(h))
                    .ToList();

                if (healers.Count == 0)
                {
                    MessageBox.Show("No healers to export.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Build export string: /rs Rotatonator set_chain: 111 Name1, 222 Name2, 333 Name3, set_delay: X
                var chainParts = new System.Text.StringBuilder();
                for (int i = 0; i < healers.Count; i++)
                {
                    if (i > 0) chainParts.Append(", ");
                    string position = PositionHelper.PositionToString(i + 1); // 111, 222, 333, ... AAA, BBB, etc.
                    chainParts.Append($"{position} {healers[i]}");
                }

                int delay = (int)currentChainInterval;
                string exportText = $"/rs Rotatonator set_chain: {chainParts}, set_delay: {delay}";

                Clipboard.SetText(exportText);

                // If Cloud Sync is enabled, push chain to the web API
                if (CloudSyncCheckBox.IsChecked == true)
                {
                    string prefix = ChainPrefixTextBox.Text.Trim();
                    string senderName = PlayerNameTextBox.Text.Trim();
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        await cloudSyncService.PushChainAsync(prefix, healers, delay, senderName);
                    });
                }

                MessageBox.Show($"Chain configuration copied to clipboard!\n\n{exportText}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting chain: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportCHStringButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var playerName = PlayerNameTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(playerName))
                {
                    MessageBox.Show("Please enter your character name.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var healers = ChainHealersTextBox.Text
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(h => h.Trim())
                    .Where(h => !string.IsNullOrWhiteSpace(h))
                    .ToList();

                if (healers.Count == 0)
                {
                    MessageBox.Show("No healers in the chain.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Find player's position in the chain (1-based)
                int playerPosition = -1;
                for (int i = 0; i < healers.Count; i++)
                {
                    if (healers[i].Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    {
                        playerPosition = i + 1;
                        break;
                    }
                }

                if (playerPosition == -1)
                {
                    MessageBox.Show($"Your character '{playerName}' is not in the healer list.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var prefix = ChainPrefixTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    MessageBox.Show("Please enter a chain prefix.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Build standardized 3-character position string: 111, 222, 333, ... AAA, BBB
                string positionString = PositionHelper.PositionToString(playerPosition);

                // Build CH macro string
                string chString = $"/rs {prefix} {positionString} CH - %t - %n";

                Clipboard.SetText(chString);
                MessageBox.Show($"CH macro string copied to clipboard!\n\n{chString}\n\nAdd this to your Complete Heal macro.", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting CH string: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportAppendMacroButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var playerName = PlayerNameTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(playerName))
                {
                    MessageBox.Show("Please enter your character name.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var healers = ChainHealersTextBox.Text
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(h => h.Trim())
                    .Where(h => !string.IsNullOrWhiteSpace(h))
                    .ToList();

                if (healers.Count == 0)
                {
                    MessageBox.Show("No healers in the chain.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int playerPosition = -1;
                for (int i = 0; i < healers.Count; i++)
                {
                    if (healers[i].Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    {
                        playerPosition = i + 1;
                        break;
                    }
                }

                if (playerPosition == -1)
                {
                    MessageBox.Show($"Your character '{playerName}' is not in the healer list.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string appendMacro = $"rotat:{playerPosition}, %t";
                Clipboard.SetText(appendMacro);
                MessageBox.Show($"Append macro copied to clipboard!\n\n{appendMacro}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting append macro: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigAudioAlertsButton_Click(object sender, RoutedEventArgs e)
        {
            HideDDROverlayForDialog();
            
            var dialog = new AudioAlertConfigDialog(audioAlertConfig);
            if (dialog.ShowDialog() == true)
            {
                audioAlertConfig = dialog.Config;
                
                // Update rotation manager if it's running
                if (rotationManager != null)
                {
                    rotationManager.Config.AudioAlerts = audioAlertConfig;
                }
                
                SaveCurrentSettings();
            }
            
            RestoreDDROverlayAfterDialog();
        }

        private void DDRModeCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // When DDR mode is enabled, hide classic overlay and show DDR overlay
            // When DDR mode is disabled, show classic overlay and hide DDR overlay
            
            bool enableDDR = DDRModeCheckBox.IsChecked == true;
            
            if (enableDDR && rotationManager != null)
            {
                // Hide classic overlay
                if (overlayWindow != null)
                {
                    overlayWindow.Visibility = Visibility.Collapsed;
                }
                
                // Show DDR overlay
                if (ddrGraphicalOverlay == null)
                {
                    var ddrAudio = overlayWindow?.GetDDRAudioService();
                    var scoreTracker = overlayWindow?.GetDDRScoreTracker();
                    ddrGraphicalOverlay = new DDRGraphicalOverlay(rotationManager, ddrAudio, scoreTracker);
                    ddrGraphicalOverlay.Show();
                }
                else
                {
                    ddrGraphicalOverlay.Visibility = Visibility.Visible;
                }
            }
            else
            {
                // Hide DDR overlay
                if (ddrGraphicalOverlay != null)
                {
                    ddrGraphicalOverlay.Visibility = Visibility.Collapsed;
                }
                
                // Show classic overlay if monitoring is active and not in DDR mode
                if (overlayWindow != null && DDRModeCheckBox.IsChecked != true)
                {
                    overlayWindow.Visibility = Visibility.Visible;
                }
            }
        }

        private void ShowHideAnchor()
        {
            bool isMonitoring = logMonitor != null;
            
            // Show anchor when monitoring hasn't started yet
            if (!isMonitoring)
            {
                if (overlayAnchor == null)
                {
                    overlayAnchor = new OverlayAnchor();
                    overlayAnchor.SetPosition(overlayPosition);
                    overlayAnchor.Closed += (s, e) =>
                    {
                        if (overlayAnchor != null)
                        {
                            overlayPosition = overlayAnchor.GetPosition();
                        }
                    };
                }
                overlayAnchor.Show();
            }
            else
            {
                if (overlayAnchor != null)
                {
                    overlayPosition = overlayAnchor.GetPosition();
                    overlayAnchor.Close();
                    overlayAnchor = null;
                }
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateLogMonitorUI(true);
            // Check if we're refreshing settings vs starting fresh
            bool isRefresh = rotationManager != null && logMonitor != null;

            // Validate inputs
            if (string.IsNullOrWhiteSpace(LogFilePathTextBox.Text) || !File.Exists(LogFilePathTextBox.Text))
            {
                MessageBox.Show("Please select a valid EverQuest log file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(ChainHealersTextBox.Text))
            {
                MessageBox.Show("Please enter healer names for the rotation chain.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Parse healers
            var healers = ChainHealersTextBox.Text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(h => h.Trim())
                .Where(h => !string.IsNullOrWhiteSpace(h))
                .ToList();

            if (healers.Count < 2)
            {
                MessageBox.Show("Please enter at least 2 healers in the rotation.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string playerName = PlayerNameTextBox.Text.Trim();
            // Player name is optional - tanks/raid leaders can monitor without being in chain

            try
            {
                var config = new RotationConfig
                {
                    Healers = healers,
                    PlayerName = playerName,
                    ChainPrefix = ChainPrefixTextBox.Text.Trim(),
                    ChainInterval = TimeSpan.FromSeconds(currentChainInterval),
                    EnableVisualAlerts = VisualAlertsCheckBox.IsChecked ?? false,
                    EnableAudioBeep = AudioBeepCheckBox.IsChecked ?? false,
                    EnableAutoCast = AutoCastCheckBox.IsChecked ?? false,
                    CastHotkey = HotkeyTextBox.Text,
                    AudioAlerts = audioAlertConfig,
                    EnableDDRMode = DDRModeCheckBox.IsChecked ?? false,
                    EnableDDRSillyMode = DDRSillyModeCheckBox.IsChecked ?? false
                };

                if (isRefresh)
                {
                    // Update existing rotation manager config (we know it's not null because isRefresh checks this)
                    rotationManager!.Config.Healers = config.Healers;
                    rotationManager.Config.PlayerName = config.PlayerName;
                    rotationManager.Config.ChainPrefix = config.ChainPrefix;
                    rotationManager.Config.ChainInterval = config.ChainInterval;
                    rotationManager.Config.EnableVisualAlerts = config.EnableVisualAlerts;
                    rotationManager.Config.EnableAudioBeep = config.EnableAudioBeep;
                    rotationManager.Config.EnableAutoCast = config.EnableAutoCast;
                    rotationManager.Config.CastHotkey = config.CastHotkey;
                    rotationManager.Config.AudioAlerts = audioAlertConfig;
                    rotationManager.Config.EnableDDRMode = DDRModeCheckBox.IsChecked ?? false;
                    rotationManager.Config.EnableDDRSillyMode = DDRSillyModeCheckBox.IsChecked ?? false;

                    // Save settings
                    SaveCurrentSettings();

                    // Update overlay if it exists
                    overlayWindow?.UpdateChainInfo();

                    StatusTextBlock.Text = $"Settings refreshed. Position in chain: {rotationManager.GetPlayerPosition() + 1} of {healers.Count}";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.LimeGreen;
                    return;
                }

                // Create rotation manager (first time start)
                rotationManager = new RotationManager(config);

                // Subscribe to chain import events
                rotationManager.ChainImported += OnChainImported;

                // Hide anchor and save position
                if (overlayAnchor != null)
                {
                    overlayPosition = overlayAnchor.GetPosition();
                    overlayAnchor.Close();
                    overlayAnchor = null;
                }

                // Always create classic overlay (needed for DDR audio/score services)
                overlayWindow = new OverlayWindow(rotationManager);
                overlayWindow.Left = overlayPosition.X;
                overlayWindow.Top = overlayPosition.Y;
                
                // Show classic overlay only if DDR mode is OFF
                if (DDRModeCheckBox.IsChecked != true)
                {
                    overlayWindow.Show();
                }
                else if (DDRModeCheckBox.IsChecked == true)
                {
                    // Don't show classic overlay when DDR mode is enabled
                    // But we still need to create it for the audio/score services
                    overlayWindow.Visibility = Visibility.Hidden;
                }

                // Create DDR graphical overlay if enabled
                if (DDRModeCheckBox.IsChecked == true)
                {
                    var ddrAudio = overlayWindow?.GetDDRAudioService();
                    var scoreTracker = overlayWindow?.GetDDRScoreTracker();
                    ddrGraphicalOverlay = new DDRGraphicalOverlay(rotationManager, ddrAudio, scoreTracker);
                    ddrGraphicalOverlay.Show();
                }

                // Start log monitoring
                logMonitor = new LogMonitor(LogFilePathTextBox.Text, rotationManager);
                logMonitor.Start();

                // Save settings
                SaveCurrentSettings();

                StartButton.Content = "Refresh Settings";
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = true;
                StatusTextBlock.Text = $"Monitoring active. Position in chain: {rotationManager.GetPlayerPosition() + 1} of {healers.Count}";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.LimeGreen;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting monitoring: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopMonitoring();
        }

        private void StopMonitoring()
        {
            UpdateLogMonitorUI(false);
            UpdateLogFileSize();
            logMonitor?.Stop();
            logMonitor = null;

            if (overlayWindow != null)
            {
                overlayPosition = new Point(overlayWindow.Left, overlayWindow.Top);
                overlayWindow.Close();
                overlayWindow = null;
            }

            if (ddrGraphicalOverlay != null)
            {
                // Stop all audio before closing
                ddrGraphicalOverlay.StopAllAudio();
                ddrGraphicalOverlay.Close();
                ddrGraphicalOverlay = null;
            }

            rotationManager = null;

            StartButton.Content = "Start Monitoring";
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            StatusTextBlock.Text = "Monitoring stopped.";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
            
            // Show anchor again if overlay is enabled
            ShowHideAnchor();
        }

        protected override void OnClosed(EventArgs e)
        {
            autoDetectTimer?.Stop();
            cloudSyncService.Dispose();
            StopMonitoring();
            overlayAnchor?.Close();
            base.OnClosed(e);
        }

        private void OnChainImported(object? sender, ChainImportEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // Update UI with imported chain
                ChainHealersTextBox.Text = string.Join(Environment.NewLine, e.Healers);
                currentChainInterval = e.Delay;
                UpdateChainIntervalDisplay();
                
                // Save imported settings
                SaveCurrentSettings();

                // Play chain updated sound notification
                SoundService.PlayChainUpdatedSound();
                
                StatusTextBlock.Text = $"Chain imported! {e.Healers.Count} healers, {e.Delay}s interval. Monitoring restarted.";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.LimeGreen;
                
                // Update overlay if it exists
                overlayWindow?.UpdateChainInfo();
            });
        }

        private void OnChainUpdatedFromCloud(object? sender, ChainImportEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // Update UI with imported chain
                ChainHealersTextBox.Text = string.Join(Environment.NewLine, e.Healers);
                currentChainInterval = e.Delay;
                UpdateChainIntervalDisplay();

                // Update active rotation manager if running
                if (rotationManager != null)
                {
                    rotationManager.Config.Healers = e.Healers;
                    rotationManager.Config.ChainInterval = TimeSpan.FromSeconds(e.Delay);
                }
                
                // Save imported settings
                SaveCurrentSettings();

                // Play chain updated sound notification
                SoundService.PlayChainUpdatedSound();
                
                StatusTextBlock.Text = $"Cloud chain update received! {e.Healers.Count} healers, {e.Delay}s interval.";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.DeepSkyBlue;
                
                // Update overlay if it exists
                overlayWindow?.UpdateChainInfo();
            });
        }

        private void OnCloudSyncStatusChanged(object? sender, string status)
        {
            Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"[CloudSync] {status}");
            });
        }

        private void CloudSyncCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (isInitializing) return;

            bool isChecked = CloudSyncCheckBox.IsChecked == true;
            cloudSyncService.IsEnabled = isChecked;
            cloudSyncService.CurrentPrefix = ChainPrefixTextBox?.Text?.Trim() ?? "D&D";
            cloudSyncService.BaseUrl = CloudSyncUrlTextBox?.Text?.Trim() ?? "https://rotatonator-web.vercel.app/";
            if (isChecked)
            {
                cloudSyncService.StartPolling();
            }
            else
            {
                cloudSyncService.StopPolling();
            }
            SaveCurrentSettings();
        }

        private void PlaySoundOnChainUpdateCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (isInitializing) return;

            SoundService.PlaySoundOnChainUpdate = PlaySoundOnChainUpdateCheckBox.IsChecked == true;
            SaveCurrentSettings();
        }

        private void CloudSyncUrlTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (isInitializing) return;

            if (cloudSyncService != null && CloudSyncUrlTextBox != null)
            {
                cloudSyncService.BaseUrl = CloudSyncUrlTextBox.Text.Trim();
            }
        }

        private void HideDDROverlayForDialog()
        {
            if (ddrGraphicalOverlay != null && ddrGraphicalOverlay.Visibility == Visibility.Visible)
            {
                ddrGraphicalOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void RestoreDDROverlayAfterDialog()
        {
            if (ddrGraphicalOverlay != null && DDRModeCheckBox.IsChecked == true)
            {
                ddrGraphicalOverlay.Visibility = Visibility.Visible;
            }
        }
        
        private MessageBoxResult ShowMessageBox(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            HideDDROverlayForDialog();
            var result = MessageBox.Show(messageBoxText, caption, button, icon);
            RestoreDDROverlayAfterDialog();
            return result;
        }

        private void SaveCurrentSettings()
        {
            if (isInitializing) return;

            var settings = new AppSettings
            {
                LogFilePath = LogFilePathTextBox?.Text ?? "",
                PlayerName = PlayerNameTextBox?.Text ?? "",
                ChainHealers = ChainHealersTextBox?.Text ?? "",
                ChainPrefix = ChainPrefixTextBox?.Text ?? "D&D",
                ChainInterval = currentChainInterval,
                ShowOverlay = true,
                EnableVisualAlerts = VisualAlertsCheckBox?.IsChecked ?? true,
                EnableAudioBeep = AudioBeepCheckBox?.IsChecked ?? false,
                AudioAlerts = audioAlertConfig,
                EnableDDRMode = DDRModeCheckBox?.IsChecked ?? false,
                EnableDDRSillyMode = DDRSillyModeCheckBox?.IsChecked ?? false,
                EnableCloudSync = CloudSyncCheckBox?.IsChecked ?? false,
                CloudSyncUrl = CloudSyncUrlTextBox?.Text?.Trim() ?? "https://rotatonator-web.vercel.app/",
                PlaySoundOnChainUpdate = PlaySoundOnChainUpdateCheckBox?.IsChecked ?? true
            };
            
            SettingsManager.SaveSettings(settings);
        }

        private void MuteAudioButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle mute for both DDR overlay and audio service
            if (ddrGraphicalOverlay != null)
            {
                ddrGraphicalOverlay.ToggleMute();
            }
            
            // Also toggle mute for the overlay window's audio service
            if (overlayWindow != null)
            {
                var ddrAudio = overlayWindow.GetDDRAudioService();
                if (ddrAudio != null)
                {
                    ddrAudio.IsMuted = !ddrAudio.IsMuted;
                }
            }
            
            // Update button text
            bool isMuted = ddrGraphicalOverlay?.IsMuted ?? false;
            MuteAudioButton.Content = isMuted ? "🔇 Unmute Audio" : "🔊 Mute Audio";
        }
        
        private void ExportDDRScoresButton_Click(object sender, RoutedEventArgs e)
        {
            if (ddrGraphicalOverlay == null)
            {
                ShowMessageBox("DDR mode is not active.", "Export Scores", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            string scores = ddrGraphicalOverlay.ExportScores();
            if (string.IsNullOrEmpty(scores))
            {
                ShowMessageBox("No scores to export yet.", "Export Scores", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            try
            {
                Clipboard.SetText(scores);
            }
            catch (Exception ex)
            {
                ShowMessageBox($"Failed to copy to clipboard: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ResetDDRScoresButton_Click(object sender, RoutedEventArgs e)
        {
            if (ddrGraphicalOverlay == null)
                return;
            
            ddrGraphicalOverlay.ResetScores();
        }
    }
}
