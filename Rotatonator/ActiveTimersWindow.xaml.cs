using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Rotatonator
{
    public partial class ActiveTimersWindow : Window
    {
        private readonly RespawnTimerService timerService;
        private readonly DispatcherTimer refreshTimer;

        public ActiveTimersWindow(RespawnTimerService timerService)
        {
            InitializeComponent();
            this.timerService = timerService ?? throw new ArgumentNullException(nameof(timerService));

            // Refresh timer every second to update live remaining times
            refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            refreshTimer.Tick += (s, e) => RefreshTimerList();
            refreshTimer.Start();

            Loaded += (s, e) => RefreshTimerList();
            Closed += (s, e) => refreshTimer.Stop();
        }

        private void RefreshTimerList()
        {
            var selectedId = (TimersListView.SelectedItem as ActiveRespawnTimer)?.Id;
            var timers = timerService.GetAllTimers();
            TimersListView.ItemsSource = timers;

            if (selectedId != null)
            {
                TimersListView.SelectedItem = timers.FirstOrDefault(t => t.Id == selectedId);
            }
        }

        private async void ForceExpireButton_Click(object sender, RoutedEventArgs e)
        {
            if (TimersListView.SelectedItem is ActiveRespawnTimer selected)
            {
                ForceExpireButton.IsEnabled = false;
                try
                {
                    await timerService.ForceExpireTimerAsync(selected.Id);
                    RefreshTimerList();
                    MessageBox.Show(
                        $"Triggered expiration alert for {selected.MobName}! Follow-up notification posted to Discord.",
                        "Alert Triggered",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to trigger alert: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    ForceExpireButton.IsEnabled = true;
                }
            }
            else
            {
                MessageBox.Show("Please select a timer from the list first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (TimersListView.SelectedItem is ActiveRespawnTimer selected)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete the timer for {selected.MobName}?",
                    "Confirm Deletion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    timerService.RemoveTimer(selected.Id);
                    RefreshTimerList();
                }
            }
            else
            {
                MessageBox.Show("Please select a timer to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ClearCompletedButton_Click(object sender, RoutedEventArgs e)
        {
            timerService.ClearCompleted();
            RefreshTimerList();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
