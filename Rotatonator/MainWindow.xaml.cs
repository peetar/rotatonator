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
        private LogMonitor? logMonitor;
        private RotationManager? rotationManager;
        private Point overlayPosition = new Point(100, 100);

        public MainWindow()
        {
            InitializeComponent();
            
            // Try to find EQ log directory
            string defaultLogPath = FindEQLogDirectory();
            if (!string.IsNullOrEmpty(defaultLogPath))
            {
                LogFilePathTextBox.Text = defaultLogPath;
                
                // Auto-detect character name from log filename
                string filename = Path.GetFileNameWithoutExtension(defaultLogPath);
                if (filename.StartsWith("eqlog_", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = filename.Substring(6).Split('_');
                    if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                    {
                        PlayerNameTextBox.Text = parts[0];
                    }
                }
            }
            
            // Show anchor by default when overlay checkbox is checked
            ShowHideAnchor();
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

        private void ChainIntervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ChainIntervalLabel != null)
            {
                ChainIntervalLabel.Text = $"{e.NewValue:F1}s";
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
                    string position = new string((char)('1' + i), i + 1); // 1, 22, 333, etc.
                    chainParts.Append($"{position} {healers[i]}");
                }

                int delay = (int)ChainIntervalSlider.Value;
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

            if (string.IsNullOrWhiteSpace(PlayerNameTextBox.Text))
            {
                MessageBox.Show("Please enter your character name.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (!healers.Contains(playerName, StringComparer.OrdinalIgnoreCase))
            {
                MessageBox.Show("Your character name must be in the healer list.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Create rotation manager
                var config = new RotationConfig
                {
                    Healers = healers,
                    PlayerName = playerName,
                    ChainPrefix = ChainPrefixTextBox.Text.Trim(),
                    ChainInterval = TimeSpan.FromSeconds(ChainIntervalSlider.Value),
                    EnableVisualAlerts = VisualAlertsCheckBox.IsChecked ?? false,
                    EnableAudioBeep = AudioBeepCheckBox.IsChecked ?? false,
                    EnableAutoCast = AutoCastCheckBox.IsChecked ?? false,
                    CastHotkey = HotkeyTextBox.Text
                };

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

                // Start log monitoring
                logMonitor = new LogMonitor(LogFilePathTextBox.Text, rotationManager);
                logMonitor.Start();

                // Update UI
                StartButton.IsEnabled = false;
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

            rotationManager = null;

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
                ChainIntervalSlider.Value = e.Delay;
                
                StatusTextBlock.Text = $"Chain imported! {e.Healers.Count} healers, {e.Delay}s interval. Monitoring restarted.";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.LimeGreen;
                
                // Update overlay if it exists
                overlayWindow?.UpdateChainInfo();
            });
        }
    }
}
