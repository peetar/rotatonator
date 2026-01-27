using System;
using System.IO;
using System.Linq;
using NAudio.Wave;

namespace Rotatonator
{
    /// <summary>
    /// Service for playing DDR mode audio feedback files
    /// </summary>
    public class DDRAudioService : IDisposable
    {

        private readonly string audioFolder;
        private readonly string goodCommonFolder;
        private readonly string goodRareFolder;
        private readonly string badCommonFolder;
        private readonly string badRareFolder;
        private readonly Random rng = new Random();
        private bool disposed = false;

        private string[] goodCommonFiles = Array.Empty<string>();
        private string[] goodRareFiles = Array.Empty<string>();
        private string[] badCommonFiles = Array.Empty<string>();
        private string[] badRareFiles = Array.Empty<string>();

        public DDRAudioService()
        {
            // Audio files should be in Audio/DDR folder next to executable
            string exeFolder = AppDomain.CurrentDomain.BaseDirectory;
            audioFolder = Path.Combine(exeFolder, "Audio", "DDR");
            goodCommonFolder = Path.Combine(audioFolder, "good_common");
            goodRareFolder = Path.Combine(audioFolder, "good_rare");
            badCommonFolder = Path.Combine(audioFolder, "bad_common");
            badRareFolder = Path.Combine(audioFolder, "bad_rare");

            // Ensure all folders exist
            Directory.CreateDirectory(goodCommonFolder);
            Directory.CreateDirectory(goodRareFolder);
            Directory.CreateDirectory(badCommonFolder);
            Directory.CreateDirectory(badRareFolder);

            RefreshAudioFiles();
        }

        private string[] GetAudioFiles(string folder)
        {
            if (!Directory.Exists(folder)) return Array.Empty<string>();
            return Directory.GetFiles(folder, "*.mp3").Concat(Directory.GetFiles(folder, "*.wav")).ToArray();
        }

        private void RefreshAudioFiles()
        {
            goodCommonFiles = GetAudioFiles(goodCommonFolder);
            goodRareFiles = GetAudioFiles(goodRareFolder);
            badCommonFiles = GetAudioFiles(badCommonFolder);
            badRareFiles = GetAudioFiles(badRareFolder);
        }

        /// <summary>
        /// Play feedback for a DDR timing result
        /// </summary>

        public void PlayFeedback(DDRTimingResult result, int badStreak = 0, int goodStreak = 0)
        {
            // Always rescan so newly added files are considered
            RefreshAudioFiles();

            if (result.Accuracy == DDRAccuracy.Early || result.Accuracy == DDRAccuracy.Late)
            {
                PlayBadFeedback(result.IsPlayer, badStreak);
            }
            else if (result.Accuracy == DDRAccuracy.Perfect)
            {
                PlayGoodFeedback(result.IsPlayer, goodStreak);
            }
        }

        private void PlayBadFeedback(bool isPlayer, int badStreak)
        {
            // Rare chance scales with streak: 0% up to 25%
            double rareChance = Math.Min(badStreak * 0.05, 0.25);
            if (rng.NextDouble() < rareChance && badRareFiles.Length > 0)
            {
                PlayRandomFile(badRareFiles);
            }
            else if (badCommonFiles.Length > 0)
            {
                PlayRandomFile(badCommonFiles);
            }
        }

        private void PlayGoodFeedback(bool isPlayer, int goodStreak)
        {
            // Rare chance scales with streak: 0% up to 25%
            double rareChance = Math.Min(goodStreak * 0.05, 0.25);
            if (rng.NextDouble() < rareChance && goodRareFiles.Length > 0)
            {
                PlayRandomFile(goodRareFiles);
            }
            else if (goodCommonFiles.Length > 0)
            {
                PlayRandomFile(goodCommonFiles);
            }
        }

        private void PlayRandomFile(string[] files)
        {
            if (files.Length == 0) return;
            int idx = rng.Next(files.Length);
            PlayAudioFile(files[idx]);
        }

        private void PlayAudioFile(string filePath)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        Console.WriteLine($"[DDR] Audio file not found: {filePath}");
                        return;
                    }
                    using (var audioFile = new AudioFileReader(filePath))
                    using (var outputDevice = new WaveOutEvent())
                    {
                        outputDevice.Init(audioFile);
                        outputDevice.Play();
                        while (outputDevice.PlaybackState == PlaybackState.Playing)
                        {
                            System.Threading.Thread.Sleep(10);
                        }
                    }
                    Console.WriteLine($"[DDR] Played: {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DDR] Error playing audio file {filePath}: {ex.Message}");
                }
            });
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
        }
    }
}
