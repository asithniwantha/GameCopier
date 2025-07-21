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
        private ObservableCollection<string> _gameFolders;
        private ObservableCollection<string> _softwareFolders;
        private StackPanel _foldersPanel = new();
        private StackPanel _driveSettingsPanel = new();
        private StackPanel _gamesFoldersPanel;
        private StackPanel _softwareFoldersPanel;

        public SettingsDialog()
        {
            this.XamlRoot = App.MainWindow?.Content?.XamlRoot;
            this.Title = "Settings";
            this.PrimaryButtonText = "Close";
            this.PrimaryButtonClick += SettingsDialog_PrimaryButtonClick;
            
            _libraryService = new LibraryService();
            _settingsService = new SettingsService();
            _gameFolders = new ObservableCollection<string>(_libraryService.GetGameFolders());
            _softwareFolders = new ObservableCollection<string>(_libraryService.GetSoftwareFolders());

            // Create main scroll viewer
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = 600
            };

            var mainPanel = new StackPanel { Spacing = 20 };

            // Library Section with TabView
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
                Text = "Library Folders:", 
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 16,
                Margin = new Thickness(0,0,0,8) 
            };
            section.Children.Add(header);

            // Create TabView for Games and Software folders
            var tabView = new TabView();
            
            // Games Tab
            var gamesTab = new TabViewItem { Header = "Game Folders" };
            _gamesFoldersPanel = new StackPanel();
            RefreshGameFoldersPanel(_gamesFoldersPanel);
            var addGameButton = new Button 
            { 
                Content = "Add Game Folder", 
                Margin = new Thickness(0,8,0,0) 
            };
            addGameButton.Click += async (s, e) => await AddGameFolderAsync();
            var gamesContent = new StackPanel();
            gamesContent.Children.Add(_gamesFoldersPanel);
            gamesContent.Children.Add(addGameButton);
            gamesTab.Content = gamesContent;
            
            // Software Tab
            var softwareTab = new TabViewItem { Header = "Software Folders" };
            _softwareFoldersPanel = new StackPanel();
            RefreshSoftwareFoldersPanel(_softwareFoldersPanel);
            var addSoftwareButton = new Button 
            { 
                Content = "Add Software Folder", 
                Margin = new Thickness(0,8,0,0) 
            };
            addSoftwareButton.Click += async (s, e) => await AddSoftwareFolderAsync();
            var softwareContent = new StackPanel();
            softwareContent.Children.Add(_softwareFoldersPanel);
            softwareContent.Children.Add(addSoftwareButton);
            softwareTab.Content = softwareContent;
            
            tabView.TabItems.Add(gamesTab);
            tabView.TabItems.Add(softwareTab);
            section.Children.Add(tabView);
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

        private void RefreshGameFoldersPanel(StackPanel panel)
        {
            panel.Children.Clear();
            foreach (var folder in _gameFolders)
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
                removeButton.Click += async (s, e) => await RemoveGameFolderAsync(currentFolder, panel);

                folderPanel.Children.Add(removeButton);
                panel.Children.Add(folderPanel);
            }
        }

        private void RefreshSoftwareFoldersPanel(StackPanel panel)
        {
            panel.Children.Clear();
            foreach (var folder in _softwareFolders)
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
                removeButton.Click += async (s, e) => await RemoveSoftwareFolderAsync(currentFolder, panel);

                folderPanel.Children.Add(removeButton);
                panel.Children.Add(folderPanel);
            }
        }

        private async Task AddGameFolderAsync()
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

                var operation = picker.PickSingleFolderAsync();
                StorageFolder? storageFolder = null;

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
                    if (!_gameFolders.Contains(folderPath))
                    {
                        await _libraryService.AddGameFolderAsync(folderPath);
                        _gameFolders.Add(folderPath);
                        RefreshGameFoldersPanel(_gamesFoldersPanel); // Refresh after add
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding game folder: {ex.Message}");
            }
        }

        private async Task AddSoftwareFolderAsync()
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

                var operation = picker.PickSingleFolderAsync();
                StorageFolder? storageFolder = null;

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
                    if (!_softwareFolders.Contains(folderPath))
                    {
                        await _libraryService.AddSoftwareFolderAsync(folderPath);
                        _softwareFolders.Add(folderPath);
                        RefreshSoftwareFoldersPanel(_softwareFoldersPanel); // Refresh after add
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding software folder: {ex.Message}");
            }
        }

        private async Task RemoveGameFolderAsync(string folderPath, StackPanel panel)
        {
            try
            {
                await _libraryService.RemoveGameFolderAsync(folderPath);
                _gameFolders.Remove(folderPath);
                RefreshGameFoldersPanel(panel);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing game folder: {ex.Message}");
            }
        }

        private async Task RemoveSoftwareFolderAsync(string folderPath, StackPanel panel)
        {
            try
            {
                await _libraryService.RemoveSoftwareFolderAsync(folderPath);
                _softwareFolders.Remove(folderPath);
                RefreshSoftwareFoldersPanel(panel);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing software folder: {ex.Message}");
            }
        }

        private void SettingsDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Save folders
            _libraryService.SaveGameFolders(_gameFolders.ToList());
            _libraryService.SaveSoftwareFolders(_softwareFolders.ToList());

            // Save drive display settings from UI
            var settings = _settingsService.GetSettings();
            int driveTypeIndex = 0;
            var driveTypeProperties = new[]
            {
                nameof(settings.ShowRemovableDrives),
                nameof(settings.ShowFixedDrives),
                nameof(settings.ShowNetworkDrives),
                nameof(settings.ShowCdRomDrives),
                nameof(settings.ShowRamDrives),
                nameof(settings.ShowUnknownDrives)
            };
            // First 6 checkboxes are drive types
            foreach (var child in _driveSettingsPanel.Children)
            {
                if (child is CheckBox cb && driveTypeIndex < driveTypeProperties.Length)
                {
                    typeof(DriveDisplaySettings).GetProperty(driveTypeProperties[driveTypeIndex])?.SetValue(settings, cb.IsChecked == true);
                    driveTypeIndex++;
                }
            }
            // 7th checkbox is HideSystemDrive
            var systemDriveCheckBox = _driveSettingsPanel.Children.OfType<CheckBox>().Skip(6).FirstOrDefault();
            if (systemDriveCheckBox != null)
            {
                settings.HideSystemDrive = systemDriveCheckBox.IsChecked == true;
            }
            // Save settings
            _settingsService.SaveSettings(settings);
        }
    }
}