using System;
using System.Windows.Threading;

namespace Project_FREAK.Controllers
{
    public class CountdownService : IDisposable
    {
        private DispatcherTimer _timer; // Timer to manage countdown
        private int _remainingSeconds; // Remaining seconds in the countdown
        public event Action<string>? CountdownUpdated; // Event triggered when the countdown is updated
        public event Action? CountdownFinished; // Event triggered when the countdown finishes

        public CountdownService(Dispatcher dispatcher)
        {
            _timer = new DispatcherTimer(DispatcherPriority.Normal, dispatcher); // Initialize the timer with the specified dispatcher
            _timer.Tick += Timer_Tick; // Subscribe to the Tick event
        }

        // Initializes the timer interval
        public void InitializeTimer(TimeSpan interval) => _timer.Interval = interval;

        // Toggles the countdown timer on or off
        public void ToggleCountdown(int seconds)
        {
            _remainingSeconds = seconds;
            if (_timer.IsEnabled)
            {
                _timer.Stop();
            }
            else
            {
                _timer.Start();
            }
            CountdownUpdated?.Invoke($"00:{_remainingSeconds}"); // Update the countdown display
        }

        // Handles the Tick event of the timer
        private void Timer_Tick(object? sender, EventArgs e)
        {
            _remainingSeconds--; // Decrement the remaining seconds
            CountdownUpdated?.Invoke($"00:{_remainingSeconds}"); // Update the countdown display

            if (_remainingSeconds <= 0)
            {
                _timer.Stop();
                CountdownFinished?.Invoke(); // Trigger the countdown finished event
            }
        }

        // Disposes the timer
        public void Dispose() => _timer.Stop();
    }
}