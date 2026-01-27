using System.Windows;

namespace Rotatonator
{
    public partial class AudioAlertConfigDialog : Window
    {
        public AudioAlertConfig Config { get; private set; }

        public AudioAlertConfigDialog(AudioAlertConfig currentConfig)
        {
            InitializeComponent();
            Config = currentConfig;

            // Load current settings
            HealerNumberCheckBox.IsChecked = currentConfig.AnnounceHealerNumber;
            HealerNameCheckBox.IsChecked = currentConfig.AnnounceHealerName;
            TargetNameCheckBox.IsChecked = currentConfig.AnnounceTargetName;
            YoureNextCheckBox.IsChecked = currentConfig.AnnounceYoureNext;
            CastNowCheckBox.IsChecked = currentConfig.AnnounceCastNow;
            AudioBeepCheckBox.IsChecked = currentConfig.EnableAudioBeep;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Update config with checkbox values
            Config.AnnounceHealerNumber = HealerNumberCheckBox.IsChecked ?? false;
            Config.AnnounceHealerName = HealerNameCheckBox.IsChecked ?? false;
            Config.AnnounceTargetName = TargetNameCheckBox.IsChecked ?? false;
            Config.AnnounceYoureNext = YoureNextCheckBox.IsChecked ?? false;
            Config.AnnounceCastNow = CastNowCheckBox.IsChecked ?? false;
            Config.EnableAudioBeep = AudioBeepCheckBox.IsChecked ?? false;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
