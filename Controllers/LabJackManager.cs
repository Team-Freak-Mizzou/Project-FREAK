using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LabJack;
using System.Threading;
using System.Reflection.Metadata;
using System.Xml.Linq;

namespace Project_FREAK.Controllers
{
    public class LabJackManager
    {
        private int _handle = -1; // Handle for the LabJack device
        private bool _isDemo = true; // Indicates if the device is in demo mode
        private bool _isIgniterArmed = false; // Indicates if the igniter is armed
        private static LabJackManager _instance; // Singleton instance
        private bool _isReading; // Indicates if data is being read from the device
        public event Action<double, double, double, double> DataUpdated; // Event triggered when data is updated
        private Timer _returnTimer; // Timer to return DAC0 to zero

        private LabJackManager()
        {
            InitalizeLabJack(); // Initialize the LabJack device
            StartReading(); // Start reading data from the device
        }

        // Singleton instance property
        public static LabJackManager Instance => _instance ??= new LabJackManager();

        // Initializes the LabJack device
        private void InitalizeLabJack()
        {
            try
            {
                LJM.OpenS("ANY", "ANY", "ANY", ref _handle);
                _isDemo = false;
                // LabJack T4 configuration
                // Resolution index = 0 (default)
                // Settling = 0 (auto)
                string[] aNames = new string[] { "AIN0_RESOLUTION_INDEX", "AIN5_RESOLUTION_INDEX",
                            "AIN0_SETTLING_US", "AIN5_SETTLING_US" };
                double[] aValues = new double[] { 0, 0, 0, 0 };
                int numFrames = aNames.Length;
                int errorAddress = -1;
                LJM.eWriteNames(_handle, numFrames, aNames, aValues, ref errorAddress);
            }
            catch (LJM.LJMException exception)
            {
                // Open in demo mode if device not found
                LJM.OpenS("ANY", "ANY", "LJM_DEMO_MODE", ref _handle);
                _isDemo = true;
            }
        }

        // Returns the handle of the LabJack device
        public int GetHandle()
        {
            return _handle;
        }

        // Checks if currently running in demo mode
        public bool IsDemo()
        {
            return _isDemo;
        }

        // Returns the armed status of the igniter
        public bool GetArmedStatus() => _isIgniterArmed;

        // Arms or disarms the igniter
        public void ArmDisarmIgniter() => _isIgniterArmed = !_isIgniterArmed;

        // Ignites the motor by writing 5V to the igniter wire
        public void IgniteMotor()
        {
            LJM.eWriteName(_handle, "DAC0", 5.0);
            // After 2.5 seconds, set DAC0 back to 0V
            _returnTimer = new Timer(ReturnToZero, null, 2500, Timeout.Infinite);
        }

        // Returns DAC0 to zero voltage
        private void ReturnToZero(object state)
        {
            LJM.eWriteName(_handle, "DAC0", 0.0);
            _returnTimer?.Dispose();
            _returnTimer = null;
        }

        // Starts reading data from the LabJack device
        private void StartReading()
        {
            _isReading = true;
            // Spin up a thread from the thread pool to read data from LabJack
            Task.Run(() =>
            {
                while (_isReading)
                {
                    ReadData();
                }
            });
        }

        // Reads data from the LabJack device
        private void ReadData()
        {
            if (_handle > 0)
            {
                try
                {
                    // Read in voltages from their specified input ports (TODO: make this customizable)
                    double thrustVoltage = 0, pressureVoltage = 0;
                    LJM.eReadName(_handle, "AIN5", ref thrustVoltage);
                    LJM.eReadName(_handle, "AIN0", ref pressureVoltage);

                    // Convert voltage to thrust
                    // Load = RatedLoad *(VAIN- Voffset)/ (gain * Sensitivity * Vexc) in kilos
                    // Our load cell's sensitivity is 2 mV/V
                    // Excitation voltage available on Input 6.
                    double Vexc = 0;
                    // 1.25 and 201 taken from dip switches active on the LJ-Tick-InAmp. For more info see https://labjack.com/pages/support?doc=/datasheets/accessories/ljtick-inamp-datasheet/
                    double loadAdjVoltage = (thrustVoltage - 1.25) / 201;
                    LJM.eReadName(_handle, "AIN6", ref Vexc);
                    // Use below lines to implement dynamic calibration. For now calibration values will be static
                    // load = 500 * (loadVoltage - 1.25) / (201 * 2 * Vexc)
                    // load = 500 * loadAdjVoltage / (2 * 2.5)
                    double calibratedLoad = 100387.5 * loadAdjVoltage - 3.8069375;
                    calibratedLoad *= 9.81; // Convert from kg to N

                    // Convert voltage to pressure
                    // Pressure transducer 0.5 V = 0 PSI, 4.5 V = 1600 PSI, linear scaling
                    double pressureAdjustedVoltage = pressureVoltage - 0.5;
                    double pressure = pressureAdjustedVoltage * 4;

                    // Invoke action, pass values onto subscribers
                    DataUpdated?.Invoke(thrustVoltage, calibratedLoad, pressureVoltage, pressure);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Error reading values: {exception.Message}");
                }
            }
        }

        // Closes the LabJack device
        public void CloseDevice()
        {
            if (_handle > 0)
            {
                // Stop reading, and close device
                _isReading = false;
                LJM.Close(_handle);
                _instance = null;
                _handle = -1;
            }
        }

        // Disposes the LabJackManager and closes the device
        public void Dispose() => CloseDevice();
    }
    }
