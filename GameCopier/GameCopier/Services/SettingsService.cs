using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GameCopier.Services
{
    public class SettingsService
    {
        private readonly string _configFilePath;
        private DriveDisplaySettings _settings;

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configFolder = Path.Combine(appDataPath, "GameCopier");
            Directory.CreateDirectory(configFolder);
            _configFilePath = Path.Combine(configFolder, "drivesettings.json");
            _settings = LoadSettings();
        }

        public DriveDisplaySettings GetSettings() => _settings;

        public void SaveSettings(DriveDisplaySettings settings)
        {
            _settings = settings;
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configFilePath, json);
                System.Diagnostics.Debug.WriteLine($"? Drive settings saved: {json}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error saving drive settings: {ex.Message}");
            }
        }

        private DriveDisplaySettings LoadSettings()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    var defaultSettings = new DriveDisplaySettings();
                    SaveSettings(defaultSettings);
                    return defaultSettings;
                }

                var json = File.ReadAllText(_configFilePath);
                var settings = JsonSerializer.Deserialize<DriveDisplaySettings>(json);
                System.Diagnostics.Debug.WriteLine($"? Drive settings loaded: {json}");
                return settings ?? new DriveDisplaySettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error loading drive settings: {ex.Message}");
                return new DriveDisplaySettings();
            }
        }
    }

    public class DriveDisplaySettings
    {
        public bool ShowRemovableDrives { get; set; } = true;
        public bool ShowFixedDrives { get; set; } = false;
        public bool ShowNetworkDrives { get; set; } = false;
        public bool ShowCdRomDrives { get; set; } = false;
        public bool ShowRamDrives { get; set; } = false;
        public bool ShowUnknownDrives { get; set; } = false;
        public bool HideSystemDrive { get; set; } = true;
        public List<string> HiddenDriveLetters { get; set; } = new();
    }
}