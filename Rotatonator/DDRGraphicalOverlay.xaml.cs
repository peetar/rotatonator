using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using NAudio.Wave;

namespace Rotatonator
{
    public partial class DDRGraphicalOverlay : Window
    {
        private readonly RotationManager rotationManager;
        private readonly Dictionary<string, HealerLane> healerLanes = new Dictionary<string, HealerLane>();
        private readonly DispatcherTimer updateTimer;
        private readonly DispatcherTimer pulseTimer;
        private readonly DispatcherTimer starfieldTimer;
        private DateTime lastPulseTime = DateTime.Now; // Start now so first pulse triggers immediately
        private readonly Random random = new Random();
        private readonly List<Star> stars = new List<Star>();
        private const int StarCount = 200;
        
        // Background music
        private IWavePlayer? wavePlayer;
        private AudioFileReader? audioFileReader;
        private string[]? loopFiles;
        private int currentLoopIndex = 0;
        private DateTime trackStartTime = DateTime.Now;
        private const double TrackDurationMinutes = 1.0;
        private bool isMuted = false;
        private DDRAudioService? ddrAudioService;
        private DDRScoreTracker? ddrScoreTracker;

        public DDRGraphicalOverlay(RotationManager manager, DDRAudioService? ddrAudio = null, DDRScoreTracker? scoreTracker = null)
        {
            InitializeComponent();
            rotationManager = manager;
            ddrAudioService = ddrAudio;
            ddrScoreTracker = scoreTracker;

            // Subscribe to rotation events
            rotationManager.HealCastDetected += OnHealCastDetected;

            // Update timer for countdown animations
            updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
            
            // Pulse timer for rhythmic visual feedback (synced to rotation interval)
            pulseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            pulseTimer.Tick += PulseTimer_Tick;
            pulseTimer.Start();
            
            // Starfield timer for background animation
            starfieldTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            starfieldTimer.Tick += StarfieldTimer_Tick;
            starfieldTimer.Start();
            
            // Initialize lanes based on current config
            InitializeLanes();
            
            // Initialize starfield after window is loaded (when canvas has actual size)
            this.Loaded += (s, e) => InitializeStarfield();
            
            // Initialize background music
            InitializeBackgroundMusic();
        }
        
        private void InitializeBackgroundMusic()
        {
            try
            {
                // Get loop files from Audio/DDR/loops folder
                string loopsPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Audio", "DDR", "loops"
                );
                
                if (!Directory.Exists(loopsPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Loops folder not found: {loopsPath}");
                    return;
                }
                
                loopFiles = Directory.GetFiles(loopsPath, "*.mp3")
                    .Concat(Directory.GetFiles(loopsPath, "*.wav"))
                    .ToArray();
                
                if (loopFiles.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No audio files found in loops folder");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"Found {loopFiles.Length} loop files");
                
                // Initialize wave player
                wavePlayer = new WaveOutEvent();
                wavePlayer.PlaybackStopped += WavePlayer_PlaybackStopped;
                
                // Start playing
                PlayNextLoop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing background music: {ex.Message}");
            }
        }
        
        private void PlayNextLoop()
        {
            try
            {
                if (loopFiles == null || loopFiles.Length == 0) return;
                
                // If muted, don't play anything
                if (isMuted)
                {
                    wavePlayer?.Stop();
                    return;
                }
                
                // Check if we've been playing this track for a minute
                var elapsed = DateTime.Now - trackStartTime;
                if (elapsed.TotalMinutes >= TrackDurationMinutes)
                {
                    // Pick a new random file
                    int newIndex = random.Next(loopFiles.Length);
                    // Ensure we pick a different file if multiple files exist
                    if (loopFiles.Length > 1)
                    {
                        while (newIndex == currentLoopIndex)
                        {
                            newIndex = random.Next(loopFiles.Length);
                        }
                    }
                    currentLoopIndex = newIndex;
                    trackStartTime = DateTime.Now;
                    System.Diagnostics.Debug.WriteLine($"Switching to new loop: {System.IO.Path.GetFileName(loopFiles[currentLoopIndex])}");
                }
                
                // Stop current playback
                wavePlayer?.Stop();
                audioFileReader?.Dispose();
                
                // Create new reader and set volume to 20%
                audioFileReader = new AudioFileReader(loopFiles[currentLoopIndex])
                {
                    Volume = 0.2f
                };
                
                wavePlayer?.Init(audioFileReader);
                wavePlayer?.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing loop: {ex.Message}");
            }
        }
        
        private void WavePlayer_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            // Play next loop when current one finishes (unless muted)
            if (!isMuted)
            {
                Dispatcher.Invoke(() => PlayNextLoop());
            }
        }
        
        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            isMuted = !isMuted;
            
            // Update button icon
            MuteButton.Content = isMuted ? "🔇" : "🔊";
            
            // Update background music volume
            if (audioFileReader != null)
            {
                audioFileReader.Volume = isMuted ? 0f : 0.2f;
            }
            
            // Update DDR audio service mute state
            if (ddrAudioService != null)
            {
                ddrAudioService.IsMuted = isMuted;
            }
            
            // If unmuting, restart the background music
            if (!isMuted)
            {
                PlayNextLoop();
            }
            else
            {
                // If muting, stop the music
                wavePlayer?.Stop();
            }
            
            System.Diagnostics.Debug.WriteLine($"Audio muted: {isMuted}");
        }
        
        private void InitializeStarfield()
        {
            stars.Clear();
            StarfieldCanvas.Children.Clear();
            
            double centerX = StarfieldCanvas.ActualWidth / 2;
            double centerY = StarfieldCanvas.ActualHeight * 0.15; // 85% toward top (15% from top)
            
            System.Diagnostics.Debug.WriteLine($"Initializing starfield - Canvas size: {StarfieldCanvas.ActualWidth}x{StarfieldCanvas.ActualHeight}, Center: ({centerX}, {centerY})");
            
            // Create stars at random positions
            for (int i = 0; i < StarCount; i++)
            {
                var star = new Star
                {
                    CenterX = centerX,
                    CenterY = centerY,
                    X = (random.NextDouble() - 0.5) * StarfieldCanvas.ActualWidth * 2,
                    Y = (random.NextDouble() - 0.5) * StarfieldCanvas.ActualHeight * 2,
                    Size = random.NextDouble() * 2.0 + 1.0, // 1.0-3.0 (larger for visibility)
                    Speed = random.NextDouble() * 30 + 20
                };
                
                var ellipse = new Ellipse
                {
                    Width = star.Size,
                    Height = star.Size,
                    Fill = Brushes.White,
                    Opacity = random.NextDouble() * 0.8 + 0.4 // 0.4-1.2 (brighter for visibility)
                };
                Canvas.SetLeft(ellipse, star.X);
                Canvas.SetTop(ellipse, star.Y);
                
                star.Visual = ellipse;
                stars.Add(star);
                StarfieldCanvas.Children.Add(ellipse);
            }
            System.Diagnostics.Debug.WriteLine($"Starfield initialized with {stars.Count} stars");
        }
        
        private void StarfieldTimer_Tick(object? sender, EventArgs e)
        {
            if (stars == null || stars.Count == 0) return;
            
            // Update star positions
            foreach (var star in stars)
            {
                // Calculate direction from center
                double dx = star.X - star.CenterX;
                double dy = star.Y - star.CenterY;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                
                if (distance < 0.1) distance = 0.1;
                
                // Move star away from center
                double moveX = (dx / distance) * star.Speed * 0.05;
                double moveY = (dy / distance) * star.Speed * 0.05;
                
                star.X += moveX;
                star.Y += moveY;
                
                // Recycle star if it goes too far
                if (distance > Math.Max(StarfieldCanvas.ActualWidth, StarfieldCanvas.ActualHeight))
                {
                    // Reset star near center
                    star.X = star.CenterX + (random.NextDouble() - 0.5) * 20;
                    star.Y = star.CenterY + (random.NextDouble() - 0.5) * 20;
                    star.Size = random.NextDouble() * 1.5 + 0.5;
                    star.Speed = random.NextDouble() * 30 + 20;
                    if (star.Visual != null)
                    {
                        star.Visual.Width = star.Size;
                        star.Visual.Height = star.Size;
                        star.Visual.Opacity = random.NextDouble() * 0.6 + 0.2;
                    }
                }
                
                // Update visual position
                Canvas.SetLeft(star.Visual, star.X);
                Canvas.SetTop(star.Visual, star.Y);
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void InitializeLanes()
        {
            LanesContainer.Children.Clear();
            LanesContainer.ColumnDefinitions.Clear();
            healerLanes.Clear();

            var healers = rotationManager.Config.Healers;
            if (healers.Count == 0) return;

            // Create equal-width columns for each healer
            for (int i = 0; i < healers.Count; i++)
            {
                LanesContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var lane = new HealerLane(healers[i], this); // Pass overlay reference for particle effects
                Grid.SetColumn(lane.Container, i);
                Grid.SetRow(lane.Container, 0); // Place in first row (above hit zone)
                LanesContainer.Children.Add(lane.Container);
                healerLanes[healers[i]] = lane;
            }
        }

        private void OnHealCastDetected(object? sender, HealCastEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (healerLanes.TryGetValue(e.HealerName, out var lane))
                {
                    // Start casting for this healer
                    lane.StartCasting();

                    // Trigger particle effect when healer casts
                    // Scale the effect based on timing accuracy
                    double timingAccuracy = 1.0; // Default to max
                    if (e.ExpectedCastTime.HasValue)
                    {
                        var difference = Math.Abs((e.CastTime - e.ExpectedCastTime.Value).TotalSeconds);
                        // Perfect = 0s diff, scale down to 0.3 for 3+ seconds off
                        timingAccuracy = Math.Max(0.3, 1.0 - (difference / 3.0));
                    }
                    PlayParticleEffect(e.HealerName, timingAccuracy);

                    // Find the current healer's position
                    int castIndex = rotationManager.Config.Healers.FindIndex(h => 
                        h.Equals(e.HealerName, StringComparison.OrdinalIgnoreCase));
                    
                    if (castIndex >= 0)
                    {
                        double interval = rotationManager.Config.ChainInterval.TotalSeconds;
                        int chainSize = rotationManager.Config.Healers.Count;
                        
                        // Full rotation time = interval * chain size
                        // This is the timer for the current caster
                        double fullRotationTime = interval * chainSize;

                        // Update all healers' countdowns
                        for (int i = 0; i < chainSize; i++)
                        {
                            int targetIndex = (castIndex + i) % chainSize;
                            string healer = rotationManager.Config.Healers[targetIndex];
                            
                            if (healerLanes.TryGetValue(healer, out var targetLane))
                            {
                                if (i == 0)
                                {
                                    // Current healer: full rotation time
                                    targetLane.StartCountdown(TimeSpan.FromSeconds(fullRotationTime));
                                }
                                else
                                {
                                    // Subsequent healers: interval * position
                                    double secondsUntilCast = interval * i;
                                    if (secondsUntilCast <= 10)
                                    {
                                        targetLane.StartCountdown(TimeSpan.FromSeconds(secondsUntilCast));
                                    }
                                    else
                                    {
                                        // Beyond 10 seconds, stop showing countdown
                                        targetLane.StopCountdown();
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }

        private void OnPlayerTurnStarting(object? sender, PlayerTurnEventArgs e)
        {
            // This event is no longer used for triggering countdowns
            // Countdowns are now triggered by HealCastDetected
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            foreach (var lane in healerLanes.Values)
            {
                lane.Update();
            }
            
            // Update scores from tracker
            if (ddrScoreTracker != null)
            {
                var leaderboard = ddrScoreTracker.GetLeaderboard();
                foreach (var entry in leaderboard)
                {
                    if (healerLanes.TryGetValue(entry.HealerName, out var lane))
                    {
                        lane.UpdateScore(entry.Score);
                    }
                }
            }
        }

        private void PulseTimer_Tick(object? sender, EventArgs e)
        {
            // Trigger rhythmic pulse on all lanes synced to rotation interval
            if (rotationManager.Config == null || rotationManager.Config.Healers.Count == 0)
                return;

            // Pulse every 0.5 seconds exactly
            double interval = 0.5;
            var now = DateTime.Now;
            
            // Pulse every interval seconds
            if ((now - lastPulseTime).TotalSeconds >= interval)
            {
                lastPulseTime = now;
                System.Diagnostics.Debug.WriteLine($"PULSE TRIGGERED at {now:HH:mm:ss.fff} - pulsing {healerLanes.Count} lanes");
                foreach (var lane in healerLanes.Values)
                {
                    lane.TriggerPulse();
                }
            }
        }

        /// <summary>
        /// Spawn particle effect when a healer casts
        /// Scale intensity based on timing accuracy (1.0 = perfect, 0.3 = very late/early)
        /// </summary>
        private void PlayParticleEffect(string healerName, double accuracy)
        {
            var lane = healerLanes.FirstOrDefault(x => x.Key == healerName);
            if (lane.Value == null) return;

            var container = lane.Value.Container;
            var color = GetNeonColorForLane(healerName);

            // More particles and larger burst for better accuracy
            int particleCount = (int)(6 + (accuracy * 10)); // 6-16 particles
            double maxDistance = 80 + (accuracy * 120); // 80-200px spread (400% bigger)

            for (int i = 0; i < particleCount; i++)
            {
                var particle = new Rectangle
                {
                    Width = 24 + (accuracy * 16), // 24-40px based on accuracy (400% bigger)
                    Height = 24 + (accuracy * 16),
                    Fill = new SolidColorBrush(color),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 30),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = color,
                        BlurRadius = 32 + (accuracy * 48), // More glow for perfect timing (400% bigger)
                        ShadowDepth = 0,
                        Opacity = 0.8 + (accuracy * 0.2)
                    }
                };

                container.Children.Add(particle);

                // Animate particle bursting outward and fading
                var storyboard = new Storyboard();

                // Movement - spread based on accuracy
                double angle = (Math.PI * 2 * i) / particleCount; // Distribute evenly in circle
                double xOffset = Math.Cos(angle) * maxDistance;
                double yOffset = -Math.Abs(Math.Sin(angle)) * maxDistance * 0.5; // Burst upward

                var moveXAnimation = new DoubleAnimationUsingKeyFrames();
                moveXAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
                moveXAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(xOffset, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400))));
                Storyboard.SetTarget(moveXAnimation, particle);
                Storyboard.SetTargetProperty(moveXAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                storyboard.Children.Add(moveXAnimation);

                var moveYAnimation = new DoubleAnimationUsingKeyFrames();
                moveYAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
                moveYAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(yOffset, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400))));
                Storyboard.SetTarget(moveYAnimation, particle);
                Storyboard.SetTargetProperty(moveYAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                storyboard.Children.Add(moveYAnimation);

                // Fade out
                var fadeAnimation = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
                Storyboard.SetTarget(fadeAnimation, particle);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));
                storyboard.Children.Add(fadeAnimation);

                storyboard.Completed += (s, e) =>
                {
                    container.Children.Remove(particle);
                };

                // Set transform for movement
                particle.RenderTransform = new TranslateTransform();

                storyboard.Begin();
            }
        }

        private Color GetNeonColorForLane(string healerName)
        {
            var lane = healerLanes.FirstOrDefault(x => x.Key == healerName);
            if (lane.Value == null) return Colors.Cyan;

            // Match the neon color assigned to this lane
            int index = healerLanes.Keys.ToList().IndexOf(healerName);
            Color[] colors = new[]
            {
                Color.FromArgb(255, 0, 255, 255),       // Cyan
                Color.FromArgb(255, 255, 0, 255),       // Magenta
                Color.FromArgb(255, 255, 255, 0),       // Yellow
                Color.FromArgb(255, 0, 255, 136)        // Green
            };
            return colors[index % colors.Length];
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            rotationManager.HealCastDetected -= OnHealCastDetected;
            updateTimer.Stop();
            pulseTimer.Stop();
            starfieldTimer.Stop();
            
            // Clean up audio
            wavePlayer?.Stop();
            audioFileReader?.Dispose();
            wavePlayer?.Dispose();
        }

        /// <summary>
        /// Represents a single healer's lane in the DDR overlay
        /// </summary>
        private class HealerLane
        {
            public Grid Container { get; }
            private readonly Rectangle laneLine;
            private readonly TextBlock healerNameBottom;
            private readonly TextBlock scoreDisplay;
            private readonly TextBlock movingName;
            private readonly TextBlock scorePopup;
            private readonly string healerName;
            private readonly Storyboard? vibrateStoryboard;
            private readonly Storyboard? pulseStoryboard;
            private readonly Brush neonBrush;
            private readonly Brush glowBrush;
            private readonly DDRGraphicalOverlay overlay;

            private DateTime? countdownStart;
            private TimeSpan countdownDuration;
            private DateTime? castStart;
            private bool isCasting = false;
            private int previousScore = 0;

            private const double NormalLaneWidth = 15;    // 1/4 of previous 60px
            private const double CastingLaneWidth = 20;   // 1/4 of previous 80px
            private const double CastDuration = 10.0;     // seconds

            private static int laneColorIndex = 0;
            private static readonly Brush[] NeonColors = new[]
            {
                new SolidColorBrush(Color.FromArgb(200, 0, 255, 255)),      // Cyan
                new SolidColorBrush(Color.FromArgb(200, 255, 0, 255)),      // Magenta
                new SolidColorBrush(Color.FromArgb(200, 255, 255, 0)),      // Yellow
                new SolidColorBrush(Color.FromArgb(200, 0, 255, 136))       // Green
            };

            public HealerLane(string name, DDRGraphicalOverlay overlayRef)
            {
                healerName = name;
                overlay = overlayRef;
                Container = new Grid { Margin = new Thickness(5, 0, 5, 0) };

                // Select neon color for this lane
                neonBrush = NeonColors[laneColorIndex % NeonColors.Length];
                glowBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)); // Glow effect
                laneColorIndex++;

                // Lane line (vertical rectangle) with glow - extends to touch bottom names as hit zone
                laneLine = new Rectangle
                {
                    Width = NormalLaneWidth,
                    Fill = neonBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    RenderTransform = new TranslateTransform(),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = ((SolidColorBrush)neonBrush).Color,
                        BlurRadius = 15,
                        ShadowDepth = 0,
                        Opacity = 0.8
                    },
                    Margin = new Thickness(0, 0, 0, 10) // Shortened by 10px to clear the score display
                };
                Container.Children.Add(laneLine);

                // Healer name at bottom with glow
                healerNameBottom = new TextBlock
                {
                    Text = healerName,
                    Foreground = Brushes.White,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 10),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.Cyan,
                        BlurRadius = 10,
                        ShadowDepth = 0,
                        Opacity = 0.6
                    }
                };
                Container.Children.Add(healerNameBottom);
                
                // Score display below healer name
                scoreDisplay = new TextBlock
                {
                    Text = "0",
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 215, 0)), // Gold
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, -5), // Position just below name
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromArgb(255, 255, 215, 0), // Gold glow
                        BlurRadius = 8,
                        ShadowDepth = 0,
                        Opacity = 0.5
                    }
                };
                Container.Children.Add(scoreDisplay);

                // Moving name for countdown with glow
                movingName = new TextBlock
                {
                    Text = healerName,
                    Foreground = Brushes.White,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 10),
                    Visibility = Visibility.Collapsed,
                    RenderTransform = new TranslateTransform(),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.Yellow,
                        BlurRadius = 12,
                        ShadowDepth = 0,
                        Opacity = 0.8
                    }
                };
                Container.Children.Add(movingName);
                
                // Score popup "+100" with glow and fade
                scorePopup = new TextBlock
                {
                    Text = "+100",
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 215, 0)), // Gold
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = Visibility.Collapsed,
                    RenderTransform = new TranslateTransform(),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromArgb(255, 255, 215, 0), // Gold glow
                        BlurRadius = 15,
                        ShadowDepth = 0,
                        Opacity = 0.9
                    }
                };
                Container.Children.Add(scorePopup);

                // Load animations from Window resources (not Container)
                try
                {
                    vibrateStoryboard = (Storyboard)overlay.FindResource("VibrateAnimation");
                    if (vibrateStoryboard != null)
                    {
                        var clonedVibrate = vibrateStoryboard.Clone();
                        Storyboard.SetTarget(clonedVibrate, laneLine);
                        vibrateStoryboard = clonedVibrate;
                    }
                    
                    pulseStoryboard = (Storyboard)overlay.FindResource("PulseAnimation");
                    if (pulseStoryboard != null)
                    {
                        var clonedPulse = pulseStoryboard.Clone();
                        Storyboard.SetTarget(clonedPulse, laneLine);
                        pulseStoryboard = clonedPulse;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load animations: {ex.Message}");
                }
            }
            
            public void TriggerPulse()
            {
                System.Diagnostics.Debug.WriteLine($"TriggerPulse called for {healerName}, storyboard is {(pulseStoryboard == null ? "NULL" : "loaded")}");
                pulseStoryboard?.Begin();
            }

            public void StartCasting()
            {
                isCasting = true;
                castStart = DateTime.Now;
                laneLine.Width = CastingLaneWidth;
                // Removed vibrate animation - it's distracting
            }

            public void StopCasting()
            {
                isCasting = false;
                castStart = null;
                laneLine.Width = NormalLaneWidth;
                vibrateStoryboard?.Stop();
            }

            public void StartCountdown(TimeSpan duration)
            {
                countdownStart = DateTime.Now;
                countdownDuration = duration;
                movingName.Visibility = Visibility.Visible;
            }

            public void StopCountdown()
            {
                countdownStart = null;
                movingName.Visibility = Visibility.Collapsed;
            }

            public void Update()
            {
                // Auto-stop casting after 10 seconds
                if (isCasting && castStart.HasValue)
                {
                    var elapsed = DateTime.Now - castStart.Value;
                    if (elapsed.TotalSeconds >= CastDuration)
                    {
                        StopCasting();
                    }
                }

                if (countdownStart.HasValue)
                {
                    var elapsed = DateTime.Now - countdownStart.Value;
                    var remaining = countdownDuration - elapsed;

                    if (remaining.TotalSeconds <= 0)
                    {
                        // Countdown complete - hide name
                        movingName.Visibility = Visibility.Collapsed;
                        countdownStart = null;
                        return;
                    }

                    // Calculate position: line is 10 seconds tall
                    // remaining=10s → top (far up, negative Y), remaining=0s → bottom (Y=0, overlapping static name)
                    double progress = 1.0 - (remaining.TotalSeconds / 10.0);
                    progress = Math.Max(0, Math.Min(1.0, progress)); // Clamp 0-1

                    // Move from top (negative Y) to bottom (Y=0)
                    // When progress=0 (10s remaining), Y should be -containerHeight
                    // When progress=1 (0s remaining), Y should be 0 (perfectly overlapping static name)
                    double containerHeight = Container.ActualHeight;
                    double targetY = -(containerHeight * (1.0 - progress));
                    ((TranslateTransform)movingName.RenderTransform).Y = targetY;
                }
            }
            
            public void UpdateScore(int score)
            {
                // Calculate score difference
                int scoreDifference = score - previousScore;
                
                // Show popup for any score change (positive points or streak break)
                if (scoreDifference != 0)
                {
                    // Determine if this is a bad cast (negative points without streak break message)
                    // or a good cast (positive points) or a streak break (score drops)
                    if (scoreDifference > 0)
                    {
                        PlayScorePopup($"+{scoreDifference}");
                    }
                    else if (scoreDifference < 0)
                    {
                        // Negative points - streak broken or early/late
                        PlayScorePopup("Streak broken");
                    }
                }
                
                previousScore = score;
                scoreDisplay.Text = score.ToString();
            }
            
            private void PlayScorePopup(string text)
            {
                scorePopup.Text = text;
                scorePopup.Visibility = Visibility.Visible;
                scorePopup.Opacity = 1.0;
                ((TranslateTransform)scorePopup.RenderTransform).Y = 0;
                
                // Create animation: fade out and move up
                var storyboard = new Storyboard();
                
                // Opacity animation (fade from 1 to 0 over 1 second)
                var opacityAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromSeconds(1.0),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(opacityAnimation, scorePopup);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(TextBlock.OpacityProperty));
                storyboard.Children.Add(opacityAnimation);
                
                // Y movement animation (move up 40 pixels)
                var moveAnimation = new DoubleAnimation
                {
                    From = 0.0,
                    To = -40.0,
                    Duration = TimeSpan.FromSeconds(1.0),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(moveAnimation, scorePopup);
                Storyboard.SetTargetProperty(moveAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                storyboard.Children.Add(moveAnimation);
                
                // Glow animation (blur increases then fades)
                var glowAnimation = new DoubleAnimation
                {
                    From = 15.0,
                    To = 30.0,
                    Duration = TimeSpan.FromSeconds(1.0),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(glowAnimation, scorePopup);
                Storyboard.SetTargetProperty(glowAnimation, new PropertyPath("(UIElement.Effect).(DropShadowEffect.BlurRadius)"));
                storyboard.Children.Add(glowAnimation);
                
                storyboard.Completed += (s, e) => scorePopup.Visibility = Visibility.Collapsed;
                storyboard.Begin();
            }
        }
        
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopAllAudio();
        }
        
        public void StopAllAudio()
        {
            // Stop all audio playback
            isMuted = true;
            wavePlayer?.Stop();
            audioFileReader?.Dispose();
            wavePlayer?.Dispose();
            
            // Stop all timers
            starfieldTimer?.Stop();
            updateTimer?.Stop();
            pulseTimer?.Stop();
        }
    }
    
    /// <summary>
    /// Represents a single star in the starfield background
    /// </summary>
    internal class Star
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double Size { get; set; }
        public double Speed { get; set; }
        public Ellipse? Visual { get; set; }
    }
}
