using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Project_FREAK.Controllers
{
    public class WebcamManager : IDisposable
    {
        private VideoCapture? _capture; // Manages video capture from a camera or video file
        private CancellationTokenSource? _cts; // Token source to cancel the capture loop
        public event Action<BitmapSource>? FrameReceived; // Event triggered when a new frame is received
        private readonly Dispatcher _dispatcher; // Dispatcher to ensure UI updates occur on the main thread
        public event Action<string>? CameraError; // Event triggered when a camera error occurs

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject); // External method to delete GDI objects

        public WebcamManager(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        // Initializes the webcam manager asynchronously
        public async Task InitializeAsync(bool demoMode, string? rtspUrl = null)
        {
            try
            {
                if (demoMode)
                    _capture = new VideoCapture(0); // Open the default camera in demo mode
                else if (!string.IsNullOrEmpty(rtspUrl))
                    _capture = new VideoCapture(rtspUrl); // Open the RTSP stream if URL is provided

                if (_capture?.IsOpened() == true)
                {
                    //_capture.Set(VideoCaptureProperties.BufferSize, 1); // TODO REMOVE THIS LINE
                    StartCaptureLoop(); // Start capturing frames
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Camera initialization failed", ex); // Propagate the exception
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
                while (!_cts!.IsCancellationRequested && _capture?.IsOpened() == true)
                {
                    try
                    {
                        using var frame = new Mat(); // Create a new frame
                        if (!_capture.Read(frame))
                            break;
                        if (!frame.Empty())
                        {
                            using var bmp = frame.ToBitmap(); // Convert the frame to a bitmap
                            var hBitmap = bmp.GetHbitmap();
                            var bitmap = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()); // Create a BitmapSource from the handle
                            bitmap.Freeze();
                            DeleteObject(hBitmap); // Delete the GDI object
                            _dispatcher.Invoke(() => FrameReceived?.Invoke(bitmap)); // Invoke the FrameReceived event on the UI thread
                        }
                        await Task.Delay((int)(1000 / Math.Max(_capture?.Fps ?? 30, 1))); // Delay to match the frame rate
                    }
                    catch (Exception ex)
                    {
                        CameraError?.Invoke($"Frame error: {ex.Message}");
                    }
                }
            }, _cts.Token);
        }

        // Disposes the webcam manager and releases resources
        public void Dispose()
        {
            _cts?.Cancel(); // Cancel the capture loop
            _capture?.Dispose(); // Dispose the video capture
            GC.SuppressFinalize(this); // Suppress finalization
        }
    }
}
