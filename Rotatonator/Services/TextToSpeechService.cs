using System;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace Rotatonator
{
    /// <summary>
    /// Service for text-to-speech audio alerts using System.Speech
    /// </summary>
    public class TextToSpeechService : IDisposable
    {
        private readonly SpeechSynthesizer synthesizer;
        private bool disposed = false;

        public TextToSpeechService()
        {
            synthesizer = new SpeechSynthesizer();
            synthesizer.SetOutputToDefaultAudioDevice();
            
            // Configure voice settings
            synthesizer.Rate = 1; // Normal speed (-10 to 10)
            synthesizer.Volume = 100; // Full volume (0 to 100)
        }

        /// <summary>
        /// Announce healer position number (e.g., "One", "Two", "Three")
        /// </summary>
        public void AnnounceHealerNumber(int position)
        {
            if (position < 1 || position > 35) return;

            Task.Run(() =>
            {
                try
                {
                    string text = position <= 20 ? NumberToWord(position) : position.ToString();
                    synthesizer.Speak(text);
                    Console.WriteLine($"[TTS] Announced healer number: {text}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TTS] Error announcing healer number: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Announce healer name
        /// </summary>
        public void AnnounceHealerName(string healerName)
        {
            if (string.IsNullOrWhiteSpace(healerName)) return;

            Task.Run(() =>
            {
                try
                {
                    synthesizer.Speak(healerName);
                    Console.WriteLine($"[TTS] Announced healer name: {healerName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TTS] Error announcing healer name: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Announce target name
        /// </summary>
        public void AnnounceTargetName(string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName)) return;

            Task.Run(() =>
            {
                try
                {
                    synthesizer.Speak(targetName);
                    Console.WriteLine($"[TTS] Announced target name: {targetName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TTS] Error announcing target name: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Announce "Next" when player is up next
        /// </summary>
        public void AnnounceYoureNext()
        {
            Task.Run(() =>
            {
                try
                {
                    synthesizer.Speak("Next");
                    Console.WriteLine($"[TTS] Announced: Next");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TTS] Error announcing you're next: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Announce "Go" when player should cast now
        /// </summary>
        public void AnnounceCastNow()
        {
            Task.Run(() =>
            {
                try
                {
                    synthesizer.Speak("Go");
                    Console.WriteLine($"[TTS] Announced: Go");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TTS] Error announcing cast now: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Convert number (1-20) to word
        /// </summary>
        private string NumberToWord(int number)
        {
            string[] words = new[]
            {
                "", "One", "Two", "Three", "Four", "Five",
                "Six", "Seven", "Eight", "Nine", "Ten",
                "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen",
                "Sixteen", "Seventeen", "Eighteen", "Nineteen", "Twenty"
            };

            return number >= 1 && number <= 20 ? words[number] : number.ToString();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                synthesizer?.Dispose();
                disposed = true;
            }
        }
    }
}
