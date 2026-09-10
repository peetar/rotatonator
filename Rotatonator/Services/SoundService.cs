using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Rotatonator
{
    /// <summary>
    /// Service for playing audio sound effects like chain update notifications
    /// </summary>
    public static class SoundService
    {
        public static bool PlaySoundOnChainUpdate { get; set; } = true;

        /// <summary>
        /// Plays an audible alert indicating the chain has been updated (via log or cloud API).
        /// </summary>
        public static void PlayChainUpdatedSound()
        {
            if (!PlaySoundOnChainUpdate)
            {
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                    // Priority order for sound files:
                    // 1. Explicit custom chain update sound if present
                    // 2. Built-in ding sound from good_common DDR assets
                    string[] candidatePaths = new[]
                    {
                        Path.Combine(baseDir, "Audio", "chain_updated.wav"),
                        Path.Combine(baseDir, "Audio", "chain_updated.mp3"),
                        Path.Combine(baseDir, "Audio", "DDR", "good_common", "ding-1.mp3"),
                    };

                    foreach (var path in candidatePaths)
                    {
                        if (File.Exists(path))
                        {
                            try
                            {
                                using (var reader = new AudioFileReader(path))
                                using (var waveOut = new WaveOutEvent())
                                {
                                    waveOut.Init(reader);
                                    waveOut.Play();
                                    while (waveOut.PlaybackState == PlaybackState.Playing)
                                    {
                                        Thread.Sleep(10);
                                    }
                                }
                                return;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[SoundService] Failed playing {path}: {ex.Message}");
                            }
                        }
                    }

                    // Fallback: Synthesize a pleasant two-tone chime (D5 -> A5)
                    PlayChimeTone(587, 90, 0.30f);
                    Thread.Sleep(20);
                    PlayChimeTone(880, 160, 0.30f);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SoundService] Error in PlayChainUpdatedSound: {ex.Message}");
                }
            });
        }

        private static void PlayChimeTone(int frequency, int durationMs, float volume)
        {
            try
            {
                const int sampleRate = 44100;
                var signalGenerator = new SignalGenerator(sampleRate, 1)
                {
                    Frequency = frequency,
                    Type = SignalGeneratorType.Sin,
                    Gain = volume
                };

                var signal = signalGenerator.Take(TimeSpan.FromMilliseconds(durationMs));

                using (var waveOut = new WaveOutEvent())
                {
                    waveOut.Init(signal);
                    waveOut.Play();
                    Thread.Sleep(durationMs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SoundService] Error playing tone: {ex.Message}");
            }
        }
    }
}
