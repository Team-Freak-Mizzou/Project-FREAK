using System;
using System.IO;
using System.Text.Json;
using Project_FREAK.Controllers;
using Xunit;

namespace Project_FREAK.Tests
{
    public class SettingsManagerTests : IDisposable
    {
        readonly string cfg = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Assets", "CONFIG");

        public SettingsManagerTests()
        {
            if (Directory.Exists(cfg)) Directory.Delete(cfg, true);
        }

        public void Dispose()
        {
            if (Directory.Exists(cfg)) Directory.Delete(cfg, true);
        }

        [Fact]
        public void DefaultConstructor_NoFiles_AppliedSettingsHaveDefaults()
        {
            var mgr = new SettingsManager();
            Assert.False(mgr.AppliedSettings.DemoModeEnabled);
            Assert.StartsWith("rtsp://admin", mgr.AppliedSettings.RtspUrl);
        }

        [Fact]
        public void Constructor_LoadsSavedSettings_WhenSavedSettingsExist()
        {
            Directory.CreateDirectory(cfg);
            var exp = new SettingsManager.SettingsData
            {
                DemoModeEnabled = true,
                RtspUrl = "rtsp://test"
            };
            File.WriteAllText(
                Path.Combine(cfg, "saved_settings.json"),
                JsonSerializer.Serialize(exp)
            );

            var mgr = new SettingsManager();
            Assert.True(mgr.AppliedSettings.DemoModeEnabled);
            Assert.Equal("rtsp://test", mgr.AppliedSettings.RtspUrl);
        }

        [Fact]
        public void UpdateAppliedSettings_RaisesEventAndWritesFiles()
        {
            Directory.CreateDirectory(cfg);
            var mgr = new SettingsManager();
            bool fired = false;
            mgr.AppliedSettingsChanged += (_, __) => fired = true;

            var ns = new SettingsManager.SettingsData
            {
                DemoModeEnabled = true,
                RtspUrl = "rtsp://new"
            };
            mgr.UpdateAppliedSettings(ns);
            Assert.True(fired);

            var applied = JsonSerializer.Deserialize<SettingsManager.SettingsData>(
                File.ReadAllText(Path.Combine(cfg, "applied_settings.json"))
            )!;
            Assert.True(applied.DemoModeEnabled);

            mgr.SaveAppliedSettingsToDisk();
            var saved = JsonSerializer.Deserialize<SettingsManager.SettingsData>(
                File.ReadAllText(Path.Combine(cfg, "saved_settings.json"))
            )!;
            Assert.Equal("rtsp://new", saved.RtspUrl);
        }
    }
}
