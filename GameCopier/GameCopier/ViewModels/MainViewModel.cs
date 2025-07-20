using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Timers;
using GameCopier.Models;
using GameCopier.Services;
using Microsoft.UI.Dispatching;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace GameCopier.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly LibraryService _libraryService;
        private readonly DriveService _driveService;
        private readonly DeploymentService _deploymentService;
        private readonly UsbDriveDetectionService _usbDetectionService;
        private readonly Timer _driveMonitorTimer;
        private readonly DispatcherQueue? _uiDispatcher;
        private int _usbEventCount = 0;

        private string _searchText = string.Empty;
        private double _overallProgress = 0.0;
        private bool _isDeploymentRunning = false;
        private string _statusText = "Ready";

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterGames();
            }
        }

        public double OverallProgress
        {
            get => _overallProgress;
            set
            {
                _overallProgress = value;
                OnPropertyChanged();
            }
        }

        public bool IsDeploymentRunning
        {
            get => _isDeploymentRunning;
            set
            {
                _isDeploymentRunning = value;
                OnPropertyChanged();
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
                System.Diagnostics.Debug.WriteLine($"Status: {value}");
            }
        }

        public ObservableCollection<Game> Games { get; } = new();
        public ObservableCollection<Game> FilteredGames { get; } = new();
        public ObservableCollection<Drive> AvailableDrives { get; } = new();
        public ObservableCollection<DeploymentJob> DeploymentJobs { get; } = new();

        public ICommand LoadGamesCommand { get; }
        public ICommand RefreshLibraryCommand { get; }
        public ICommand LoadDrivesCommand { get; }
        public ICommand StartDeploymentCommand { get; }
        public ICommand CancelDeploymentCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand AddGameFolderCommand { get; }

        public event EventHandler? RequestSettingsDialog;

        public MainViewModel()
        {
            System.Diagnostics.Debug.WriteLine("?? Initializing MainViewModel with USB-only detection...");
            
            // CRITICAL: Capture the UI dispatcher during initialization (on UI thread)
            _uiDispatcher = DispatcherQueue.GetForCurrentThread();
            if (_uiDispatcher == null)
            {
                System.Diagnostics.Debug.WriteLine("? CRITICAL: Could not get UI dispatcher during initialization!");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("? UI dispatcher captured successfully");
            }
            
            _libraryService = new LibraryService();
            _driveService = new DriveService();
            _deploymentService = new DeploymentService();
            _usbDetectionService = new UsbDriveDetectionService();

            _deploymentService.JobUpdated += OnJobUpdated;
            _deploymentService.OverallProgressUpdated += OnOverallProgressUpdated;
            _usbDetectionService.UsbDriveChanged += OnUsbDriveChanged;

            LoadGamesCommand = new RelayCommand(async () => await LoadGamesAsync());
            RefreshLibraryCommand = new RelayCommand(async () => await RefreshLibraryAsync());
            LoadDrivesCommand = new RelayCommand(async () => await LoadDrivesAsync());
            StartDeploymentCommand = new RelayCommand(async () => await StartDeploymentAsync());
            CancelDeploymentCommand = new RelayCommand(CancelDeployment);
            ShowSettingsCommand = new RelayCommand(ShowSettings);
            AddGameFolderCommand = new RelayCommand(async () => await AddGameFolderAsync());

            // Backup timer monitoring (much less frequent since we have real-time detection)
            _driveMonitorTimer = new Timer(30000); // Check every 30 seconds as backup
            _driveMonitorTimer.Elapsed += OnDriveMonitorTimer;
            _driveMonitorTimer.AutoReset = true;
            _driveMonitorTimer.Start();

            System.Diagnostics.Debug.WriteLine("? MainViewModel initialization complete - USB-only mode active");
            _ = InitializeAsync();
        }

        private async void OnUsbDriveChanged(object? sender, UsbDriveChangedEventArgs e)
        {
            _usbEventCount++;
            System.Diagnostics.Debug.WriteLine($"?? USB-only drive change event #{_usbEventCount} received from BACKGROUND THREAD");
            
            if (e.AddedDrives.Any())
            {
                System.Diagnostics.Debug.WriteLine($"? Added drives: {string.Join(", ", e.AddedDrives)}");
                if (e.MostRecentDrive != null)
                {
                    System.Diagnostics.Debug.WriteLine($"? Most recent drive: {e.MostRecentDrive}");
                }
            }
            
            if (e.RemovedDrives.Any())
            {
                System.Diagnostics.Debug.WriteLine($"? Removed drives: {string.Join(", ", e.RemovedDrives)}");
            }
            
            // Real-time USB drive detection event
            if (!IsDeploymentRunning)
            {
                try
                {
                    // Handle added drives with enhanced highlighting
                    if (e.AddedDrives.Any())
                    {
                        await HandleDrivesAdded(e.AddedDrives, e.MostRecentDrive);
                    }
                    
                    // Handle removed drives
                    if (e.RemovedDrives.Any())
                    {
                        await HandleDrivesRemoved(e.RemovedDrives);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"? Error handling USB drive change: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("??  USB event ignored - deployment is running");
            }
        }

        private async Task HandleDrivesAdded(List<string> addedDrives, string? mostRecentDrive)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("?? Handling drives added with enhanced highlighting...");
                
                if (_uiDispatcher == null)
                {
                    System.Diagnostics.Debug.WriteLine("? No UI dispatcher available!");
                    return;
                }
                
                // Get fresh drive data with highlighting
                var drives = await _driveService.GetRemovableDrivesWithHighlightAsync(mostRecentDrive);
                System.Diagnostics.Debug.WriteLine($"?? Got {drives.Count()} USB drives from service");
                
                // Update UI on the captured UI thread with enhanced logic from USBDriveDitector
                var success = _uiDispatcher.TryEnqueue(() =>
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("?? EXECUTING ON UI THREAD: Enhanced drive addition handling...");
                        
                        // Clear the "newly inserted" flag from all existing drives (USBDriveDitector pattern)
                        foreach (var existingDrive in AvailableDrives)
                        {
                            existingDrive.IsRecentlyPlugged = false;
                        }
                        
                        // Preserve selection state when refreshing
                        var previousSelections = AvailableDrives.Where(d => d.IsSelected).Select(d => d.DriveLetter).ToList();
                        System.Diagnostics.Debug.WriteLine($"?? Preserving selections for: {string.Join(", ", previousSelections)}");
                        
                        AvailableDrives.Clear();
                        
                        foreach (var drive in drives)
                        {
                            // Restore selection if drive was previously selected
                            if (previousSelections.Contains(drive.DriveLetter))
                            {
                                drive.IsSelected = true;
                                System.Diagnostics.Debug.WriteLine($"?? Restored selection for drive: {drive.DriveLetter}");
                            }
                            
                            // Set insertion time for newly added drives
                            if (addedDrives.Contains(drive.DriveLetter))
                            {
                                drive.InsertedTime = DateTime.Now;
                                
                                // Create enhanced status message with brand info
                                var brandInfo = !string.IsNullOrEmpty(drive.BrandName) ? $" ({drive.BrandName})" : "";
                                var driveDescription = !string.IsNullOrEmpty(drive.Label) ? drive.Label : "USB Drive";
                                StatusText = $"?? USB drive inserted: {driveDescription}{brandInfo} - {drive.DriveLetter}";
                                
                                System.Diagnostics.Debug.WriteLine($"? Drive added with enhanced info: {drive.DriveLetter} - {drive.EnhancedDescription}");
                                
                                // Schedule highlight removal after 30 seconds (USBDriveDitector pattern)
                                _ = Task.Delay(30000).ContinueWith(_ =>
                                {
                                    _uiDispatcher?.TryEnqueue(() =>
                                    {
                                        if (AvailableDrives.Any(d => d.DriveLetter == drive.DriveLetter))
                                        {
                                            var driveToUpdate = AvailableDrives.FirstOrDefault(d => d.DriveLetter == drive.DriveLetter);
                                            if (driveToUpdate != null)
                                            {
                                                driveToUpdate.IsRecentlyPlugged = false;
                                                System.Diagnostics.Debug.WriteLine($"? Removed highlight from drive: {drive.DriveLetter}");
                                            }
                                        }
                                    });
                                });
                            }
                            
                            AvailableDrives.Add(drive);
                        }
                        
                        UpdateStatusText();
                        System.Diagnostics.Debug.WriteLine($"? Enhanced drive addition complete - {AvailableDrives.Count} USB drives now in UI");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"? UI UPDATE ERROR: {ex.Message}");
                    }
                });
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("? Successfully queued enhanced UI update");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("? Failed to queue UI update");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error in HandleDrivesAdded: {ex.Message}");
            }
        }

        private async Task HandleDrivesRemoved(List<string> removedDrives)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("?? Handling drives removed...");
                
                if (_uiDispatcher == null)
                {
                    System.Diagnostics.Debug.WriteLine("? No UI dispatcher available!");
                    return;
                }
                
                // Update UI on the captured UI thread with USBDriveDitector pattern
                var success = _uiDispatcher.TryEnqueue(() =>
                {
                    try
                    {
                        foreach (var removedDriveLetter in removedDrives)
                        {
                            var driveToRemove = AvailableDrives.FirstOrDefault(d => d.DriveLetter == removedDriveLetter);
                            if (driveToRemove != null)
                            {
                                // Enhanced status message with brand info (USBDriveDitector pattern)
                                var driveDescription = !string.IsNullOrEmpty(driveToRemove.BrandName) 
                                    ? $"{driveToRemove.Label} ({driveToRemove.BrandName})" 
                                    : driveToRemove.Label ?? "USB Drive";
                                
                                AvailableDrives.Remove(driveToRemove);
                                StatusText = $"?? USB drive removed: {driveDescription} - {removedDriveLetter}";
                                System.Diagnostics.Debug.WriteLine($"? Drive removed from UI: {removedDriveLetter}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"?? Drive not found in UI for removal: {removedDriveLetter}");
                            }
                        }
                        
                        UpdateStatusText();
                        
                        // Reset status after a moment
                        _ = Task.Delay(3000).ContinueWith(_ =>
                        {
                            _uiDispatcher?.TryEnqueue(() => 
                            {
                                if (StatusText.Contains("USB drive removed"))
                                {
                                    UpdateStatusText();
                                }
                            });
                        });
                        
                        System.Diagnostics.Debug.WriteLine($"? Drive removal complete - {AvailableDrives.Count} USB drives remaining");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"? UI UPDATE ERROR: {ex.Message}");
                    }
                });
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("? Successfully queued drive removal UI update");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("? Failed to queue drive removal UI update");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error in HandleDrivesRemoved: {ex.Message}");
            }
        }

        private async void OnDriveMonitorTimer(object? sender, ElapsedEventArgs e)
        {
            // Backup monitoring (much less frequent)
            if (!IsDeploymentRunning)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("? Backup timer: Checking USB drives...");
                    await LoadDrivesAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"? Backup timer error: {ex.Message}");
                }
            }
        }

        private void ShowSettings()
        {
            RequestSettingsDialog?.Invoke(this, EventArgs.Empty);
        }

        private async Task InitializeAsync()
        {
            try
            {
                StatusText = "?? Initializing GameDeploy Kiosk with USB-only detection...";
                await LoadGamesAsync();
                await LoadDrivesAsync();
                StatusText = "? Initialization complete - USB-only monitoring active!";
                
                // Show initial status after a moment
                await Task.Delay(2000);
                UpdateStatusText();
            }
            catch (Exception ex)
            {
                StatusText = $"? Initialization error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"? Initialization error: {ex.Message}");
            }
        }

        private async Task LoadGamesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("?? Loading games...");
                var games = await _libraryService.GetGamesAsync();
                
                // Update on UI thread using stored dispatcher
                if (_uiDispatcher != null)
                {
                    _ = _uiDispatcher.TryEnqueue(() =>
                    {
                        Games.Clear();
                        foreach (var game in games)
                        {
                            Games.Add(game);
                        }
                        FilterGames();
                        UpdateStatusText();
                        System.Diagnostics.Debug.WriteLine($"? Loaded {games.Count()} games");
                    });
                }
            }
            catch (Exception ex)
            {
                StatusText = $"? Error loading games: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"? Error loading games: {ex.Message}");
            }
        }

        private async Task RefreshLibraryAsync()
        {
            try
            {
                StatusText = "?? Scanning game library...";
                System.Diagnostics.Debug.WriteLine("?? Refreshing game library...");
                var games = await _libraryService.ScanLibraryAsync();
                
                // Update on UI thread using stored dispatcher
                if (_uiDispatcher != null)
                {
                    _ = _uiDispatcher.TryEnqueue(() =>
                    {
                        Games.Clear();
                        foreach (var game in games)
                        {
                            Games.Add(game);
                        }
                        FilterGames();
                        UpdateStatusText();
                        System.Diagnostics.Debug.WriteLine($"? Refreshed library with {games.Count()} games");
                    });
                }
            }
            catch (Exception ex)
            {
                StatusText = $"? Error refreshing library: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"? Error refreshing library: {ex.Message}");
            }
        }

        private async Task LoadDrivesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("?? === LoadDrivesAsync STARTED ===");
                
                // Get the current most recent drive from the detection service
                var mostRecentDrive = _usbDetectionService.CurrentMostRecentDrive;
                System.Diagnostics.Debug.WriteLine($"?? Most recent drive from detection service: {mostRecentDrive ?? "none"}");
                
                // Use the highlighting method to include most recent drive info
                System.Diagnostics.Debug.WriteLine("?? Calling GetRemovableDrivesWithHighlightAsync...");
                var drives = await _driveService.GetRemovableDrivesWithHighlightAsync(mostRecentDrive);
                System.Diagnostics.Debug.WriteLine($"?? GetRemovableDrivesWithHighlightAsync returned {drives.Count()} drives");
                
                // Log each drive returned
                foreach (var drive in drives)
                {
                    System.Diagnostics.Debug.WriteLine($"?? Service returned drive: {drive.DriveLetter} - {drive.Name}");
                }
                
                // Update on UI thread using stored dispatcher
                if (_uiDispatcher != null)
                {
                    System.Diagnostics.Debug.WriteLine("?? Queueing UI update...");
                    var success = _uiDispatcher.TryEnqueue(() =>
                    {
                        UpdateDriveListDirectly(drives.ToList());
                    });
                    System.Diagnostics.Debug.WriteLine($"?? UI update queued successfully: {success}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("?? ERROR: No UI dispatcher available!");
                }
                
                System.Diagnostics.Debug.WriteLine("?? === LoadDrivesAsync COMPLETED ===");
            }
            catch (Exception ex)
            {
                StatusText = $"? Error loading USB drives: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"? Error loading USB drives: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"? Stack trace: {ex.StackTrace}");
            }
        }

        private async Task StartDeploymentAsync()
        {
            if (IsDeploymentRunning) return;

            var selectedGames = Games.Where(g => g.IsSelected).ToList();
            var selectedDrives = AvailableDrives.Where(d => d.IsSelected).ToList();

            if (!selectedGames.Any())
            {
                StatusText = "?? Please select at least one game to deploy.";
                return;
            }

            if (!selectedDrives.Any())
            {
                StatusText = "?? Please select at least one USB drive.";
                return;
            }

            // Check if drives have sufficient space
            foreach (var drive in selectedDrives)
            {
                if (!_driveService.HasSufficientSpace(drive, selectedGames))
                {
                    StatusText = $"?? USB drive {drive.Name} does not have sufficient space for selected games.";
                    return;
                }
            }

            try
            {
                IsDeploymentRunning = true;
                _driveMonitorTimer.Stop(); // Stop monitoring during deployment
                DeploymentJobs.Clear();

                _deploymentService.QueueMultipleDeployments(selectedGames, selectedDrives);
                
                StatusText = $"?? Starting deployment of {selectedGames.Count} games to {selectedDrives.Count} USB drives...";
                System.Diagnostics.Debug.WriteLine($"?? Starting deployment: {selectedGames.Count} games to {selectedDrives.Count} USB drives");
                
                await _deploymentService.StartDeploymentAsync();
                
                StatusText = "? Deployment completed successfully!";
                System.Diagnostics.Debug.WriteLine("? Deployment completed successfully");
            }
            catch (Exception ex)
            {
                StatusText = $"? Deployment error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"? Deployment error: {ex.Message}");
            }
            finally
            {
                IsDeploymentRunning = false;
                OverallProgress = 0;
                _driveMonitorTimer.Start(); // Resume monitoring
            }
        }

        private void CancelDeployment()
        {
            if (!IsDeploymentRunning) return;

            _deploymentService.CancelAllJobs();
            StatusText = "?? Deployment cancelled by user.";
            IsDeploymentRunning = false;
            OverallProgress = 0;
            _driveMonitorTimer.Start(); // Resume monitoring
            System.Diagnostics.Debug.WriteLine("?? Deployment cancelled by user");
        }

        private async Task AddGameFolderAsync()
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            folderPicker.FileTypeFilter.Add("*");

            // Use the static MainWindow property from App
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(GameCopier.App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                await _libraryService.AddGameFolderAsync(folder.Path);
                await LoadGamesAsync(); // Refresh the game list
            }
        }

        private void FilterGames()
        {
            FilteredGames.Clear();
            
            var filtered = string.IsNullOrWhiteSpace(SearchText) 
                ? Games 
                : _libraryService.SearchGames(SearchText);

            foreach (var game in filtered)
            {
                FilteredGames.Add(game);
            }
        }

        private void UpdateStatusText()
        {
            var selectedGames = Games.Where(g => g.IsSelected).ToList();
            var selectedDrives = AvailableDrives.Where(d => d.IsSelected).ToList();
            var mostRecentDrive = AvailableDrives.FirstOrDefault(d => d.IsRecentlyPlugged);

            if (selectedGames.Any() && selectedDrives.Any())
            {
                var totalSize = selectedGames.Sum(g => g.SizeInBytes);
                var sizeText = FormatBytes(totalSize);
                StatusText = $"? {selectedGames.Count} games ({sizeText}) selected for {selectedDrives.Count} USB drives. Ready to deploy!";
            }
            else if (selectedGames.Any())
            {
                var totalSize = selectedGames.Sum(g => g.SizeInBytes);
                var sizeText = FormatBytes(totalSize);
                StatusText = $"?? {selectedGames.Count} games ({sizeText}) selected. Please select USB drives.";
            }
            else if (selectedDrives.Any())
            {
                StatusText = $"?? {selectedDrives.Count} USB drives selected. Please select games to deploy.";
            }
            else
            {
                var driveText = AvailableDrives.Count > 0 ? $"{AvailableDrives.Count} USB drives" : "No USB drives";
                var eventText = _usbEventCount > 0 ? $" | {_usbEventCount} USB events detected" : "";
                var recentText = mostRecentDrive != null ? $" | Most recent: {mostRecentDrive.DriveLetter}" : "";
                StatusText = $"?? {Games.Count} games available, {driveText} detected{eventText}{recentText}. USB-only monitoring active!";
            }
        }

        private void OnJobUpdated(object? sender, DeploymentJob job)
        {
            // Update on UI thread using stored dispatcher
            _uiDispatcher?.TryEnqueue(() =>
            {
                var existingJob = DeploymentJobs.FirstOrDefault(j => j.Id == job.Id);
                if (existingJob == null)
                {
                    DeploymentJobs.Add(job);
                }
                else
                {
                    var index = DeploymentJobs.IndexOf(existingJob);
                    DeploymentJobs[index] = job;
                }
            });
        }

        private void OnOverallProgressUpdated(object? sender, double progress)
        {
            _uiDispatcher?.TryEnqueue(() =>
            {
                OverallProgress = progress;
            });
        }

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:F1} {suffixes[suffixIndex]}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            System.Diagnostics.Debug.WriteLine("?? Disposing MainViewModel...");
            _driveMonitorTimer?.Stop();
            _driveMonitorTimer?.Dispose();
            _usbDetectionService?.Dispose();
        }

        private void UpdateDriveListDirectly(List<Drive> drives)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"?? UpdateDriveListDirectly: Processing {drives.Count} USB drives ON UI THREAD");
                
                // Preserve selection state when refreshing
                var previousSelections = AvailableDrives.Where(d => d.IsSelected).Select(d => d.DriveLetter).ToList();
                System.Diagnostics.Debug.WriteLine($"?? Preserving selections for: {string.Join(", ", previousSelections)}");
                
                AvailableDrives.Clear();
                System.Diagnostics.Debug.WriteLine("?? Cleared existing drives");
                
                foreach (var drive in drives)
                {
                    // Restore selection if drive was previously selected
                    if (previousSelections.Contains(drive.DriveLetter))
                    {
                        drive.IsSelected = true;
                        System.Diagnostics.Debug.WriteLine($"?? Restored selection for drive: {drive.DriveLetter}");
                    }
                    AvailableDrives.Add(drive);
                    
                    var highlightStatus = drive.IsRecentlyPlugged ? " (HIGHLIGHTED)" : "";
                    var brandInfo = !string.IsNullOrEmpty(drive.BrandName) ? $" - {drive.BrandName}" : "";
                    System.Diagnostics.Debug.WriteLine($"?? Added drive: {drive.DriveLetter} - {drive.Name}{brandInfo}{highlightStatus}");
                }
                
                UpdateStatusText();
                System.Diagnostics.Debug.WriteLine($"? UpdateDriveListDirectly: Complete - {AvailableDrives.Count} USB drives now in UI");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? UpdateDriveListDirectly: Error - {ex.Message}");
            }
        }
    }

    // Simple RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}