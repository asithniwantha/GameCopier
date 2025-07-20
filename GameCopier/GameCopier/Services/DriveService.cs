using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Management;
using GameCopier.Models;

namespace GameCopier.Services
{
    public class UsbDriveDetectionService : IDisposable
    {
        private readonly Timer _pollingTimer;
        private HashSet<string> _lastKnownDrives = new();
        private string? _mostRecentDrive = null;
        private bool _isDisposed = false;

        public event EventHandler<UsbDriveChangedEventArgs>? UsbDriveChanged;

        public string? CurrentMostRecentDrive => _mostRecentDrive;

        public UsbDriveDetectionService()
        {
            System.Diagnostics.Debug.WriteLine("🔍 Starting USB-only detection service...");
            
            UpdateKnownDrives();
            
            _pollingTimer = new Timer(CheckForDriveChanges, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            
            System.Diagnostics.Debug.WriteLine("✅ USB-only detection service started with 1-second polling");
        }

        private void UpdateKnownDrives()
        {
            try
            {
                var currentDrives = GetCurrentUsbDrives();
                _lastKnownDrives = currentDrives;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error updating known drives: {ex.Message}");
            }
        }

        private HashSet<string> GetCurrentUsbDrives()
        {
            var drives = new HashSet<string>();
            
            try
            {
                var allDrives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .ToList();

                foreach (var drive in allDrives)
                {
                    if (drive.Name.ToUpper().StartsWith("C:"))
                        continue;

                    if (drive.DriveType == DriveType.Removable)
                    {
                        drives.Add(drive.Name.TrimEnd('\\'));
                        System.Diagnostics.Debug.WriteLine($"🔍 Found USB drive: {drive.Name} - {drive.DriveType}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"🔍 Skipped non-removable drive: {drive.Name} - {drive.DriveType}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting current USB drives: {ex.Message}");
            }

            return drives;
        }

        private void CheckForDriveChanges(object? state)
        {
            try
            {
                var currentDrives = GetCurrentUsbDrives();

                if (!currentDrives.SetEquals(_lastKnownDrives))
                {
                    var added = currentDrives.Except(_lastKnownDrives).ToList();
                    var removed = _lastKnownDrives.Except(currentDrives).ToList();
                    
                    if (added.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"🔍 USB drives ADDED: {string.Join(", ", added)}");
                        
                        _mostRecentDrive = added.Last();
                        System.Diagnostics.Debug.WriteLine($"✅ Most recent USB drive: {_mostRecentDrive}");
                    }
                    
                    if (removed.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"🔍 USB drives REMOVED: {string.Join(", ", removed)}");
                        
                        if (_mostRecentDrive != null && removed.Contains(_mostRecentDrive))
                        {
                            _mostRecentDrive = null;
                            System.Diagnostics.Debug.WriteLine("🔍 Most recent drive was removed");
                        }
                    }

                    _lastKnownDrives = currentDrives;
                    
                    var eventArgs = new UsbDriveChangedEventArgs
                    {
                        AddedDrives = added,
                        RemovedDrives = removed,
                        MostRecentDrive = _mostRecentDrive
                    };
                    
                    UsbDriveChanged?.Invoke(this, eventArgs);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error checking for drive changes: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            try
            {
                _pollingTimer?.Dispose();
                System.Diagnostics.Debug.WriteLine("✅ USB detection service disposed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error disposing USB detection service: {ex.Message}");
            }

            _isDisposed = true;
        }
    }

    public class UsbDriveChangedEventArgs : EventArgs
    {
        public List<string> AddedDrives { get; set; } = new();
        public List<string> RemovedDrives { get; set; } = new();
        public string? MostRecentDrive { get; set; }
    }

    public class DriveService
    {
        private readonly SettingsService _settingsService;

        public DriveService()
        {
            _settingsService = new SettingsService();
        }

        public async Task<IEnumerable<Drive>> GetRemovableDrivesAsync()
        {
            return await GetRemovableDrivesWithHighlightAsync(null);
        }

        public async Task<IEnumerable<Drive>> GetRemovableDrivesWithHighlightAsync(string? mostRecentDrive)
        {
            return await Task.Run(() =>
            {
                var drives = new List<Drive>();
                var settings = _settingsService.GetSettings();

                try
                {
                    System.Diagnostics.Debug.WriteLine("🔍 === Starting FILTERED drive detection with user preferences ===");
                    
                    var allDrives = DriveInfo.GetDrives()
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
                                System.Diagnostics.Debug.WriteLine($"🔍 Creating drive object for {driveInfo.Name}...");
                                var isRemovable = driveInfo.DriveType == DriveType.Removable;
                                var drive = CreateDriveFromDriveInfo(driveInfo, isRemovable);
                                
                                System.Diagnostics.Debug.WriteLine($"🔍 Getting device description for {driveInfo.Name}...");
                                drive.DeviceDescription = GetDeviceDescription(driveInfo.Name.TrimEnd('\\'));
                                System.Diagnostics.Debug.WriteLine($"🔍 Device description result: '{drive.DeviceDescription}'");
                                
                                ExtractBrandAndModel(drive);
                                
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
                                System.Diagnostics.Debug.WriteLine($"✅ Added drive: {driveInfo.Name} - {driveInfo.DriveType} - {drive.DeviceDescription}");
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

                    if (!drives.Any())
                    {
                        System.Diagnostics.Debug.WriteLine("🔍 No drives found with current settings - adding demo drives for debugging...");
                        var demoDrives = GetDemoModeDrives();
                        drives.AddRange(demoDrives);
                        System.Diagnostics.Debug.WriteLine($"🔍 Added {demoDrives.Count} demo drives");
                    }

                    System.Diagnostics.Debug.WriteLine($"🔍 === Total drives returned: {drives.Count} ===");
                    foreach (var drive in drives)
                    {
                        var highlight = drive.IsRecentlyPlugged ? " (MOST RECENT)" : "";
                        System.Diagnostics.Debug.WriteLine($"   ✅ {drive.DriveLetter}: {drive.Name} - {drive.DeviceDescription}{highlight}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error in GetRemovableDrivesWithHighlightAsync: {ex.Message}");
                }

                return drives.OrderByDescending(d => d.IsRecentlyPlugged)
                           .ThenBy(d => d.DriveLetter)
                           .ToList();
            });
        }

        private List<Drive> GetDemoModeDrives()
        {
            var drives = new List<Drive>();
            
            try
            {
                var demoDrives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && !d.Name.ToUpper().StartsWith("C:"))
                    .Take(3)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"🔍 Demo mode: Found {demoDrives.Count} potential demo drives");

                foreach (var driveInfo in demoDrives)
                {
                    try
                    {
                        var drive = CreateDriveFromDriveInfo(driveInfo, false);
                        drive.Name += " (Demo Mode - Not Real USB)";
                        drive.DeviceDescription = $"Demo {driveInfo.DriveType} Drive";
                        drives.Add(drive);
                        System.Diagnostics.Debug.WriteLine($"🔍 Added demo drive: {driveInfo.Name} - {driveInfo.DriveType}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error processing demo drive {driveInfo.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in GetDemoModeDrives: {ex.Message}");
            }

            return drives;
        }

        private string GetDeviceDescription(string driveLetter)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 === ENHANCED DEVICE DETECTION for {driveLetter} ===");
                
                // PRIORITY 1: Get the SPECIFIC device name for THIS drive letter FIRST
                try
                {
                    var specificDeviceName = GetSpecificDeviceNameForDrive(driveLetter);
                    if (!string.IsNullOrEmpty(specificDeviceName))
                    {
                        System.Diagnostics.Debug.WriteLine($"🔍 ✅ Found specific device name for {driveLetter}: '{specificDeviceName}'");
                        return specificDeviceName;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Specific device name query failed: {ex.Message}");
                }
                
                // PRIORITY 2: Try to get enhanced physical disk info
                try
                {
                    var physicalInfo = GetEnhancedPhysicalDiskInfo(driveLetter);
                    if (!string.IsNullOrEmpty(physicalInfo))
                    {
                        System.Diagnostics.Debug.WriteLine($"🔍 Enhanced Physical Disk Info: '{physicalInfo}'");
                        return physicalInfo;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Enhanced Physical Disk query failed: {ex.Message}");
                }
                
                // PRIORITY 3: Use volume label as fallback (but this will be handled in EnhancedDescription)
                try
                {
                    var query = $"SELECT VolumeLabel, FileSystem FROM Win32_LogicalDisk WHERE DeviceID = '{driveLetter}:'";
                    using var searcher = new ManagementObjectSearcher(query);
                    
                    foreach (ManagementObject disk in searcher.Get())
                    {
                        var volumeLabel = disk["VolumeLabel"]?.ToString();
                        var fileSystem = disk["FileSystem"]?.ToString();
                        
                        System.Diagnostics.Debug.WriteLine($"🔍 Volume Label for {driveLetter}: '{volumeLabel}', FileSystem: '{fileSystem}'");
                        
                        // Only return volume label if it's meaningful and custom
                        if (!string.IsNullOrEmpty(volumeLabel) && 
                            volumeLabel.Trim().Length > 0 &&
                            !volumeLabel.ToUpper().Contains("NEW VOLUME") &&
                            volumeLabel != "USB Drive")
                        {
                            System.Diagnostics.Debug.WriteLine($"🔍 ✅ Using meaningful volume label: '{volumeLabel}'");
                            return volumeLabel.Trim();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Volume label query failed: {ex.Message}");
                }
                
                // Final fallback - return empty to let EnhancedDescription handle the volume label display
                System.Diagnostics.Debug.WriteLine($"🔍 No specific device info found for {driveLetter}, letting EnhancedDescription handle volume label");
                return "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Critical error in GetDeviceDescription for {driveLetter}: {ex.Message}");
                return "";
            }
        }

        private string GetSpecificDeviceNameForDrive(string driveLetter)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 === GETTING SPECIFIC DEVICE NAME for {driveLetter} ===");
                
                // Step 1: Map drive letter to partition
                string? diskIndex = null;
                string? partitionIndex = null;
                
                try
                {
                    var partitionQuery = $"SELECT * FROM Win32_LogicalDiskToPartition WHERE Dependent = 'Win32_LogicalDisk.DeviceID=\"{driveLetter}:\"'";
                    using var partitionSearcher = new ManagementObjectSearcher(partitionQuery);
                    
                    foreach (ManagementObject partition in partitionSearcher.Get())
                    {
                        var antecedent = partition["Antecedent"]?.ToString();
                        System.Diagnostics.Debug.WriteLine($"🔍 Partition antecedent for {driveLetter}: '{antecedent}'");
                        
                        if (!string.IsNullOrEmpty(antecedent))
                        {
                            // Extract disk and partition index from antecedent
                            var diskMatch = System.Text.RegularExpressions.Regex.Match(antecedent, @"Disk #(\d+).*Partition #(\d+)");
                            if (diskMatch.Success)
                            {
                                diskIndex = diskMatch.Groups[1].Value;
                                partitionIndex = diskMatch.Groups[2].Value;
                                System.Diagnostics.Debug.WriteLine($"🔍 Found mapping for {driveLetter}: Disk #{diskIndex}, Partition #{partitionIndex}");
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Partition mapping failed for {driveLetter}: {ex.Message}");
                }
                
                // Step 2: If we found a specific disk, get its device information
                if (!string.IsNullOrEmpty(diskIndex))
                {
                    try
                    {
                        var diskQuery = $"SELECT Model, Caption, Manufacturer, SerialNumber, Size FROM Win32_DiskDrive WHERE Index = {diskIndex}";
                        using var diskSearcher = new ManagementObjectSearcher(diskQuery);
                        
                        foreach (ManagementObject diskDrive in diskSearcher.Get())
                        {
                            var model = diskDrive["Model"]?.ToString()?.Trim();
                            var caption = diskDrive["Caption"]?.ToString()?.Trim();
                            var manufacturer = diskDrive["Manufacturer"]?.ToString()?.Trim();
                            var serialNumber = diskDrive["SerialNumber"]?.ToString()?.Trim();
                            var size = diskDrive["Size"]?.ToString();
                            
                            System.Diagnostics.Debug.WriteLine($"🔍 Disk {diskIndex} details:");
                            System.Diagnostics.Debug.WriteLine($"  Model: '{model}'");
                            System.Diagnostics.Debug.WriteLine($"  Caption: '{caption}'");
                            System.Diagnostics.Debug.WriteLine($"  Manufacturer: '{manufacturer}'");
                            System.Diagnostics.Debug.WriteLine($"  SerialNumber: '{serialNumber}'");
                            System.Diagnostics.Debug.WriteLine($"  Size: '{size}'");
                            
                            // Use the most specific identifier available
                            if (!string.IsNullOrEmpty(model) && model != "Unknown")
                            {
                                // Add partition info to make it unique if multiple partitions exist
                                var result = model;
                                if (!string.IsNullOrEmpty(partitionIndex) && partitionIndex != "0")
                                {
                                    result += $" (Partition {partitionIndex})";
                                }
                                System.Diagnostics.Debug.WriteLine($"🔍 ✅ Using model with partition info: '{result}'");
                                return result;
                            }
                            else if (!string.IsNullOrEmpty(caption) && caption != "Unknown")
                            {
                                var result = caption;
                                if (!string.IsNullOrEmpty(partitionIndex) && partitionIndex != "0")
                                {
                                    result += $" (Partition {partitionIndex})";
                                }
                                System.Diagnostics.Debug.WriteLine($"🔍 ✅ Using caption with partition info: '{result}'");
                                return result;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Disk drive query failed for index {diskIndex}: {ex.Message}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"🔍 No specific device name found for {driveLetter}");
                return "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting specific device name for {driveLetter}: {ex.Message}");
                return "";
            }
        }

        private string GetEnhancedPhysicalDiskInfo(string driveLetter)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 Getting enhanced physical disk info for {driveLetter}");
                
                var usbQuery = "SELECT Model, Caption, Manufacturer, Size, MediaType, InterfaceType FROM Win32_DiskDrive WHERE InterfaceType = 'USB'";
                using var searcher = new ManagementObjectSearcher(usbQuery);
                
                foreach (ManagementObject drive in searcher.Get())
                {
                    var model = drive["Model"]?.ToString() ?? "";
                    var caption = drive["Caption"]?.ToString() ?? "";
                    var manufacturer = drive["Manufacturer"]?.ToString() ?? "";
                    var mediaType = drive["MediaType"]?.ToString() ?? "";
                    
                    System.Diagnostics.Debug.WriteLine($"🔍 Physical Disk - Model: {model}, Caption: {caption}, Manufacturer: {manufacturer}, MediaType: {mediaType}");
                    
                    if (!string.IsNullOrEmpty(model) && model != "Unknown")
                    {
                        return CleanAndEnhanceDeviceName(model, manufacturer);
                    }
                    else if (!string.IsNullOrEmpty(caption) && caption != "Unknown")
                    {
                        return CleanAndEnhanceDeviceName(caption, manufacturer);
                    }
                }
                
                return "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting enhanced physical disk info: {ex.Message}");
                return "";
            }
        }

        private string CleanAndEnhanceDeviceName(string deviceName, string? manufacturer = null)
        {
            System.Diagnostics.Debug.WriteLine($"🧹 CleanAndEnhanceDeviceName - Input: '{deviceName}', Manufacturer: '{manufacturer}'");
            
            var cleaned = deviceName;
            
            // Step 1: Remove USB prefix but preserve the rest
            if (cleaned.ToUpper().StartsWith("USB "))
            {
                cleaned = cleaned.Substring(4).Trim();
                System.Diagnostics.Debug.WriteLine($"🧹 Removed USB prefix: '{cleaned}'");
            }
            
            // Step 2: Remove USB suffixes but preserve version info
            if (cleaned.ToUpper().EndsWith(" USB DEVICE"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 11).Trim();
                System.Diagnostics.Debug.WriteLine($"🧹 Removed ' USB DEVICE' suffix: '{cleaned}'");
            }
            else if (cleaned.ToUpper().EndsWith(" USB"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 4).Trim();
                System.Diagnostics.Debug.WriteLine($"🧹 Removed ' USB' suffix: '{cleaned}'");
            }
            
            // Step 3: PRESERVE version info like "3.2Gen1" - REMOVED the regex that was deleting it
            // The user wants to keep version information, so we skip the version removal step
            System.Diagnostics.Debug.WriteLine($"🧹 Preserving version info: '{cleaned}'");
            
            // Step 4: Clean up multiple spaces
            while (cleaned.Contains("  "))
            {
                cleaned = cleaned.Replace("  ", " ");
            }
            cleaned = cleaned.Trim();
            System.Diagnostics.Debug.WriteLine($"🧹 Space cleanup: '{cleaned}'");
            
            // Step 5: Final validation and fallback
            if (string.IsNullOrWhiteSpace(cleaned) || cleaned.Length < 2)
            {
                cleaned = "USB Storage";
                System.Diagnostics.Debug.WriteLine($"🧹 Used fallback: '{cleaned}'");
            }
            
            System.Diagnostics.Debug.WriteLine($"🧹 ✅ Final result: '{cleaned}'");
            return cleaned;
        }

        private Drive CreateDriveFromDriveInfo(DriveInfo driveInfo, bool isRemovable)
        {
            var drive = new Drive();
            drive.Name = GetDriveName(driveInfo, isRemovable);
            drive.DriveLetter = driveInfo.Name.TrimEnd('\\');
            drive.Label = driveInfo.VolumeLabel ?? "";
            drive.TotalSizeInBytes = driveInfo.TotalSize;
            drive.FreeSizeInBytes = driveInfo.AvailableFreeSpace;
            drive.IsRemovable = isRemovable;
            drive.DetectedAt = DateTime.Now;
            
            try
            {
                drive.FileSystem = driveInfo.DriveFormat ?? "";
                System.Diagnostics.Debug.WriteLine($"✅ Created enhanced drive: {drive.DriveLetter} - FileSystem: {drive.FileSystem}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error setting enhanced drive properties: {ex.Message}");
                drive.DeviceDescription = "USB Storage Device";
            }
            
            return drive;
        }

        private string GetDriveName(DriveInfo driveInfo, bool isRemovable)
        {
            var label = string.IsNullOrEmpty(driveInfo.VolumeLabel) ? "USB Drive" : driveInfo.VolumeLabel;
            var sizeGB = Math.Round(driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0), 1);
            
            return $"{label} ({driveInfo.Name}) {sizeGB}GB";
        }

        public bool HasSufficientSpace(Drive drive, IEnumerable<Game> games)
        {
            var totalGameSize = games.Sum(g => g.SizeInBytes);
            return drive.FreeSizeInBytes >= totalGameSize;
        }

        private void ExtractBrandAndModel(Drive drive)
        {
            try
            {
                var description = drive.DeviceDescription;
                System.Diagnostics.Debug.WriteLine($"🔍 === BRAND/MODEL EXTRACTION for {drive.DriveLetter} ===");
                System.Diagnostics.Debug.WriteLine($"🔍 Drive Label: '{drive.Label}'");
                System.Diagnostics.Debug.WriteLine($"🔍 Device Description: '{description}'");
                
                // Clear any previous values
                drive.BrandName = "";
                drive.Model = "";
                
                // Only extract brand/model if we have a meaningful device description that's not a volume label
                if (!string.IsNullOrEmpty(description) && 
                    description != drive.Label &&
                    !description.StartsWith("USB Drive ") &&
                    description != "USB Storage Device" &&
                    description != "USB Device" &&
                    description != "Storage Device" &&
                    description.Length > 5)
                {
                    var cleanedDescription = description;
                    
                    // Pre-clean the description but PRESERVE version info
                    if (cleanedDescription.ToUpper().StartsWith("USB "))
                    {
                        cleanedDescription = cleanedDescription.Substring(4).Trim();
                    }
                    cleanedDescription = cleanedDescription.Replace(" USB Device", "").Replace(" USB DEVICE", "").Trim();
                    
                    System.Diagnostics.Debug.WriteLine($"🔍 Cleaned description for brand extraction: '{cleanedDescription}'");
                    
                    var brands = new[] { 
                        "SanDisk", "Kingston", "Samsung", "Lexar", "PNY", 
                        "Corsair", "Transcend", "Verbatim", "Toshiba", "Sony", "ADATA",
                        "Seagate", "Western Digital", "WD", "Crucial", "Patriot", "Micron"
                    };
                    
                    bool brandFound = false;
                    foreach (var brand in brands)
                    {
                        if (cleanedDescription.ToUpper().Contains(brand.ToUpper()))
                        {
                            drive.BrandName = brand;
                            brandFound = true;
                            System.Diagnostics.Debug.WriteLine($"🔍 ✅ Found brand '{brand}' in device description");
                            
                            var brandIndex = cleanedDescription.ToUpper().IndexOf(brand.ToUpper());
                            if (brandIndex >= 0)
                            {
                                var afterBrand = cleanedDescription.Substring(brandIndex + brand.Length).Trim();
                                System.Diagnostics.Debug.WriteLine($"🔍 Text after brand: '{afterBrand}'");
                                
                                // Clean up but preserve version numbers and model info
                                afterBrand = afterBrand
                                    .Replace("USB Device", "")
                                    .Replace("USB DEVICE", "")
                                    .Replace("USB", "")
                                    .Replace("Device", "")
                                    .Replace("DEVICE", "")
                                    .Trim();
                                
                                if (!string.IsNullOrEmpty(afterBrand) && afterBrand.Length > 1)
                                {
                                    drive.Model = afterBrand;
                                    System.Diagnostics.Debug.WriteLine($"🔍 ✅ Set model to: '{afterBrand}' (with version preserved)");
                                }
                            }
                            break;
                        }
                    }
                    
                    // If no brand found but we have a good device description, treat the whole thing as a model
                    if (!brandFound && cleanedDescription.Length > 3)
                    {
                        // Check if it looks like a model name (contains numbers, version info, etc.)
                        if (System.Text.RegularExpressions.Regex.IsMatch(cleanedDescription, @"[\d\.]+|Ultra|Pro|Plus|Max|Extreme"))
                        {
                            drive.Model = cleanedDescription;
                            System.Diagnostics.Debug.WriteLine($"🔍 ✅ Set whole description as model: '{cleanedDescription}'");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"🔍 Skipping brand/model extraction - no meaningful device description");
                }
                
                System.Diagnostics.Debug.WriteLine($"🔍 === FINAL EXTRACTION RESULT for {drive.DriveLetter} ===");
                System.Diagnostics.Debug.WriteLine($"🔍 ✅ Brand: '{drive.BrandName}' | Model: '{drive.Model}' | Description: '{description}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error extracting brand and model for {drive.DriveLetter}: {ex.Message}");
            }
        }
    }
}