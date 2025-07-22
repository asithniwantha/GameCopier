using GameCopier.Models.Domain;
using GameCopier.Models.Configuration;
using GameCopier.Models.Events;
using GameCopier.Services.Infrastructure;
using GameCopier.Services.Data;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GameCopier.ViewModels.Managers
{
    /// <summary>
    /// Manages USB drive detection, monitoring, and drive-related operations
    /// </summary>
    public class UsbDriveManager : INotifyPropertyChanged, IDisposable
    {
        private readonly DriveService _driveService;
        private readonly UsbDetectionService _usbDetectionService;
        private readonly DispatcherQueue? _uiDispatcher;
        private int _usbEventCount = 0;

        public event EventHandler<List<Drive>>? DrivesUpdated;
        public event EventHandler<string>? StatusChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public UsbDriveManager(DispatcherQueue? uiDispatcher)
        {
            _uiDispatcher = uiDispatcher;
            _driveService = new DriveService();
            _usbDetectionService = new UsbDetectionService();

            _usbDetectionService.UsbDriveChanged += OnUsbDriveChanged;
        }

        public async Task<List<Drive>> LoadDrivesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 UsbDriveManager: Loading drives...");

                // DEBUG: Let's see what the settings service returns
                var settings = new SettingsService().GetSettings();
                System.Diagnostics.Debug.WriteLine($"🔍 Settings: ShowRemovableDrives={settings.ShowRemovableDrives}, ShowFixedDrives={settings.ShowFixedDrives}");
                System.Diagnostics.Debug.WriteLine($"🔍 Settings: HiddenDriveLetters={string.Join(",", settings.HiddenDriveLetters)}");

                var mostRecentDrive = _usbDetectionService.CurrentMostRecentDrive;
                System.Diagnostics.Debug.WriteLine($"🔍 Most recent drive: {mostRecentDrive ?? "null"}");

                List<Drive> driveList;

                try
                {
                    var drives = await _driveService.GetRemovableDrivesWithHighlightAsync(mostRecentDrive);
                    driveList = drives.ToList();
                    System.Diagnostics.Debug.WriteLine($"🔍 UsbDriveManager: DriveService returned {driveList.Count} drives");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ DriveService failed: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine("🔍 Falling back to basic drive detection...");

                    // FALLBACK: Use basic drive detection without WMI
                    driveList = GetBasicDriveList(settings, mostRecentDrive);
                }

                // DEBUG: Log details of each drive found
                foreach (var drive in driveList)
                {
                    System.Diagnostics.Debug.WriteLine($"🔍   Drive: {drive.DriveLetter} - {drive.Name} - Removable: {drive.IsRemovable}");
                }

                // DEBUG: If no drives found, let's check what's actually on the system and force demo mode
                if (driveList.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("🔍 No drives found! Checking system drives...");
                    var allDrives = System.IO.DriveInfo.GetDrives();
                    foreach (var sysDrive in allDrives)
                    {
                        if (sysDrive.IsReady)
                        {
                            System.Diagnostics.Debug.WriteLine($"🔍   System Drive: {sysDrive.Name} - Type: {sysDrive.DriveType} - Ready: {sysDrive.IsReady}");
                        }
                    }

                    // Create demo drives for testing
                    System.Diagnostics.Debug.WriteLine("🔍 Creating demo drives for testing...");
                    driveList = CreateDemoDrives();
                    System.Diagnostics.Debug.WriteLine($"🔍 Created {driveList.Count} demo drives");
                }

                return driveList;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ UsbDriveManager: Error loading drives - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ UsbDriveManager: Stack trace - {ex.StackTrace}");
                StatusChanged?.Invoke(this, $"Error loading USB drives: {ex.Message}");

                // Return demo drives even on error so something shows up
                return CreateDemoDrives();
            }
        }

        private List<Drive> GetBasicDriveList(DriveDisplaySettings settings, string? mostRecentDrive)
        {
            var drives = new List<Drive>();

            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 FALLBACK: Using basic DriveInfo API...");

                var allDrives = System.IO.DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"🔍 Found {allDrives.Count} ready drives total");

                foreach (var driveInfo in allDrives)
                {
                    try
                    {
                        // Skip system drive if setting is enabled
                        if (settings.HideSystemDrive && driveInfo.Name.ToUpper().StartsWith("C:"))
                        {
                            System.Diagnostics.Debug.WriteLine($"🔍 Skipping system drive: {driveInfo.Name}");
                            continue;
                        }

                        // Skip specifically hidden drives
                        if (settings.HiddenDriveLetters.Contains(driveInfo.Name.TrimEnd('\\')))
                        {
                            System.Diagnostics.Debug.WriteLine($"🔍 Skipping user-hidden drive: {driveInfo.Name}");
                            continue;
                        }

                        // Filter by drive type based on settings
                        bool shouldInclude = driveInfo.DriveType switch
                        {
                            DriveType.Removable => settings.ShowRemovableDrives,
                            DriveType.Fixed => settings.ShowFixedDrives,
                            DriveType.Network => settings.ShowNetworkDrives,
                            DriveType.CDRom => settings.ShowCdRomDrives,
                            DriveType.Ram => settings.ShowRamDrives,
                            DriveType.Unknown => settings.ShowUnknownDrives,
                            _ => false
                        };

                        System.Diagnostics.Debug.WriteLine($"🔍 Examining drive {driveInfo.Name} - Type: {driveInfo.DriveType} - Include: {shouldInclude}");

                        if (shouldInclude)
                        {
                            var drive = CreateBasicDriveFromDriveInfo(driveInfo);

                            var driveLetter = drive.DriveLetter;
                            System.Diagnostics.Debug.WriteLine($"🔍 Checking if {driveLetter} is most recent ({mostRecentDrive})...");

                            if (driveLetter == mostRecentDrive)
                            {
                                drive.IsRecentlyPlugged = true;
                                drive.DetectedAt = DateTime.Now;
                                drive.Name = "🔥 " + drive.Name + " (Just Plugged!)";
                                System.Diagnostics.Debug.WriteLine($"✅ Highlighted most recent drive: {driveLetter}");
                            }

                            drives.Add(drive);
                            System.Diagnostics.Debug.WriteLine($"✅ Added drive: {driveInfo.Name} - {driveInfo.DriveType}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"🔍 Skipped drive: {driveInfo.Name} - {driveInfo.DriveType} (filtered by settings)");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error processing drive {driveInfo.Name}: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"🔍 FALLBACK: Total drives processed: {drives.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in GetBasicDriveList: {ex.Message}");
            }

            return drives.OrderByDescending(d => d.IsRecentlyPlugged)
                         .ThenBy(d => d.DriveLetter)
                         .ToList();
        }

        private Drive CreateBasicDriveFromDriveInfo(System.IO.DriveInfo driveInfo)
        {
            var drive = new Drive();

            // Basic properties from DriveInfo
            drive.DriveLetter = driveInfo.Name.TrimEnd('\\');
            drive.Label = string.IsNullOrEmpty(driveInfo.VolumeLabel) ? "Drive" : driveInfo.VolumeLabel;
            drive.TotalSizeInBytes = driveInfo.TotalSize;
            drive.FreeSizeInBytes = driveInfo.AvailableFreeSpace;
            drive.IsRemovable = driveInfo.DriveType == DriveType.Removable;
            drive.DetectedAt = DateTime.Now;

            try
            {
                drive.FileSystem = driveInfo.DriveFormat ?? "Unknown";
            }
            catch
            {
                drive.FileSystem = "Unknown";
            }

            // Create a basic name with size info
            var sizeGB = Math.Round(driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0), 1);
            var typeIcon = driveInfo.DriveType switch
            {
                DriveType.Removable => "💾",
                DriveType.Fixed => "🗄️",
                DriveType.Network => "🌐",
                DriveType.CDRom => "💿",
                _ => "📀"
            };

            drive.Name = $"{typeIcon} {drive.Label} ({drive.DriveLetter}) {sizeGB}GB";

            // Basic device description without WMI
            drive.DeviceDescription = $"{driveInfo.DriveType} Drive";
            drive.BrandName = "";
            drive.Model = "";

            System.Diagnostics.Debug.WriteLine($"✅ Created basic drive: {drive.DriveLetter} - {drive.Name}");
            return drive;
        }

        private async void OnUsbDriveChanged(object? sender, UsbDriveChangedEventArgs e)
        {
            _usbEventCount++;
            System.Diagnostics.Debug.WriteLine($"🔍 UsbDriveManager: USB event #{_usbEventCount}");

            try
            {
                if (e.AddedDrives.Count > 0 || e.RemovedDrives.Count > 0)
                {
                    var drives = await LoadDrivesAsync();

                    if (e.AddedDrives.Count > 0)
                    {
                        var driveNames = string.Join(", ", e.AddedDrives);
                        StatusChanged?.Invoke(this, $"🔌 USB drives connected: {driveNames}");
                    }

                    if (e.RemovedDrives.Count > 0)
                    {
                        var driveNames = string.Join(", ", e.RemovedDrives);
                        StatusChanged?.Invoke(this, $"🔌 USB drives removed: {driveNames}");
                    }

                    // Update drives with highlighting for recently added
                    foreach (var drive in drives)
                    {
                        if (e.AddedDrives.Contains(drive.DriveLetter))
                        {
                            drive.InsertedTime = DateTime.Now;
                            drive.IsRecentlyPlugged = true;

                            // Schedule highlight removal
                            _ = Task.Delay(30000).ContinueWith(_ =>
                            {
                                _uiDispatcher?.TryEnqueue(() =>
                                {
                                    drive.IsRecentlyPlugged = false;
                                });
                            });
                        }
                    }

                    DrivesUpdated?.Invoke(this, drives);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ UsbDriveManager: Error handling USB change - {ex.Message}");
            }
        }

        private List<Drive> CreateDemoDrives()
        {
            var demoDrives = new List<Drive>();

            try
            {
                System.Diagnostics.Debug.WriteLine("🧪 Creating demo USB drives for testing...");

                // Create a few demo drives with different characteristics
                var demoData = new[]
                {
                    new { Letter = "D:", Name = "USB Drive Demo 1", Size = 16L * 1024 * 1024 * 1024, Brand = "SanDisk", Model = "Ultra 3.2" },
                    new { Letter = "E:", Name = "USB Drive Demo 2", Size = 32L * 1024 * 1024 * 1024, Brand = "Kingston", Model = "DataTraveler" },
                    new { Letter = "F:", Name = "USB Drive Demo 3", Size = 64L * 1024 * 1024 * 1024, Brand = "Corsair", Model = "Flash Voyager" }
                };

                foreach (var demo in demoData)
                {
                    var drive = new Drive
                    {
                        Name = $"🧪 {demo.Name} (Demo Mode)",
                        DriveLetter = demo.Letter,
                        Label = demo.Name,
                        TotalSizeInBytes = demo.Size,
                        FreeSizeInBytes = demo.Size - (1024 * 1024 * 1024), // 1GB used
                        IsRemovable = true,
                        DetectedAt = DateTime.Now,
                        DeviceDescription = $"Demo {demo.Brand} {demo.Model}",
                        BrandName = demo.Brand,
                        Model = demo.Model,
                        FileSystem = "NTFS"
                    };

                    demoDrives.Add(drive);
                    System.Diagnostics.Debug.WriteLine($"🧪 Created demo drive: {drive.DriveLetter} - {drive.Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error creating demo drives: {ex.Message}");
            }

            return demoDrives;
        }

        public void Dispose()
        {
            System.Diagnostics.Debug.WriteLine("🗑️ UsbDriveManager: Disposing...");
            _usbDetectionService?.Dispose();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}