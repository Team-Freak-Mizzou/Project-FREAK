using System.IO;
using System.Text.Json;
using Project_FREAK.Controllers;
using Xunit;

namespace Project_FREAK.Tests.Tests
{
    public class DataRecorderTests
    {
        [Fact]
        public void AddDataPoint_AppendsToAllLists()
        {
            var dr = new DataRecorder();
            dr.AddDataPoint(1.0, 2.0, 3.0, 4.0, 5.0);

            Assert.Single(dr.TimeData);
            Assert.Equal(1.0, dr.TimeData[0]);
            Assert.Equal(2.0, dr.ThrustData[0]);
            Assert.Equal(3.0, dr.PressureData[0]);
            Assert.Equal(4.0, dr.RawThrustVoltages[0]);
            Assert.Equal(5.0, dr.RawPressureVoltages[0]);
        }

        [Fact]
        public void ExportToJson_CreatesValidFile()
        {
            var dr = new DataRecorder();
            dr.AddDataPoint(0.5, 1.5, 2.5, 3.5, 4.5);

            var tmp = Path.GetTempFileName();
            dr.ExportToJson(tmp);

            using var doc = JsonDocument.Parse(File.ReadAllText(tmp));
            Assert.True(doc.RootElement.TryGetProperty("time_values_seconds", out _));
            Assert.True(doc.RootElement.TryGetProperty("thrust_values_N", out _));
        }
    }
}
