using System;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Rotatonator
{
    /// <summary>
    /// Service for playing audio feedback and voice notifications like chain update alerts
    /// </summary>
    public static class SoundService
    {
        public static bool PlaySoundOnChainUpdate { get; set; } = true;
        private static SpeechSynthesizer? synth;
        private static readonly object syncLock = new object();

        private static void EnsureSynthesizer()
        {
            if (synth == null)
            {
                lock (syncLock)
                {
                    if (synth == null)
                    {
                        try
                        {
                            synth = new SpeechSynthesizer();
                            synth.SetOutputToDefaultAudioDevice();
                            synth.Rate = 0; // Natural pacing
                            synth.Volume = 100;

                            // Prefer a clear female voice if installed (e.g. Microsoft Zira)
                            var femaleVoice = synth.GetInstalledVoices()
                                .FirstOrDefault(v => v.Enabled && v.VoiceInfo.Gender == VoiceGender.Female);
                            if (femaleVoice != null)
                            {
                                synth.SelectVoice(femaleVoice.VoiceInfo.Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[SoundService] Failed to initialize SpeechSynthesizer: {ex.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Plays an audible alert indicating the chain has been updated (via log or cloud API).
        /// Plays a custom sound file if present, otherwise announces "Chain updated" via TTS.
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

                    // 1. Custom audio file override if user provided one
                    string[] customFiles = new[]
                    {
                        Path.Combine(baseDir, "Audio", "chain_updated.wav"),
                        Path.Combine(baseDir, "Audio", "chain_updated.mp3"),
                    };

                    foreach (var path in customFiles)
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
                                Console.WriteLine($"[SoundService] Failed playing custom audio file {path}: {ex.Message}");
                            }
                        }
                    }

                    // 2. Clear, natural TTS announcement: "Chain updated"
                    EnsureSynthesizer();
                    if (synth != null)
                    {
                        lock (syncLock)
                        {
                            synth.Speak("Chain updated");
                        }
                        Console.WriteLine("[SoundService] TTS Announced: 'Chain updated'");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SoundService] Error in PlayChainUpdatedSound: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Plays 1 short audio tone for PVP events.
        /// </summary>
        public static void PlayPvpAlertTone()
        {
            Task.Run(() =>
            {
                try
                {
                    PlayTone(750, 130, 0.35f);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SoundService] Error playing PVP tone: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Plays 2 short victory tones for raid kill events.
        /// </summary>
        public static void PlayRaidKillAlertTones()
        {
            Task.Run(() =>
            {
                try
                {
                    PlayTone(600, 90, 0.35f);
                    Thread.Sleep(50);
                    PlayTone(900, 150, 0.35f);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SoundService] Error playing raid kill tones: {ex.Message}");
                }
            });
        }

        private static void PlayTone(int frequency, int durationMs, float volume)
        {
            try
            {
                const int sampleRate = 44100;
                var signalGenerator = new NAudio.Wave.SampleProviders.SignalGenerator(sampleRate, 1)
                {
                    Frequency = frequency,
                    Type = NAudio.Wave.SampleProviders.SignalGeneratorType.Sin,
                    Gain = volume
                };

                var signal = signalGenerator.Take(TimeSpan.FromMilliseconds(durationMs));

                using var waveOut = new WaveOutEvent();
                waveOut.Init(signal);
                waveOut.Play();
                Thread.Sleep(durationMs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SoundService] Error playing tone: {ex.Message}");
            }
        }
    }
}
