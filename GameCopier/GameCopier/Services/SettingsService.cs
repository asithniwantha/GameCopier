using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameCopier.Services
{
    [JsonSerializable(typeof(DriveDisplaySettings))]
    [JsonSerializable(typeof(CopyMethod))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    internal partial class SettingsJsonContext : JsonSerializerContext
    {
    }

    public class SettingsService
    {
        private readonly string _configFilePath;
        private DriveDisplaySettings _settings;

        // Event to notify when settings change
        public event EventHandler<DriveDisplaySettings>? SettingsChanged;

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configFolder = Path.Combine(appDataPath, "GameCopier");
            Directory.CreateDirectory(configFolder);
            _configFilePath = Path.Combine(configFolder, "drivesettings.json");
            LogMessage($"Config file path: {_configFilePath}");
            _settings = LoadSettings();
        }

        public DriveDisplaySettings GetSettings() => _settings;

        public void SaveSettings(DriveDisplaySettings settings)
        {
            _settings = settings;
            try
            {
                var json = JsonSerializer.Serialize(_settings, SettingsJsonContext.Default.DriveDisplaySettings);
                File.WriteAllText(_configFilePath, json);
                LogMessage($"Drive settings saved to: {_configFilePath}");
                LogMessage($"Drive settings saved: {json}");
                
                // Notify subscribers that settings have changed
                SettingsChanged?.Invoke(this, _settings);
            }
            catch (Exception ex)
            {
                LogError($"Error saving drive settings: {ex.Message}");
                LogError($"Exception details: {ex}");
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
                    LogMessage($"No config file, created default at: {_configFilePath}");
                    return defaultSettings;
                }

                var json = File.ReadAllText(_configFilePath);
                var settings = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.DriveDisplaySettings);
                LogMessage($"Drive settings loaded from: {_configFilePath}");
                LogMessage($"Drive settings loaded: {json}");
                return settings ?? new DriveDisplaySettings();
            }
            catch (Exception ex)
            {
                LogError($"Error loading drive settings: {ex.Message}");
                LogError($"Exception details: {ex}");
                return new DriveDisplaySettings();
            }
        }

        [Conditional("DEBUG")]
        private static void LogMessage(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] {message}");
        }

        private static void LogError(string message)
        {
            // Always log errors, even in release builds
            System.Diagnostics.Debug.WriteLine($"[SettingsService] {message}");
#if !DEBUG
            // In release builds, also write to console/trace for visibility
            Console.WriteLine($"[SettingsService] {message}");
            Trace.WriteLine($"[SettingsService] {message}");
#endif
        }
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
    public class DriveDisplaySettings
    {
        public bool ShowRemovableDrives { get; set; } = true;
        public bool ShowFixedDrives { get; set; } = true; // Changed to true for better USB detection
        public bool ShowNetworkDrives { get; set; } = false;
        public bool ShowCdRomDrives { get; set; } = false;
        public bool ShowRamDrives { get; set; } = false;
        public bool ShowUnknownDrives { get; set; } = false;
        public bool HideSystemDrive { get; set; } = true;
        public List<string> HiddenDriveLetters { get; set; } = new();
        
        // Copy performance settings - Now defaults to Windows Explorer dialog
        public CopyMethod PreferredCopyMethod { get; set; } = CopyMethod.ExplorerDialog;
        public bool UseLargeDiskBuffer { get; set; } = true;
        public int MaxConcurrentCopyJobs { get; set; } = 1; // Sequential for Explorer dialog visibility
    }
}