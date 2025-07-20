using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace GameCopier.Models
{
    public class Drive : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _driveLetter = string.Empty;
        private string _label = string.Empty;
        private long _totalSizeInBytes;
        private long _freeSizeInBytes;
        private bool _isSelected;
        private bool _isRemovable;
        private bool _isRecentlyPlugged;
        private DateTime _detectedAt;
        private string _deviceDescription = string.Empty;
        private string _brandName = string.Empty;
        private string _model = string.Empty;
        private string _deviceId = string.Empty;
        private string _fileSystem = string.Empty;
        private DateTime _insertedTime;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string DriveLetter
        {
            get => _driveLetter;
            set => SetProperty(ref _driveLetter, value);
        }

        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }

        public long TotalSizeInBytes
        {
            get => _totalSizeInBytes;
            set
            {
                if (SetProperty(ref _totalSizeInBytes, value))
                {
                    OnPropertyChanged(nameof(UsedSizeInBytes));
                    OnPropertyChanged(nameof(SizeDisplay));
                    OnPropertyChanged(nameof(UsagePercentage));
                    OnPropertyChanged(nameof(TotalSizeFormatted));
                    OnPropertyChanged(nameof(UsedSpaceFormatted));
                }
            }
        }

        public long FreeSizeInBytes
        {
            get => _freeSizeInBytes;
            set
            {
                if (SetProperty(ref _freeSizeInBytes, value))
                {
                    OnPropertyChanged(nameof(UsedSizeInBytes));
                    OnPropertyChanged(nameof(SizeDisplay));
                    OnPropertyChanged(nameof(UsagePercentage));
                    OnPropertyChanged(nameof(FreeSpaceFormatted));
                    OnPropertyChanged(nameof(UsedSpaceFormatted));
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsRemovable
        {
            get => _isRemovable;
            set => SetProperty(ref _isRemovable, value);
        }

        public bool IsRecentlyPlugged
        {
            get => _isRecentlyPlugged;
            set
            {
                if (SetProperty(ref _isRecentlyPlugged, value))
                {
                    OnPropertyChanged(nameof(GetCardBackgroundColor));
                    OnPropertyChanged(nameof(GetCardBorderColor));
                    OnPropertyChanged(nameof(GetCardBorderThickness));
                    OnPropertyChanged(nameof(GetNewBadgeVisibilityValue));
                }
            }
        }

        public DateTime DetectedAt
        {
            get => _detectedAt;
            set => SetProperty(ref _detectedAt, value);
        }

        public string DeviceDescription
        {
            get => _deviceDescription;
            set => SetProperty(ref _deviceDescription, value);
        }

        /// <summary>
        /// Brand name or manufacturer of the USB drive
        /// </summary>
        public string BrandName
        {
            get => _brandName;
            set => SetProperty(ref _brandName, value);
        }

        /// <summary>
        /// Model name of the USB drive
        /// </summary>
        public string Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        /// <summary>
        /// Device ID from WMI for better identification
        /// </summary>
        public string DeviceId
        {
            get => _deviceId;
            set => SetProperty(ref _deviceId, value);
        }

        /// <summary>
        /// File system type (NTFS, FAT32, exFAT, etc.)
        /// </summary>
        public string FileSystem
        {
            get => _fileSystem;
            set => SetProperty(ref _fileSystem, value);
        }

        /// <summary>
        /// Time when the drive was inserted (for highlighting purposes)
        /// </summary>
        public DateTime InsertedTime
        {
            get => _insertedTime;
            set => SetProperty(ref _insertedTime, value);
        }

        // Computed properties
        public long UsedSizeInBytes => TotalSizeInBytes - FreeSizeInBytes;
        public string SizeDisplay => $"{FormatBytes(UsedSizeInBytes)}/{FormatBytes(TotalSizeInBytes)}";
        public double UsagePercentage => TotalSizeInBytes > 0 ? ((double)UsedSizeInBytes / TotalSizeInBytes) * 100 : 0;

        public string TotalSizeFormatted => FormatBytes(TotalSizeInBytes);
        public string FreeSpaceFormatted => FormatBytes(FreeSizeInBytes);
        public string UsedSpaceFormatted => FormatBytes(UsedSizeInBytes);

        /// <summary>
        /// Enhanced description that includes brand, model, and other identifying information
        /// </summary>
        public string EnhancedDescription
        {
            get
            {
                System.Diagnostics.Debug.WriteLine($"?? EnhancedDescription - Brand: '{BrandName}', Model: '{Model}', FileSystem: '{FileSystem}', DeviceDescription: '{DeviceDescription}'");
                
                var parts = new List<string>();

                // Start with brand name if available and meaningful
                if (!string.IsNullOrEmpty(BrandName) && 
                    BrandName != "USB" && 
                    BrandName != "Storage" && 
                    BrandName != "Device" && 
                    BrandName.Length > 2)
                {
                    parts.Add(BrandName);
                    System.Diagnostics.Debug.WriteLine($"?? Added brand: '{BrandName}'");
                }

                // Add model if available and different from brand
                if (!string.IsNullOrEmpty(Model) && 
                    Model != BrandName && 
                    Model.Length > 2 &&
                    Model != "USB" &&
                    Model != "Storage" &&
                    Model != "Device")
                {
                    // Clean the model name further
                    var cleanModel = Model
                        .Replace("USB Device", "")
                        .Replace("USB", "")
                        .Replace("Device", "")
                        .Replace("Storage", "")
                        .Trim();
                    
                    if (!string.IsNullOrEmpty(cleanModel) && cleanModel.Length > 2)
                    {
                        parts.Add(cleanModel);
                        System.Diagnostics.Debug.WriteLine($"?? Added model: '{cleanModel}'");
                    }
                }

                // Always add filesystem if available - show it prominently at the end
                var result = "";
                
                if (parts.Count > 0)
                {
                    // Join brand and model
                    result = string.Join(" ", parts);
                    System.Diagnostics.Debug.WriteLine($"?? Combined brand/model: '{result}'");
                }
                else
                {
                    // Fallback to cleaned device description
                    if (!string.IsNullOrEmpty(DeviceDescription))
                    {
                        var cleaned = DeviceDescription;
                        
                        // Only clean if it contains redundant terms
                        if (cleaned.Contains("USB Storage Device") || 
                            cleaned.Contains("USB Device") || 
                            cleaned.Contains("Storage Device") ||
                            cleaned.Contains("Standard disk drives")) 
                        {
                            cleaned = cleaned
                                .Replace("USB Storage Device", "")
                                .Replace("USB Device", "")
                                .Replace("Storage Device", "")
                                .Replace("(Standard disk drives)","")
                                .Trim();
                        }
                        
                        // If after cleaning we still have something meaningful, use it
                        if (!string.IsNullOrEmpty(cleaned) && cleaned.Length > 3)
                        {
                            result = cleaned;
                            System.Diagnostics.Debug.WriteLine($"?? Using cleaned device description: '{result}'");
                        }
                        // If original device description is meaningful, keep it
                        else if (DeviceDescription != "USB Storage Device" && 
                                DeviceDescription != "USB Device" && 
                                DeviceDescription != "Storage Device" &&
                                DeviceDescription.Length > 5)
                        {
                            result = DeviceDescription;
                            System.Diagnostics.Debug.WriteLine($"?? Using original device description: '{result}'");
                        }
                    }
                    
                    // Final fallback if no meaningful device info
                    if (string.IsNullOrEmpty(result))
                    {
                        result = "USB Storage";
                        System.Diagnostics.Debug.WriteLine($"?? Using fallback: '{result}'");
                    }
                }

                // Always append filesystem if available - this provides the technical detail
                if (!string.IsNullOrEmpty(FileSystem) && FileSystem != "Unknown")
                {
                    result += $" ({FileSystem})";
                    System.Diagnostics.Debug.WriteLine($"?? Added filesystem: '({FileSystem})'");
                }

                // Final cleanup
                while (result.Contains("  "))
                {
                    result = result.Replace("  ", " ");
                }
                
                var finalResult = result.Trim();
                System.Diagnostics.Debug.WriteLine($"?? EnhancedDescription final result: '{finalResult}'");
                return finalResult;
            }
        }

        /// <summary>
        /// Time since insertion formatted as a string
        /// </summary>
        public string TimeSinceInsertion
        {
            get
            {
                if (InsertedTime == default)
                    return "";

                var timeSpan = DateTime.Now - InsertedTime;
                if (timeSpan.TotalMinutes < 1)
                    return "Just now";
                else if (timeSpan.TotalHours < 1)
                    return $"{(int)timeSpan.TotalMinutes}m ago";
                else if (timeSpan.TotalDays < 1)
                    return $"{(int)timeSpan.TotalHours}h ago";
                else
                    return $"{(int)timeSpan.TotalDays}d ago";
            }
        }

        // UI Helper Properties for WinUI 3
        public SolidColorBrush GetCardBackgroundColor =>
            IsRecentlyPlugged ?
                new SolidColorBrush(Windows.UI.Color.FromArgb(25, 0, 255, 0)) :
                new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)); // Transparent for default theme color

        public SolidColorBrush GetCardBorderColor =>
            IsRecentlyPlugged ?
                new SolidColorBrush(Windows.UI.Color.FromArgb(255, 50, 205, 50)) : // LimeGreen
                new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)); // Transparent

        public Thickness GetCardBorderThickness => IsRecentlyPlugged ? new Thickness(2) : new Thickness(0);
        public Visibility GetNewBadgeVisibilityValue => IsRecentlyPlugged ? Visibility.Visible : Visibility.Collapsed;
        public Visibility GetDescriptionVisibilityValue => !string.IsNullOrEmpty(EnhancedDescription) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility GetTimeVisibilityValue => !string.IsNullOrEmpty(TimeSinceInsertion) ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);

            // Update computed properties that depend on this change
            if (propertyName is nameof(BrandName) or nameof(Model) or nameof(FileSystem) or nameof(DeviceDescription))
            {
                OnPropertyChanged(nameof(EnhancedDescription));
                OnPropertyChanged(nameof(GetDescriptionVisibilityValue));
            }
            if (propertyName == nameof(InsertedTime))
            {
                OnPropertyChanged(nameof(TimeSinceInsertion));
                OnPropertyChanged(nameof(GetTimeVisibilityValue));
            }

            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static string FormatBytes(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;
            const long TB = GB * 1024;

            return bytes switch
            {
                >= TB => $"{bytes / (double)TB:F2} TB",
                >= GB => $"{bytes / (double)GB:F2} GB",
                >= MB => $"{bytes / (double)MB:F2} MB",
                >= KB => $"{bytes / (double)KB:F2} KB",
                _ => $"{bytes} B"
            };
        }
    }
}