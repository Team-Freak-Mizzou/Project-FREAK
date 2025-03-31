using System;
using System.IO;
using System.Text.Json;
using System.ComponentModel;

namespace Project_FREAK.Controllers;

public class SettingsManager : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? AppliedSettingsChanged;

    // Paths to the settings files
    private static readonly string SavedSettingsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Assets", "CONFIG", "saved_settings.json");

    private static readonly string AppliedSettingsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Assets", "CONFIG", "applied_settings.json");

    // Settings data
    private SettingsData _appliedSettings = new();
    public SettingsData AppliedSettings
    {
        get => _appliedSettings;
        private set
        {
            _appliedSettings = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AppliedSettings))); // Notify when AppliedSettings changes
        }
    }

    public SettingsManager()
    {
        LoadSavedSettings();
        LoadAppliedSettings();
    }

    // Loads saved settings from disk
    public void LoadSavedSettings()
    {
        try
        {
            if (File.Exists(SavedSettingsPath))
            {
                var json = File.ReadAllText(SavedSettingsPath);
                var settings = JsonSerializer.Deserialize<SettingsData>(json);
                AppliedSettings = settings ?? new SettingsData(); // Set AppliedSettings to the loaded settings or a new instance if null
            }
        }
        catch { /* Handle errors */ }
    }

    // Saves the applied settings to disk
    public void SaveAppliedSettingsToDisk()
    {
        try
        {
            var json = JsonSerializer.Serialize(AppliedSettings);
            Directory.CreateDirectory(Path.GetDirectoryName(SavedSettingsPath)!); // Ensure the directory exists
            File.WriteAllText(SavedSettingsPath, json);
        }
        catch { /* Handle errors */ }
    }

    // Updates the applied settings and saves them to disk
    public void UpdateAppliedSettings(SettingsData newSettings)
    {
        AppliedSettings = newSettings;
        AppliedSettingsChanged?.Invoke(this, EventArgs.Empty); // Trigger the AppliedSettingsChanged event

        // Save applied settings to session file
        try
        {
            var json = JsonSerializer.Serialize(newSettings);
            Directory.CreateDirectory(Path.GetDirectoryName(AppliedSettingsPath)!); // Ensure the directory exists
            File.WriteAllText(AppliedSettingsPath, json);
        }
        catch { /* Handle errors */ }
    }

    // Loads the applied settings from disk
    public void LoadAppliedSettings()
    {
        try
        {
            if (File.Exists(AppliedSettingsPath))
            {
                var json = File.ReadAllText(AppliedSettingsPath);
                var settings = JsonSerializer.Deserialize<SettingsData>(json);
                AppliedSettings = settings ?? new SettingsData(); // Set AppliedSettings to the loaded settings or a new instance if null
            }
        }
        catch { /* Handle errors */ }
    }

    // Class representing the settings data
    public class SettingsData
    {
        public bool DemoModeEnabled { get; set; } // Indicates if demo mode is enabled
        public string RtspUrl { get; set; } = "rtsp://admin:MSPMOTORTEST2025@192.168.20.4:554/cam/realmonitor?channel=1&subtype=0"; // Default RTSP URL
    }
}
