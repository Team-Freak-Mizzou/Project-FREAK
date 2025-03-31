using Project_FREAK.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Project_FREAK.Views
{
    /// <summary>
    /// Interaction logic for SensorCheckWindow.xaml
    /// </summary>
    public partial class SensorCheckWindow : Window
    {
        public SensorCheckWindow()
        {
            InitializeComponent();
            //begin to subscribe to labjack data updates through action
            LabJackManager.Instance.DataUpdated += UpdateSensors;
            //check if a device is connected
            UpdateLabjackStatus();

        }
        private void UpdateLabjackStatus()
        {
            if (LabJackManager.Instance.IsDemo())
            {
                //in demo mode, update UI
                DemoModeStatus.Text = "❌";
                DemoModeStatus.Foreground = Brushes.Red;
                DemoModeEnabledDisabled.Text = "Disconnected";
                PressureTransducerStatus.Text = "❌";
                PressureTransducerStatus.Foreground = Brushes.Red;
                LoadCellStatus.Text = "❌";
                LoadCellStatus.Foreground = Brushes.Red;
            }
            else
            {
                DemoModeEnabledDisabled.Text = "Connected";
                DemoModeStatus.Foreground = Brushes.LimeGreen;
                DemoModeStatus.Text = "✔️";
                PressureTransducerStatus.Text = "✔️";
                PressureTransducerStatus.Foreground = Brushes.LimeGreen;
                LoadCellStatus.Text = "✔️";
                LoadCellStatus.Foreground = Brushes.LimeGreen;

            }
        }
        private void UpdateSensors(double thrustVoltage, double calibratedThrust, double pressureVoltage, double calibratedPressure)
        {
            //on an update, invoke ui thread to update with correct values. This will be replaced later with graphs of data.
            Dispatcher.Invoke(() => {
                LoadCellVoltage.Text = $"Voltage: {thrustVoltage:F6} V";
                PressureTransducerVoltage.Text = $"Voltage: {pressureVoltage:F2} V";
            });
        }

        private void ReconnectButton_Click(object sender, RoutedEventArgs e)
        {
            LabJackManager.Instance.CloseDevice();
            _ = LabJackManager.Instance; //create LabJack device handle to reconnect to
            UpdateLabjackStatus();
            LabJackManager.Instance.DataUpdated += UpdateSensors;
            
        }
    }
}
