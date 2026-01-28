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

        public MainWindow()
        {
            InitializeComponent();
            
            // Load saved settings
            LoadSavedSettings();
            
            // Show anchor by default when overlay checkbox is checked
            ShowHideAnchor();
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
                string defaultLogPath = FindEQLogDirectory();
                if (!string.IsNullOrEmpty(defaultLogPath))
                {
                    LogFilePathTextBox.Text = defaultLogPath;
                }
            }
            
            // Auto-detect character name from log filename if not saved
            if (!string.IsNullOrEmpty(LogFilePathTextBox.Text))
            {
                string filename = Path.GetFileNameWithoutExtension(LogFilePathTextBox.Text);
                if (filename.StartsWith("eqlog_", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = filename.Substring(6).Split('_');
                    if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                    {
                        PlayerNameTextBox.Text = string.IsNullOrEmpty(settings.PlayerName) ? parts[0] : settings.PlayerName;
                    }
                }
            }
            
            // Load other settings
            if (!string.IsNullOrEmpty(settings.PlayerName))
                PlayerNameTextBox.Text = settings.PlayerName;
            
            if (!string.IsNullOrEmpty(settings.ChainHealers))
                ChainHealersTextBox.Text = settings.ChainHealers;
            
            ChainPrefixTextBox.Text = settings.ChainPrefix;
            currentChainInterval = (int)settings.ChainInterval;
            UpdateChainIntervalDisplay();
            ShowOverlayCheckBox.IsChecked = settings.ShowOverlay;
            VisualAlertsCheckBox.IsChecked = settings.EnableVisualAlerts;
            AudioBeepCheckBox.IsChecked = settings.EnableAudioBeep;
            DDRModeCheckBox.IsChecked = settings.EnableDDRMode;
            
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

        private string FindEQLogDirectory()
        {
            // Common EQ installation paths
            string[] possiblePaths = new[]
            {
                @"C:\Program Files (x86)\Sony\EverQuest\Logs",
                @"C:\Program Files\Sony\EverQuest\Logs",
                @"C:\EverQuest\Logs",
                @"C:\Games\EverQuest\Logs"
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    var logFiles = Directory.GetFiles(path, "eqlog_*.txt");
                    if (logFiles.Length > 0)
                    {
                        return logFiles.OrderByDescending(f => new FileInfo(f).LastWriteTime).First();
                    }
                }
            }

            return string.Empty;
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
                LogFilePathTextBox.Text = dialog.FileName;
                
                // Auto-detect character name from log filename
                // Format: eqlog_CharacterName_ServerName.txt
                string filename = Path.GetFileNameWithoutExtension(dialog.FileName);
                if (filename.StartsWith("eqlog_", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = filename.Substring(6).Split('_');
                    if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                    {
                        PlayerNameTextBox.Text = parts[0];
                    }
                }
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
                string exportText = $"/rs Rotatonator set_delay: {currentChainInterval}";
                Clipboard.SetText(exportText);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting delay: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                // Build position string: 1, 22, 333, 4444, etc.
                string positionString = new string((char)('0' + playerPosition), playerPosition);

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
            if (DDRModeCheckBox.IsChecked == true && rotationManager != null)
            {
                if (ddrGraphicalOverlay == null)
                {
                    // Pass DDRAudioService and DDRScoreTracker from overlayWindow if available
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
            else if (ddrGraphicalOverlay != null)
            {
                ddrGraphicalOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowOverlayCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            ShowHideAnchor();
            
            if (overlayWindow != null)
            {
                overlayWindow.Visibility = ShowOverlayCheckBox.IsChecked == true 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }

        private void ShowHideAnchor()
        {
            bool shouldShowOverlay = ShowOverlayCheckBox?.IsChecked == true;
            bool isMonitoring = logMonitor != null;
            
            // Show anchor when overlay is enabled but monitoring hasn't started
            if (shouldShowOverlay && !isMonitoring)
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
                    EnableDDRMode = DDRModeCheckBox.IsChecked ?? false
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

                // Create and show overlay at saved position
                if (ShowOverlayCheckBox.IsChecked == true)
                {
                    overlayWindow = new OverlayWindow(rotationManager);
                    overlayWindow.Left = overlayPosition.X;
                    overlayWindow.Top = overlayPosition.Y;
                    overlayWindow.Show();
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
                
                StatusTextBlock.Text = $"Chain imported! {e.Healers.Count} healers, {e.Delay}s interval. Monitoring restarted.";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.LimeGreen;
                
                // Update overlay if it exists
                overlayWindow?.UpdateChainInfo();
            });
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
            var settings = new AppSettings
            {
                LogFilePath = LogFilePathTextBox.Text,
                PlayerName = PlayerNameTextBox.Text,
                ChainHealers = ChainHealersTextBox.Text,
                ChainPrefix = ChainPrefixTextBox.Text,
                ChainInterval = currentChainInterval,
                ShowOverlay = ShowOverlayCheckBox.IsChecked ?? true,
                EnableVisualAlerts = VisualAlertsCheckBox.IsChecked ?? true,
                EnableAudioBeep = AudioBeepCheckBox.IsChecked ?? false,
                AudioAlerts = audioAlertConfig,
                EnableDDRMode = DDRModeCheckBox.IsChecked ?? false
            };
            
            SettingsManager.SaveSettings(settings);
        }
    }
}
