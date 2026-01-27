namespace Rotatonator
{
    /// <summary>
    /// Configuration for text-to-speech audio alerts
    /// </summary>
    public class AudioAlertConfig
    {
        // On heal cast alerts
        public bool AnnounceHealerNumber { get; set; } = false;
        public bool AnnounceHealerName { get; set; } = false;
        public bool AnnounceTargetName { get; set; } = false;

        // On my turn alerts
        public bool AnnounceYoureNext { get; set; } = false;
        public bool AnnounceCastNow { get; set; } = false;
        
        // Audio beeps
        public bool EnableAudioBeep { get; set; } = false;
    }
}
