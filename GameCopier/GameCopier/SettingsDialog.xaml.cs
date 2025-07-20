using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;
using GameCopier.Services;
using System.Linq;
using Windows.Storage;
using System.Collections.Generic;
using System.IO;
using System;

namespace GameCopier
{
    public sealed partial class SettingsDialog : ContentDialog
    {
        private readonly LibraryService _libraryService;
        private readonly SettingsService _settingsService;
        private ObservableCollection<string> _folders;
        private StackPanel _foldersPanel = new();
        private StackPanel _driveSettingsPanel = new();

        public SettingsDialog()
        {
            this.XamlRoot = App.MainWindow?.Content?.XamlRoot;
            this.Title = "Settings";
            this.PrimaryButtonText = "Close";
            
            _libraryService = new LibraryService();
            _settingsService = new SettingsService();
            _folders = new ObservableCollection<string>(_libraryService.GetGameFolders());

            // Create main scroll viewer
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = 600
            };

            var mainPanel = new StackPanel { Spacing = 20 };

            // Game Library Section
            var librarySection = CreateLibrarySection();
            mainPanel.Children.Add(librarySection);

            // Drive Display Settings Section
            var driveSection = CreateDriveSettingsSection();
            mainPanel.Children.Add(driveSection);

            scrollViewer.Content = mainPanel;
            Content = scrollViewer;
        }

        private StackPanel CreateLibrarySection()
        {
            var section = new StackPanel();
            
            var header = new TextBlock 
            { 
                Text = "Game Library Folders:", 
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 16,
                Margin = new Thickness(0,0,0,8) 
            };
            section.Children.Add(header);

            _foldersPanel = new StackPanel();
            RefreshFoldersPanel();
            section.Children.Add(_foldersPanel);

            var addButton = new Button 
            { 
                Content = "Add Folder", 
                Margin = new Thickness(0,8,0,0) 
            };
            addButton.Click += async (s, e) => await AddFolderAsync();
            section.Children.Add(addButton);

            return section;
        }

        private StackPanel CreateDriveSettingsSection()
        {
            var section = new StackPanel();
            
            var header = new TextBlock 
            { 
                Text = "Drive Display Settings:", 
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 16,
                Margin = new Thickness(0,0,0,8) 
            };
            section.Children.Add(header);

            var description = new TextBlock
            {
                Text = "Select which types of drives to show in the drive list:",
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Margin = new Thickness(0,0,0,12),
                TextWrapping = TextWrapping.Wrap
            };
            section.Children.Add(description);

            _driveSettingsPanel = new StackPanel();
            RefreshDriveSettingsPanel();
            section.Children.Add(_driveSettingsPanel);

            return section;
        }

        private void RefreshDriveSettingsPanel()
        {
            _driveSettingsPanel.Children.Clear();
            var settings = _settingsService.GetSettings();

            // Create checkboxes for each drive type
            var driveTypes = new[]
            {
                ("Show USB/Removable Drives", settings.ShowRemovableDrives, nameof(settings.ShowRemovableDrives)),
                ("Show Internal/Fixed Drives", settings.ShowFixedDrives, nameof(settings.ShowFixedDrives)),
                ("Show Network Drives", settings.ShowNetworkDrives, nameof(settings.ShowNetworkDrives)),
                ("Show CD/DVD Drives", settings.ShowCdRomDrives, nameof(settings.ShowCdRomDrives)),
                ("Show RAM Drives", settings.ShowRamDrives, nameof(settings.ShowRamDrives)),
                ("Show Unknown Drive Types", settings.ShowUnknownDrives, nameof(settings.ShowUnknownDrives))
            };

            foreach (var (label, isChecked, propertyName) in driveTypes)
            {
                var checkBox = new CheckBox
                {
                    Content = label,
                    IsChecked = isChecked,
                    Margin = new Thickness(0,0,0,8)
                };

                checkBox.Checked += (s, e) => UpdateDriveSetting(propertyName, true);
                checkBox.Unchecked += (s, e) => UpdateDriveSetting(propertyName, false);

                _driveSettingsPanel.Children.Add(checkBox);
            }

            // System drive setting
            var systemDriveCheckBox = new CheckBox
            {
                Content = "Hide System Drive (C:)",
                IsChecked = settings.HideSystemDrive,
                Margin = new Thickness(0,8,0,8)
            };
            systemDriveCheckBox.Checked += (s, e) => UpdateDriveSetting(nameof(settings.HideSystemDrive), true);
            systemDriveCheckBox.Unchecked += (s, e) => UpdateDriveSetting(nameof(settings.HideSystemDrive), false);
            _driveSettingsPanel.Children.Add(systemDriveCheckBox);

            // Individual Drive Letter Hiding Section
            CreateDriveLetterHidingSection();

            // Note about USB hard drives
            var note = new TextBlock
            {
                Text = "?? Tip: Enable 'Show Internal/Fixed Drives' to see USB pluggable hard drives that aren't detected as removable.",
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 149, 237)),
                Margin = new Thickness(0,12,0,0),
                TextWrapping = TextWrapping.Wrap
            };
            _driveSettingsPanel.Children.Add(note);
        }

        private void CreateDriveLetterHidingSection()
        {
            var settings = _settingsService.GetSettings();

            // Section header
            var sectionHeader = new TextBlock
            {
                Text = "Hide Specific Drive Letters:",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 14,
                Margin = new Thickness(0,16,0,8)
            };
            _driveSettingsPanel.Children.Add(sectionHeader);

            var description = new TextBlock
            {
                Text = "Select individual drive letters to hide from the main drive list:",
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Margin = new Thickness(0,0,0,8),
                TextWrapping = TextWrapping.Wrap
            };
            _driveSettingsPanel.Children.Add(description);

            // Get all available drives on the system
            var availableDrives = GetAllSystemDrives();
            
            if (availableDrives.Any())
            {
                // Create a grid for drive checkboxes (3 columns)
                var driveGrid = new Grid();
                driveGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                driveGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                driveGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                int row = 0;
                int col = 0;

                foreach (var driveInfo in availableDrives)
                {
                    var driveLetter = driveInfo.Name.TrimEnd('\\');
                    var isHidden = settings.HiddenDriveLetters.Contains(driveLetter);
                    
                    // Create drive display info
                    var driveLabel = driveInfo.VolumeLabel;
                    var driveSize = "";
                    try
                    {
                        var sizeGB = Math.Round(driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0), 1);
                        driveSize = $" ({sizeGB}GB)";
                    }
                    catch { }

                    var displayText = string.IsNullOrEmpty(driveLabel) 
                        ? $"{driveLetter} [{driveInfo.DriveType}]{driveSize}" 
                        : $"{driveLetter} - {driveLabel}{driveSize}";

                    var checkBox = new CheckBox
                    {
                        Content = displayText,
                        IsChecked = isHidden,
                        Margin = new Thickness(0,0,8,8),
                        FontSize = 12
                    };

                    // Add row if needed
                    if (driveGrid.RowDefinitions.Count <= row)
                    {
                        driveGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    }

                    Grid.SetRow(checkBox, row);
                    Grid.SetColumn(checkBox, col);
                    driveGrid.Children.Add(checkBox);

                    // Handle checkbox changes
                    var currentDriveLetter = driveLetter; // Capture for closure
                    checkBox.Checked += (s, e) => ToggleDriveLetterVisibility(currentDriveLetter, true);
                    checkBox.Unchecked += (s, e) => ToggleDriveLetterVisibility(currentDriveLetter, false);

                    // Move to next position
                    col++;
                    if (col >= 3)
                    {
                        col = 0;
                        row++;
                    }
                }

                _driveSettingsPanel.Children.Add(driveGrid);
            }
            else
            {
                var noDrivesText = new TextBlock
                {
                    Text = "No drives detected on the system.",
                    FontStyle = Windows.UI.Text.FontStyle.Italic,
                    Margin = new Thickness(0,0,0,8)
                };
                _driveSettingsPanel.Children.Add(noDrivesText);
            }

            // Quick action buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0,8,0,0),
                Spacing = 8
            };

            var showAllButton = new Button
            {
                Content = "Show All Drives",
                FontSize = 12,
                Padding = new Thickness(12,6,12,6)
            };
            showAllButton.Click += (s, e) => ClearAllHiddenDrives();

            var hideAllButton = new Button
            {
                Content = "Hide All Non-System",
                FontSize = 12,
                Padding = new Thickness(12,6,12,6)
            };
            hideAllButton.Click += (s, e) => HideAllNonSystemDrives();

            buttonPanel.Children.Add(showAllButton);
            buttonPanel.Children.Add(hideAllButton);
            _driveSettingsPanel.Children.Add(buttonPanel);
        }

        private List<DriveInfo> GetAllSystemDrives()
        {
            try
            {
                return DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .OrderBy(d => d.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting system drives: {ex.Message}");
                return new List<DriveInfo>();
            }
        }

        private void ToggleDriveLetterVisibility(string driveLetter, bool hide)
        {
            var settings = _settingsService.GetSettings();
            
            if (hide && !settings.HiddenDriveLetters.Contains(driveLetter))
            {
                settings.HiddenDriveLetters.Add(driveLetter);
                System.Diagnostics.Debug.WriteLine($"?? Hidden drive letter: {driveLetter}");
            }
            else if (!hide && settings.HiddenDriveLetters.Contains(driveLetter))
            {
                settings.HiddenDriveLetters.Remove(driveLetter);
                System.Diagnostics.Debug.WriteLine($"?? Unhidden drive letter: {driveLetter}");
            }
            
            _settingsService.SaveSettings(settings);
        }

        private void ClearAllHiddenDrives()
        {
            var settings = _settingsService.GetSettings();
            settings.HiddenDriveLetters.Clear();
            _settingsService.SaveSettings(settings);
            RefreshDriveSettingsPanel(); // Refresh to update checkboxes
            System.Diagnostics.Debug.WriteLine("?? Cleared all hidden drive letters");
        }

        private void HideAllNonSystemDrives()
        {
            var settings = _settingsService.GetSettings();
            var allDrives = GetAllSystemDrives();
            
            settings.HiddenDriveLetters.Clear();
            foreach (var drive in allDrives)
            {
                var driveLetter = drive.Name.TrimEnd('\\');
                if (!driveLetter.ToUpper().StartsWith("C"))
                {
                    settings.HiddenDriveLetters.Add(driveLetter);
                }
            }
            
            _settingsService.SaveSettings(settings);
            RefreshDriveSettingsPanel(); // Refresh to update checkboxes
            System.Diagnostics.Debug.WriteLine($"?? Hidden all non-system drives: {string.Join(", ", settings.HiddenDriveLetters)}");
        }

        private void UpdateDriveSetting(string propertyName, bool value)
        {
            var settings = _settingsService.GetSettings();
            var property = typeof(DriveDisplaySettings).GetProperty(propertyName);
            property?.SetValue(settings, value);
            _settingsService.SaveSettings(settings);
            
            System.Diagnostics.Debug.WriteLine($"?? Updated drive setting {propertyName} = {value}");
        }

        private void RefreshFoldersPanel()
        {
            _foldersPanel.Children.Clear();
            foreach (var folder in _folders)
            {
                var folderPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 4)
                };

                folderPanel.Children.Add(new TextBlock 
                { 
                    Text = folder, 
                    VerticalAlignment = VerticalAlignment.Center, 
                    Margin = new Thickness(0, 0, 8, 0) 
                });

                var removeButton = new Button
                {
                    Content = "Remove",
                    FontSize = 12,
                    Padding = new Thickness(8, 4, 8, 4),
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 53, 69))
                };

                var currentFolder = folder; // Capture for closure
                removeButton.Click += async (s, e) => await RemoveFolderAsync(currentFolder);

                folderPanel.Children.Add(removeButton);
                _foldersPanel.Children.Add(folderPanel);
            }
        }

        private async Task AddFolderAsync()
        {
            try
            {
                var picker = new FolderPicker();
                picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                picker.FileTypeFilter.Add("*");

                if (App.MainWindow != null)
                {
                    var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
                    InitializeWithWindow.Initialize(picker, hwnd);
                }

                // Convert WinRT IAsyncOperation to Task
                var operation = picker.PickSingleFolderAsync();
                StorageFolder? storageFolder = null;

                // Use TaskCompletionSource to convert WinRT async to .NET Task
                var tcs = new TaskCompletionSource<StorageFolder?>();
                operation.Completed = (asyncInfo, status) =>
                {
                    try
                    {
                        if (status == Windows.Foundation.AsyncStatus.Completed)
                        {
                            tcs.SetResult(asyncInfo.GetResults());
                        }
                        else
                        {
                            tcs.SetResult(null);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                };

                storageFolder = await tcs.Task;

                if (storageFolder != null)
                {
                    var folderPath = storageFolder.Path;
                    if (!_folders.Contains(folderPath))
                    {
                        await _libraryService.AddGameFolderAsync(folderPath);
                        _folders.Add(folderPath);
                        RefreshFoldersPanel();
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding folder: {ex.Message}");
            }
        }

        private async Task RemoveFolderAsync(string folderPath)
        {
            try
            {
                await _libraryService.RemoveGameFolderAsync(folderPath);
                _folders.Remove(folderPath);
                RefreshFoldersPanel();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing folder: {ex.Message}");
            }
        }
    }
}