using System;
using System.Collections.Generic;

namespace Rotatonator
{
    public class RotationConfig
    {
        public List<string> Healers { get; set; } = new();
        public string PlayerName { get; set; } = "";
        public string ChainPrefix { get; set; } = "D&D";
        public TimeSpan ChainInterval { get; set; } = TimeSpan.FromSeconds(6);
        public bool EnableVisualAlerts { get; set; } = true;
        public bool EnableAudioBeep { get; set; } = false;
        public bool EnableAutoCast { get; set; } = false;
        public string CastHotkey { get; set; } = "1";
        public AudioAlertConfig AudioAlerts { get; set; } = new AudioAlertConfig();
        public bool EnableDDRMode { get; set; } = false;
    }
}
