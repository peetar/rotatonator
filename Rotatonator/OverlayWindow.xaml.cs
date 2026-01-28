using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Rotatonator
{
    public partial class OverlayWindow : Window
    {
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private readonly RotationManager rotationManager;
        private readonly DispatcherTimer updateTimer;
        private readonly ObservableCollection<HealTimerViewModel> activeHeals;
        private IntPtr hwnd;
        private DispatcherTimer? warningFlashTimer;
        private DispatcherTimer? castNowResetTimer;
        private DateTime lastHealTime = DateTime.MinValue;
        private readonly TextToSpeechService ttsService;
        private readonly DDRScoreTracker ddrScoreTracker;
        private readonly DDRAudioService ddrAudioService;

        public OverlayWindow(RotationManager manager)
        {
            InitializeComponent();
            
            rotationManager = manager;
            activeHeals = new ObservableCollection<HealTimerViewModel>();
            ActiveHealsItemsControl.ItemsSource = activeHeals;
            ttsService = new TextToSpeechService();
            ddrScoreTracker = new DDRScoreTracker();
            ddrAudioService = new DDRAudioService();

            // Make window completely click-through
            Loaded += (s, e) =>
            {
                hwnd = new WindowInteropHelper(this).Handle;
                MakeClickThrough();
            };

            // Subscribe to rotation events
            rotationManager.HealCastDetected += OnHealCastDetected;
            rotationManager.PlayerTurnStarting += OnPlayerTurnStarting;
            rotationManager.PlayerTurnNow += OnPlayerTurnNow;

            // Update timer for countdown
            updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            UpdateChainInfo();
        }

        private void MakeClickThrough()
        {
            if (hwnd != IntPtr.Zero)
            {
                int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            }
        }

        public void UpdateChainInfo()
        {
            var config = rotationManager.Config;
            int position = rotationManager.GetPlayerPosition() + 1;
            ChainInfoTextBlock.Text = $"Chain: {config.Healers.Count} healers | Your position: {position} | Interval: {config.ChainInterval.TotalSeconds}s";
        }

        private void OnHealCastDetected(object? sender, HealCastEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // Update last heal time
                lastHealTime = DateTime.Now;
                
                // Make sure overlay is visible
                if (Visibility != Visibility.Visible)
                    Visibility = Visibility.Visible;
                
                // Stop the warning flash timer when any heal is cast
                warningFlashTimer?.Stop();
                
                // Stop the cast now reset timer since a heal was actually cast
                castNowResetTimer?.Stop();
                
                // Reset overlay background to normal when any heal is cast
                OverlayBorder.Background = new SolidColorBrush(Color.FromArgb(204, 0, 0, 0));
                
                // Play beep if enabled
                var audioConfig = rotationManager.Config.AudioAlerts;
                if (audioConfig?.EnableAudioBeep == true)
                {
                    try
                    {
                        System.Threading.Tasks.Task.Run(() =>
                        {
                            PlayBeep(600, 50, 0.25f);
                        });
                    }
                    catch
                    {
                        // Beep not supported
                    }
                }
                
                // TTS announcements based on config
                if (audioConfig != null)
                {
                    Console.WriteLine($"[TTS] Config - Number:{audioConfig.AnnounceHealerNumber} Name:{audioConfig.AnnounceHealerName} Target:{audioConfig.AnnounceTargetName}");
                    
                    if (audioConfig.AnnounceHealerNumber && e.HealerPosition > 0)
                    {
                        Console.WriteLine($"[TTS] Calling AnnounceHealerNumber({e.HealerPosition})");
                        ttsService.AnnounceHealerNumber(e.HealerPosition);
                    }
                    
                    if (audioConfig.AnnounceHealerName)
                    {
                        Console.WriteLine($"[TTS] Calling AnnounceHealerName({e.HealerName})");
                        ttsService.AnnounceHealerName(e.HealerName);
                    }
                    
                    if (audioConfig.AnnounceTargetName && !string.IsNullOrWhiteSpace(e.TargetName))
                    {
                        Console.WriteLine($"[TTS] Calling AnnounceTargetName({e.TargetName})");
                        ttsService.AnnounceTargetName(e.TargetName);
                    }
                }
                else
                {
                    Console.WriteLine("[TTS] AudioAlerts config is null");
                }
                
                // DDR Mode feedback
                if (rotationManager.Config.EnableDDRMode && e.ExpectedCastTime.HasValue && !e.SkipDDRScoring)
                {
                    var ddrResult = ddrScoreTracker.EvaluateTiming(e.ExpectedCastTime.Value, e.CastTime, e.HealerName, e.IsPlayerCast, rotationManager.Config.ChainInterval);
                    int goodStreak = ddrScoreTracker.GetCurrentStreak(e.HealerName);
                    int badStreak = ddrScoreTracker.GetCurrentBadStreak(e.HealerName);
                    ddrAudioService.PlayFeedback(ddrResult, badStreak, goodStreak);
                    
                    Console.WriteLine($"[DDR] {e.HealerName} - Accuracy: {ddrResult.Accuracy}, " +
                                    $"Difference: {ddrResult.TimeDifference:F2}s, " +
                                    $"Points: {ddrResult.PointsAwarded:+#;-#;0}, " +
                                    $"Total: {ddrResult.TotalScore}" +
                                    (e.IsPlayerCast ? $", Streak: {ddrResult.PerfectStreak}, Combo: {ddrResult.ComboLevel}" : ""));
                }
                else if (e.SkipDDRScoring)
                {
                    Console.WriteLine($"[DDR] Skipping DDR scoring for {e.HealerName} - heal timing out of range");
                }
                
                // If this is the player casting, hide the "YOU'RE NEXT" warning
                if (e.IsPlayerCast)
                {
                    NextWarningTextBlock.Visibility = Visibility.Collapsed;
                }
                
                // Remove existing timer for this healer
                for (int i = activeHeals.Count - 1; i >= 0; i--)
                {
                    if (activeHeals[i].HealerName == e.HealerName)
                    {
                        activeHeals.RemoveAt(i);
                    }
                }

                // Add new timer
                var viewModel = new HealTimerViewModel
                {
                    HealerName = e.HealerName,
                    TargetName = e.TargetName,
                    CastTime = e.CastTime,
                    TotalTime = 10.0, // Complete Heal cast time is ~10 seconds
                    IsPlayerCast = e.IsPlayerCast,
                    ChainDelay = rotationManager.Config.ChainInterval,
                    IsInRecastDelay = true // Starts in recast delay
                };

                activeHeals.Add(viewModel);
            });
        }

        private void OnPlayerTurnStarting(object? sender, PlayerTurnEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                NextWarningTextBlock.Text = $"YOU'RE NEXT! ({e.TimeUntilCast.TotalSeconds}s)";
                NextWarningTextBlock.Visibility = Visibility.Visible;
                
                // TTS announcement for "you're next"
                var audioConfig = rotationManager.Config.AudioAlerts;
                if (audioConfig?.AnnounceYoureNext == true)
                {
                    ttsService.AnnounceYoureNext();
                }
                
                if (!rotationManager.Config.EnableVisualAlerts)
                    return;
                
                // Start progressive flashing that gets faster as time approaches
                DateTime startTime = DateTime.Now;
                var originalBrush = OverlayBorder.Background;
                var whiteBrush = new SolidColorBrush(Color.FromArgb(204, 255, 255, 255));
                bool isWhite = false;
                
                warningFlashTimer?.Stop();
                warningFlashTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
                warningFlashTimer.Tick += (s, args) =>
                {
                    var elapsed = DateTime.Now - startTime;
                    var remaining = e.TimeUntilCast - elapsed;
                    
                    // Update countdown text
                    NextWarningTextBlock.Text = $"YOU'RE NEXT! ({remaining.TotalSeconds:F1}s)";
                    
                    // Flash white, getting faster as we approach 0
                    // Start at 1000ms, speed up to 100ms
                    double progress = Math.Min(1.0, Math.Max(0, 1.0 - (remaining.TotalSeconds / e.TimeUntilCast.TotalSeconds)));
                    int newInterval = (int)(1000 - (progress * 900)); // 1000ms -> 100ms
                    warningFlashTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(newInterval, 100));
                    
                    isWhite = !isWhite;
                    OverlayBorder.Background = isWhite ? whiteBrush : originalBrush;
                };
                warningFlashTimer.Start();
            });
        }
        private void OnPlayerTurnNow(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // Stop the flashing timer
                warningFlashTimer?.Stop();
                
                // Update text
                NextWarningTextBlock.Text = "CAST NOW!";
                
                // TTS announcement for "cast now"
                var audioConfig = rotationManager.Config.AudioAlerts;
                if (audioConfig?.AnnounceCastNow == true)
                {
                    ttsService.AnnounceCastNow();
                }
                
                if (!rotationManager.Config.EnableVisualAlerts)
                    return;
                
                // Turn overlay solid WHITE and keep it white
                OverlayBorder.Background = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255));
                
                // Set a timer to reset the overlay back to transparent after the cast window
                // This ensures the overlay returns to normal even if the user doesn't cast
                castNowResetTimer?.Stop();
                castNowResetTimer = new DispatcherTimer
                {
                    Interval = rotationManager.Config.ChainInterval,
                    IsEnabled = false
                };
                castNowResetTimer.Tick += (s, args) =>
                {
                    castNowResetTimer?.Stop();
                    OverlayBorder.Background = new SolidColorBrush(Color.FromArgb(204, 0, 0, 0));
                    NextWarningTextBlock.Visibility = Visibility.Collapsed;
                };
                castNowResetTimer.Start();
            });
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            
            // Check if we should hide the overlay due to inactivity
            if (lastHealTime != DateTime.MinValue && (now - lastHealTime).TotalSeconds > 30)
            {
                if (Visibility == Visibility.Visible)
                    Visibility = Visibility.Collapsed;
                return;
            }
            
            for (int i = activeHeals.Count - 1; i >= 0; i--)
            {
                var heal = activeHeals[i];
                var elapsed = now - heal.CastTime;
                var remaining = TimeSpan.FromSeconds(10) - elapsed; // CH cast time ~10s

                if (remaining.TotalSeconds <= 0)
                {
                    activeHeals.RemoveAt(i);
                }
                else
                {
                    // Round to nearest 0.5 seconds
                    double roundedSeconds = Math.Round(remaining.TotalSeconds * 2) / 2;
                    heal.TimeRemaining = $"{roundedSeconds:F1}s";
                    heal.ProgressValue = elapsed.TotalSeconds;
                    
                    // Check if this healer is still in recast delay
                    heal.IsInRecastDelay = elapsed < heal.ChainDelay;
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            updateTimer.Stop();
            rotationManager.HealCastDetected -= OnHealCastDetected;
            rotationManager.PlayerTurnStarting -= OnPlayerTurnStarting;
            rotationManager.PlayerTurnNow -= OnPlayerTurnNow;
            ttsService?.Dispose();
            ddrAudioService?.Dispose();
            base.OnClosed(e);
        }
        
        /// <summary>
        /// Get the DDR audio service for use by other overlays
        /// </summary>
        public DDRAudioService? GetDDRAudioService()
        {
            return ddrAudioService;
        }
        
        /// <summary>
        /// Get the DDR score tracker for use by other overlays
        /// </summary>
        public DDRScoreTracker? GetDDRScoreTracker()
        {
            return ddrScoreTracker;
        }

        private void PlayBeep(int frequency, int duration, float volume)
        {
            try
            {
                var sampleRate = 44100;
                var samples = (int)(sampleRate * duration / 1000.0);
                
                var signalGenerator = new SignalGenerator(sampleRate, 1)
                {
                    Frequency = frequency,
                    Type = SignalGeneratorType.Sin,
                    Gain = volume
                };

                var signal = signalGenerator.Take(TimeSpan.FromMilliseconds(duration));

                using (var waveOut = new WaveOutEvent())
                {
                    waveOut.Init(signal);
                    waveOut.Play();
                    System.Threading.Thread.Sleep(duration);
                }
            }
            catch
            {
                // Audio playback failed
            }
        }
    }

    public class HealTimerViewModel : INotifyPropertyChanged
    {
        private string timeRemaining = "10.0s";
        private double progressValue = 0;
        private bool isInRecastDelay = false;

        public string HealerName { get; set; } = "";
        public string TargetName { get; set; } = "";
        public DateTime CastTime { get; set; }
        public double TotalTime { get; set; }
        public bool IsPlayerCast { get; set; }
        public TimeSpan ChainDelay { get; set; } = TimeSpan.FromSeconds(6);

        public string TimeRemaining
        {
            get => timeRemaining;
            set
            {
                timeRemaining = value;
                OnPropertyChanged(nameof(TimeRemaining));
            }
        }

        public double ProgressValue
        {
            get => progressValue;
            set
            {
                progressValue = value;
                OnPropertyChanged(nameof(ProgressValue));
            }
        }

        public bool IsInRecastDelay
        {
            get => isInRecastDelay;
            set
            {
                isInRecastDelay = value;
                OnPropertyChanged(nameof(IsInRecastDelay));
                OnPropertyChanged(nameof(BarColor));
            }
        }

        public Brush TextColor => IsPlayerCast 
            ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) 
            : Brushes.White;

        public Brush BarColor
        {
            get
            {
                // Red while in recast delay
                if (IsInRecastDelay)
                {
                    return new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
                }
                // Normal colors once recast delay is over
                return IsPlayerCast
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))  // Green for player
                    : new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Blue for others
            }
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(TargetName))
                {
                    return $"{HealerName} → {TargetName}";
                }
                return HealerName;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
