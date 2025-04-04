using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OpenCvSharp.WpfExtensions;

namespace Project_FREAK.Controllers
{
    public partial class WebcamManager : IDisposable
    {
        private VideoCapture? _capture; // Manages video capture from a camera or video file
        private CancellationTokenSource? _cts; // Token source to cancel the capture loop
        public event Action<BitmapSource>? FrameReceived; // Event triggered when a new frame is received
        private readonly Dispatcher _dispatcher; // Dispatcher to ensure UI updates occur on the main thread
        public event Action<string>? CameraError; // Event triggered when a camera error occurs

        private VideoWriter? _videoWriter;
        public int FrameWidth { get; private set; }
        public int FrameHeight { get; private set; }
        public double Fps { get; private set; }

        public WebcamManager(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        // Initializes the webcam manager asynchronously
        public async Task InitializeAsync(bool demoMode, string? rtspUrl = null)
        {
            try
            {
                await Task.Run(() =>
                {
                    if (demoMode)
                        _capture = new VideoCapture(0);
                    else if (!string.IsNullOrEmpty(rtspUrl))
                        _capture = new VideoCapture(rtspUrl);
                });

                if (_capture?.IsOpened() == true)
                {
                    FrameWidth = (int)_capture.Get(VideoCaptureProperties.FrameWidth);
                    FrameHeight = (int)_capture.Get(VideoCaptureProperties.FrameHeight);
                    Fps = _capture.Get(VideoCaptureProperties.Fps);
                    if (Fps <= 0 || Fps > 60)
                    {
                        // Default to 30 FPS if invalid or unrealistic value
                        Fps = 30;
                    }
                    StartCaptureLoop();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Camera initialization failed", ex);
            }
        }

        // Reinitializes the webcam manager asynchronously
        public async Task ReinitializeAsync(bool demoMode, string? rtspUrl = null)
        {
            Dispose(); // Dispose the current capture
            await InitializeAsync(demoMode, rtspUrl); // Reinitialize with new parameters
        }

        // Starts the capture loop to continuously capture frames
        private void StartCaptureLoop()
        {
            _cts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                var targetFrameTime = TimeSpan.FromSeconds(1 / Fps);
                var sw = new Stopwatch();

                while (!_cts!.IsCancellationRequested && _capture?.IsOpened() == true)
                {
                    sw.Restart();
                    try
                    {
                        using var frame = new Mat();
                        if (!_capture.Read(frame) || frame.Empty()) continue;

                        _videoWriter?.Write(frame);

                        // Convert Mat to WPF's WriteableBitmap directly
                        var bitmap = frame.ToWriteableBitmap();
                        bitmap.Freeze();

                        _dispatcher.Invoke(() => FrameReceived?.Invoke(bitmap));

                        var processingTime = sw.Elapsed;
                        var delay = targetFrameTime - processingTime;
                        if (delay > TimeSpan.Zero) await Task.Delay(delay);
                    }
                    catch (Exception ex)
                    {
                        CameraError?.Invoke($"Frame error: {ex.Message}");
                    }
                }
            }, _cts.Token);
        }

        // Starts recording video to the specified file path
        public void StartRecording(string filePath)
        {
            if (_capture == null || !_capture.IsOpened())
                throw new InvalidOperationException("Camera not initialized.");

            int fourcc = VideoWriter.FourCC('a', 'v', 'c', '1');    // Default to AVC codec
            if (!IsFourccSupported(fourcc))
            {
                fourcc = VideoWriter.FourCC('m', 'p', '4', 'v');    // Fallback to MP4V codec
            }
            _videoWriter = new VideoWriter(     // Initialize the video writer
                filePath,
                fourcc,
                Fps,
                new OpenCvSharp.Size(FrameWidth, FrameHeight)
            );

            if (!_videoWriter.IsOpened())
                throw new Exception("Failed to initialize video writer.");
        }

        // Checks if the specified FourCC codec is supported
        private static bool IsFourccSupported(int fourcc)
        {
            try
            {
                using var testWriter = new VideoWriter("test.mp4", fourcc, 30, new OpenCvSharp.Size(640, 480));
                return testWriter.IsOpened();
            }
            catch { return false; }
        }

        // Stops the video recording
        public void StopRecording()
        {
            _videoWriter?.Release();
            _videoWriter = null;
        }

        // Disposes the webcam manager and releases resources
        public void Dispose()
        {
            _cts?.Cancel();
            _capture?.Dispose();
            _videoWriter?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
