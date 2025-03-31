using System.Collections.Generic;
using System.Text.Json;
using System.IO;

namespace Project_FREAK.Controllers
{
    public class DataRecorder
    {
        // Lists to store recorded data
        public List<double> TimeData { get; } = new();
        public List<double> ThrustData { get; } = new();
        public List<double> PressureData { get; } = new();
        public List<double> RawThrustVoltages { get; } = new();
        public List<double> RawPressureVoltages { get; } = new();

        // Adds a data point to the lists
        public void AddDataPoint(double time, double thrust, double pressure,
                               double thrustVoltage, double pressureVoltage)
        {
            TimeData.Add(time);
            ThrustData.Add(thrust);
            PressureData.Add(pressure);
            RawThrustVoltages.Add(thrustVoltage);
            RawPressureVoltages.Add(pressureVoltage);
        }

        // Exports the recorded data to a JSON file
        public void ExportToJson(string path)
        {
            var data = new
            {
                time_values_seconds = TimeData, // Time data in seconds
                thrust_values_N = ThrustData, // Thrust data in Newtons
                pressure_values_PSI = PressureData, // Pressure data in PSI
                load_cell_voltages_mv = RawThrustVoltages, // Load cell voltages in millivolts
                pressure_transducer_voltages_v = RawPressureVoltages // Pressure transducer voltages in volts
            };

            // Serialize the data to JSON and write to the specified file
            File.WriteAllText(path, JsonSerializer.Serialize(data,
                new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}