using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Project_FREAK.Controllers;
using Microsoft.Win32;
using System.Collections.Concurrent;
using System.IO;

namespace Project_FREAK.Views
{
    public partial class RecordPage : Page
    {
        private WebcamManager _webcamManager = null!;
        private GraphManager _graphManager = null!;
        private LabJackManager _labjackManager = null!;
        private DataRecorder _dataRecorder = null!; // Records data points
        private CountdownService _countdownService = null!;
        private readonly Stopwatch _stopwatch = new(); // Stopwatch to track elapsed time
        private readonly Stopwatch _graphUpdateStopwatch = new(); // Stopwatch to control graph update rate
        private readonly ConcurrentQueue<(double time, double thrust, double pressure, double thrustVoltage, double pressureVoltage)> _dataQueue = new(); // Queue to store data points
        private SensorCheckWindow? _sensorCheckWindow; // Window for sensor check

        private bool _isSaving = false;
        private bool _isRecording = false; // Flag to indicate if recording is in progress
        private const int graphFPS = 30;

        private string? _currentSessionFolder;
        private string? _currentVideoPath;

        public RecordPage()
        {
            InitializeComponent();
            Loaded += RecordPage_Loaded;
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
            // Initialize managers asynchronously
            await InitializeManagersAsync();

            _stopwatch.Start();
            SetupCameraEvents();
            await InitializeCamera();
        }

        private Task InitializeManagersAsync()
        {
            _graphManager = new GraphManager(ThrustGraph, PressureGraph);
            _labjackManager = LabJackManager.Instance; // Ensure LabJackManager is thread-safe
            _webcamManager = new WebcamManager(Dispatcher);
            _dataRecorder = new DataRecorder();
            _countdownService = new CountdownService(Dispatcher);

            // Subscribe to events
            Unloaded += RecordPage_Unloaded;
            _labjackManager.DataUpdated += UpdateGraphs;
            _countdownService.CountdownUpdated += UpdateCountdownDisplay;
            _countdownService.CountdownFinished += HandleCountdownCompletion;

            SetupTimer();
            SubscribeToSettingsChanges();
            _graphUpdateStopwatch.Start();

            return Task.CompletedTask; // Return completed task
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
                StartVideoRecording(); // Start recording after ignition
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
            if (_labjackManager.GetArmedStatus() && !_isRecording)
            {
                _countdownService.ToggleCountdown(5); // Start the countdown if the igniter is armed
            } 
            else if (_isRecording)
            {
                _webcamManager.StopRecording();
                _isRecording = false;
                StartTestTextBlock.Text = "Start";
                StartButton.Background = Brushes.Green;

                _currentSessionFolder = null;
                _currentVideoPath = null;
            }
        }

        // Handles the click event of the Save button
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSessionFolder == null)
            {
                MessageBox.Show("No active test session to save!", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _isSaving = true;
                var jsonFileName = $"data_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json";
                var jsonPath = Path.Combine(_currentSessionFolder, jsonFileName);

                _dataRecorder.ExportToJson(jsonPath);

                MessageBox.Show($"Data and video saved in:\n{_currentSessionFolder}", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isSaving = false;
            }
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

        private void StartVideoRecording()
        {
            try
            {
                // Create timestamped folder
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _currentSessionFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "RocketTests",
                    timestamp);

                Directory.CreateDirectory(_currentSessionFolder);

                // Generate filenames
                var videoFileName = $"test_{timestamp}.mp4";
                _currentVideoPath = Path.Combine(_currentSessionFolder, videoFileName);

                _webcamManager.StartRecording(_currentVideoPath);
                _isRecording = true;
                StartTestTextBlock.Text = "Stop Recording";
                StartButton.Background = Brushes.Red;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting recording: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}