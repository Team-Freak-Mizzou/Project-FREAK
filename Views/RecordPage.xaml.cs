using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Project_FREAK.Controllers;
using Microsoft.Win32;
using System.Collections.Concurrent;

namespace Project_FREAK.Views
{
    public partial class RecordPage : Page
    {
        private readonly WebcamManager _webcamManager;
        private readonly GraphManager _graphManager;
        private readonly LabJackManager _labjackManager;
        private readonly DataRecorder _dataRecorder; // Records data points
        private readonly CountdownService _countdownService;
        private readonly Stopwatch _stopwatch = new(); // Stopwatch to track elapsed time
        private readonly Stopwatch _graphUpdateStopwatch = new(); // Stopwatch to control graph update rate
        private readonly ConcurrentQueue<(double time, double thrust, double pressure, double thrustVoltage, double pressureVoltage)> _dataQueue = new(); // Queue to store data points
        private SensorCheckWindow? _sensorCheckWindow; // Window for sensor check

        private DateTime _startTime; // Start time of the recording
        private bool _isSaving = false;
        private const int graphFPS = 30;

        public RecordPage()
        {
            InitializeComponent();

            // Initialize managers
            _graphManager = new GraphManager(ThrustGraph, PressureGraph);
            _labjackManager = LabJackManager.Instance;
            _webcamManager = new WebcamManager(Dispatcher);
            _dataRecorder = new DataRecorder();
            _countdownService = new CountdownService(Dispatcher);

            // Setup event subscriptions
            Loaded += RecordPage_Loaded;
            Unloaded += RecordPage_Unloaded;
            _labjackManager.DataUpdated += UpdateGraphs;
            _countdownService.CountdownUpdated += UpdateCountdownDisplay;
            _countdownService.CountdownFinished += HandleCountdownCompletion;

            SetupTimer();
            SubscribeToSettingsChanges();

            _graphUpdateStopwatch.Start(); // Start the stopwatch for controlling graph updates
        }

        // Sets up the countdown timer interval
        private void SetupTimer()
        {
            _countdownService.InitializeTimer(TimeSpan.FromSeconds(1));
        }

        // Subscribes to settings changes
        private void SubscribeToSettingsChanges()
        {
            ((App)Application.Current).SettingsManager.AppliedSettingsChanged += SettingsChangedHandler;
        }

        // Handles the Loaded event of the page
        private async void RecordPage_Loaded(object sender, RoutedEventArgs e)
        {
            _stopwatch.Start();
            SetupCameraEvents();
            await InitializeCamera();
        }

        // Initializes the camera
        private async Task InitializeCamera()
        {
            try
            {
                LoadingTextBlock.Visibility = Visibility.Visible; // Show loading text
                var settings = ((App)Application.Current).SettingsManager.AppliedSettings;
                await _webcamManager.InitializeAsync(settings.DemoModeEnabled, settings.RtspUrl);
                LoadingTextBlock.Visibility = Visibility.Collapsed; // Hide only on success
            }
            catch (Exception ex)
            {
                LoadingTextBlock.Text = $"Camera Error: {ex.Message}"; // Show detailed error
                LoadingTextBlock.Visibility = Visibility.Visible;
            }
        }

        // Sets up camera events
        private void SetupCameraEvents()
        {
            _webcamManager.FrameReceived += bitmap => WebcamImage.Source = bitmap; // Update webcam image
            _webcamManager.CameraError += message =>
                Dispatcher.Invoke(() => LoadingTextBlock.Text = message); // Show camera error message
        }

        // Updates the graphs with new data points
        private void UpdateGraphs(double thrustVoltage, double thrust, double pressureVoltage, double pressure)
        {
            if (_isSaving) return; // Skip if data is being saved

            var elapsedTime = _stopwatch.Elapsed.TotalSeconds; // Get elapsed time

            // Store data point in queue incase we don't update the graph this frame
            _dataQueue.Enqueue((elapsedTime, thrust, pressure, thrustVoltage, pressureVoltage));

            // Only update graphs at specified frame rate
            if (_graphUpdateStopwatch.ElapsedMilliseconds >= 1000 / graphFPS)
            {
                Dispatcher.Invoke(() =>
                {
                    while (_dataQueue.TryDequeue(out var dataPoint))
                    {
                        _dataRecorder.AddDataPoint(dataPoint.time, dataPoint.thrust, dataPoint.pressure, dataPoint.thrustVoltage, dataPoint.pressureVoltage); // Add data point to recorder
                        _graphManager.AddDataPoint(dataPoint.time, dataPoint.thrust, dataPoint.pressure); // Add data point to graphs
                    }
                    _graphUpdateStopwatch.Restart(); // Restart the stopwatch for graph updates
                });
            }
        }

        // Updates the countdown display
        private void UpdateCountdownDisplay(string timeText)
        {
            StartTestTextBlock.Text = timeText; // Update countdown text
        }

        // Handles the completion of the countdown
        private void HandleCountdownCompletion()
        {
            Dispatcher.Invoke(() =>
            {
                StartTestTextBlock.Text = "Igniting Motor!"; // Update text to indicate motor ignition
                _labjackManager.IgniteMotor(); // Ignite the motor
            });
        }

        // Handles the click event of the Arm button
        private void ArmButton_Click(object sender, RoutedEventArgs e)
        {
            _labjackManager.ArmDisarmIgniter(); // Arm or disarm the igniter
            UpdateArmingUI(_labjackManager.GetArmedStatus()); // Update the UI to reflect the arming status
        }

        // Updates the UI to reflect the arming status
        private void UpdateArmingUI(bool isArmed)
        {
            ArmButton.Background = isArmed ? Brushes.Red : Brushes.Green;
            ArmTextBlock.Text = isArmed ? "Disarm" : "Arm"; // Update button text
            StartButton.Background = isArmed ? Brushes.Orange : Brushes.DarkGray; // Change start button color
            StartTestTextBlock.TextDecorations = isArmed ? null : TextDecorations.Strikethrough;
        }

        // Handles the click event of the Start Test button
        private void StartTestButton_Click(object sender, RoutedEventArgs e)
        {
            if (_labjackManager.GetArmedStatus())
            {
                _countdownService.ToggleCountdown(5); // Start the countdown if the igniter is armed
            }
        }

        // Handles the click event of the Save button
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _isSaving = true; // Set saving flag
            var saveDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                FileName = "test_data.json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                _dataRecorder.ExportToJson(saveDialog.FileName); // Export data to JSON file
                MessageBox.Show("Data saved successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information); // Show success message
            }
            _isSaving = false; // Reset saving flag
        }

        // Handles the Unloaded event of the page
        private void RecordPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Cleanup resources
            _stopwatch.Reset();
            _webcamManager.Dispose();
            _labjackManager.Dispose();
            _countdownService.Dispose();

            // Unsubscribe events
            ((App)Application.Current).SettingsManager.AppliedSettingsChanged -= SettingsChangedHandler;
            _labjackManager.DataUpdated -= UpdateGraphs;
        }

        // Handles settings changes
        private async void SettingsChangedHandler(object? sender, EventArgs e)
        {
            await _webcamManager.ReinitializeAsync(
                ((App)Application.Current).SettingsManager.AppliedSettings.DemoModeEnabled,
                ((App)Application.Current).SettingsManager.AppliedSettings.RtspUrl
            ); // Reinitialize the webcam with new settings
        }

        // Handles the click event of the Sensor Check button
        private void SensorCheckButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sensorCheckWindow == null)
            {
                _sensorCheckWindow = new SensorCheckWindow();
                _sensorCheckWindow.Closed += (s, args) => _sensorCheckWindow = null; // Reset the window reference when closed
                _sensorCheckWindow.Show(); // Show the sensor check window
            }
            else
            {
                _sensorCheckWindow.Activate(); // Activate the existing sensor check window
            }
        }
    }
}