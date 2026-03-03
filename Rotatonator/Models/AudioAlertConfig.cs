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

        // Alert if an NPC is the target of a complete heal
        public bool AlertOnNpcCompleteHeal { get; set; } = true;

        // On my turn alerts
        public bool AnnounceYoureNext { get; set; } = false;
        public bool AnnounceCastNow { get; set; } = false;
        
        // Audio beeps
        public bool EnableAudioBeep { get; set; } = false;
    }
}
