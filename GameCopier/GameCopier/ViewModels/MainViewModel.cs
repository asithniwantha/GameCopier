using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
        private string _softwareSearchText = string.Empty;
        private double _overallProgress = 0.0;
        private bool _isDeploymentRunning = false;
        private string _statusText = "Ready";

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    System.Diagnostics.Debug.WriteLine($"?? SearchText changed to: '{value}'");
                    OnPropertyChanged();
                    FilterGames();
                }
            }
        }

        public string SoftwareSearchText
        {
            get => _softwareSearchText;
            set
            {
                if (_softwareSearchText != value)
                {
                    _softwareSearchText = value;
                    System.Diagnostics.Debug.WriteLine($"?? SoftwareSearchText changed to: '{value}'");
                    OnPropertyChanged();
                    FilterSoftware();
                }
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
        public ObservableCollection<Software> Software { get; } = new();
        public ObservableCollection<Software> FilteredSoftware { get; } = new();
        public ObservableCollection<Drive> AvailableDrives { get; } = new();
        public ObservableCollection<DeploymentJob> DeploymentJobs { get; } = new();

        public string GameSearchResultsText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                    return "";
                
                var count = FilteredGames.Count;
                var total = Games.Count;
                var result = count == 1 ? $"Found 1 game out of {total}" : $"Found {count} games out of {total}";
                System.Diagnostics.Debug.WriteLine($"?? GameSearchResultsText: '{result}'");
                return result;
            }
        }

        public string SoftwareSearchResultsText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SoftwareSearchText))
                    return "";
                
                var count = FilteredSoftware.Count;
                var total = Software.Count;
                var result = count == 1 ? $"Found 1 software out of {total}" : $"Found {count} software out of {total}";
                System.Diagnostics.Debug.WriteLine($"?? SoftwareSearchResultsText: '{result}'");
                return result;
            }
        }

        public ICommand LoadGamesCommand { get; }
        public ICommand RefreshLibraryCommand { get; }
        public ICommand LoadSoftwareCommand { get; }
        public ICommand RefreshSoftwareLibraryCommand { get; }
        public ICommand LoadDrivesCommand { get; }
        public ICommand StartDeploymentCommand { get; }
        public ICommand CancelDeploymentCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand AddGameFolderCommand { get; }
        public ICommand AddSoftwareFolderCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand ClearSoftwareSearchCommand { get; }
        public ICommand OpenGameFolderCommand { get; }
        public ICommand OpenSoftwareFolderCommand { get; }
        
        // New copy queue commands
        public ICommand AddToQueueCommand { get; }
        public ICommand RemoveFromQueueCommand { get; }
        public ICommand ClearQueueCommand { get; }
        public ICommand StartQueueCommand { get; }
        public ICommand PauseQueueCommand { get; }
        
        // Individual job control commands
        public ICommand PauseJobCommand { get; }
        public ICommand ResumeJobCommand { get; }
        public ICommand CancelJobCommand { get; }

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
            LoadSoftwareCommand = new RelayCommand(async () => await LoadSoftwareAsync());
            RefreshSoftwareLibraryCommand = new RelayCommand(async () => await RefreshSoftwareLibraryAsync());
            LoadDrivesCommand = new RelayCommand(async () => await LoadDrivesAsync());
            StartDeploymentCommand = new RelayCommand(async () => await StartDeploymentAsync());
            CancelDeploymentCommand = new RelayCommand(CancelDeployment);
            ShowSettingsCommand = new RelayCommand(ShowSettings);
            AddGameFolderCommand = new RelayCommand(async () => await AddGameFolderAsync());
            AddSoftwareFolderCommand = new RelayCommand(async () => await AddSoftwareFolderAsync());
            ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);
            ClearSoftwareSearchCommand = new RelayCommand(() => SoftwareSearchText = string.Empty);
            OpenGameFolderCommand = new RelayCommand<Game>(OpenGameFolder);
            OpenSoftwareFolderCommand = new RelayCommand<Software>(OpenSoftwareFolder);
            
            // Initialize new copy queue commands
            AddToQueueCommand = new RelayCommand(AddToQueue, CanAddToQueue);
            RemoveFromQueueCommand = new RelayCommand<DeploymentJob>(RemoveFromQueue);
            ClearQueueCommand = new RelayCommand(ClearQueue, () => DeploymentJobs.Any());
            StartQueueCommand = new RelayCommand(async () => await StartQueueAsync(), CanStartQueue);
            PauseQueueCommand = new RelayCommand(PauseQueue, () => IsDeploymentRunning);
            
            // Individual job control commands
            PauseJobCommand = new RelayCommand<DeploymentJob>(PauseJob);
            ResumeJobCommand = new RelayCommand<DeploymentJob>(ResumeJob);
            CancelJobCommand = new RelayCommand<DeploymentJob>(CancelJob);

            // Backup timer monitoring (much less frequent since we have real-time detection)
            _driveMonitorTimer = new Timer(30000); // Check every 30 seconds as backup
            _driveMonitorTimer.Elapsed += OnDriveMonitorTimer;
            _driveMonitorTimer.AutoReset = true;
            // Don't start timer yet - wait for initialization

            System.Diagnostics.Debug.WriteLine("?? MainViewModel initialization complete - USB-only mode active");
            
            // Start initialization immediately
            _ = Task.Run(async () => await InitializeAsync());
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
                await LoadSoftwareAsync();
                await LoadDrivesAsync();
                StatusText = "? Initialization complete - USB-only monitoring active!";
                
                // Start the backup timer after initialization
                _driveMonitorTimer.Start();
                
                // Show initial status after a moment
                await Task.Delay(2000);
                UpdateStatusText();
            }
            catch (Exception ex)
            {
                StatusText = $"? Initialization error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"? Initialization error: {ex.Message}");
                
                // Still start the timer even if initialization fails
                _driveMonitorTimer.Start();
            }
        }

        private async Task LoadGamesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("?? Loading games...");
                var games = await _libraryService.GetGamesAsync();
                
                // Add sample data if no games found (for testing)
                if (!games.Any())
                {
                    System.Diagnostics.Debug.WriteLine("?? No games found, adding sample data for testing...");
                    var sampleGames = new List<Game>
                    {
                        new Game { Name = "Super Mario Bros", SizeInBytes = 100L * 1024 * 1024, FolderPath = @"C:\Games\SuperMarioBros" }, // 100MB
                        new Game { Name = "The Legend of Zelda", SizeInBytes = 200L * 1024 * 1024, FolderPath = @"C:\Games\Zelda" }, // 200MB
                        new Game { Name = "Minecraft", SizeInBytes = 500L * 1024 * 1024, FolderPath = @"C:\Games\Minecraft" }, // 500MB
                        new Game { Name = "Call of Duty", SizeInBytes = 2L * 1024 * 1024 * 1024, FolderPath = @"C:\Games\CallOfDuty" }, // 2GB
                        new Game { Name = "Among Us", SizeInBytes = 50L * 1024 * 1024, FolderPath = @"C:\Games\AmongUs" } // 50MB
                    };
                    games = sampleGames;
                }
                
                // Update on UI thread using stored dispatcher
                if (_uiDispatcher != null)
                {
                    _ = _uiDispatcher.TryEnqueue(() =>
                    {
                        Games.Clear();
                        foreach (var game in games)
                        {
                            // Subscribe to PropertyChanged to update command states
                            game.PropertyChanged += OnGamePropertyChanged;
                            Games.Add(game);
                        }
                        FilterGames();
                        UpdateStatusText();
                        OnPropertyChanged(nameof(GameSearchResultsText));
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
                var games = await _libraryService.ScanGameLibraryAsync();
                
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
                        OnPropertyChanged(nameof(GameSearchResultsText));
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

        private async Task LoadSoftwareAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("?? Loading software...");
                var software = await _libraryService.GetSoftwareAsync();
                
                // Add sample data if no software found (for testing)
                if (!software.Any())
                {
                    System.Diagnostics.Debug.WriteLine("?? No software found, adding sample data for testing...");
                    var sampleSoftware = new List<Software>
                    {
                        new Software { Name = "Microsoft Office", SizeInBytes = 3L * 1024 * 1024 * 1024, FolderPath = @"C:\Software\Office" }, // 3GB
                        new Software { Name = "Adobe Photoshop", SizeInBytes = 2L * 1024 * 1024 * 1024, FolderPath = @"C:\Software\Photoshop" }, // 2GB
                        new Software { Name = "Visual Studio Code", SizeInBytes = 300L * 1024 * 1024, FolderPath = @"C:\Software\VSCode" }, // 300MB
                        new Software { Name = "Chrome Browser", SizeInBytes = 150L * 1024 * 1024, FolderPath = @"C:\Software\Chrome" }, // 150MB
                        new Software { Name = "Notepad++", SizeInBytes = 10L * 1024 * 1024, FolderPath = @"C:\Software\NotepadPlusPlus" } // 10MB
                    };
                    software = sampleSoftware;
                }
                
                // Update on UI thread using stored dispatcher
                if (_uiDispatcher != null)
                {
                    _ = _uiDispatcher.TryEnqueue(() =>
                    {
                        Software.Clear();
                        foreach (var softwareItem in software)
                        {
                            // Subscribe to PropertyChanged to update command states
                            softwareItem.PropertyChanged += OnSoftwarePropertyChanged;
                            Software.Add(softwareItem);
                        }
                        FilterSoftware();
                        UpdateStatusText();
                        OnPropertyChanged(nameof(SoftwareSearchResultsText));
                        System.Diagnostics.Debug.WriteLine($"? Loaded {software.Count()} software items");
                    });
                }
            }
            catch (Exception ex)
            {
                StatusText = $"? Error loading software: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"? Error loading software: {ex.Message}");
            }
        }

        private async Task RefreshSoftwareLibraryAsync()
        {
            try
            {
                StatusText = "?? Scanning software library...";
                System.Diagnostics.Debug.WriteLine("?? Refreshing software library...");
                var software = await _libraryService.ScanSoftwareLibraryAsync();
                
                // Update on UI thread using stored dispatcher
                if (_uiDispatcher != null)
                {
                    _ = _uiDispatcher.TryEnqueue(() =>
                    {
                        Software.Clear();
                        foreach (var softwareItem in software)
                        {
                            Software.Add(softwareItem);
                        }
                        FilterSoftware();
                        UpdateStatusText();
                        OnPropertyChanged(nameof(SoftwareSearchResultsText));
                        System.Diagnostics.Debug.WriteLine($"? Refreshed software library with {software.Count()} items");
                    });
                }
            }
            catch (Exception ex)
            {
                StatusText = $"? Error refreshing software library: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"? Error refreshing software library: {ex.Message}");
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
            var selectedSoftware = Software.Where(s => s.IsSelected).ToList();
            var selectedDrives = AvailableDrives.Where(d => d.IsSelected).ToList();

            if (!selectedGames.Any() && !selectedSoftware.Any())
            {
                StatusText = "?? Please select at least one game or software to deploy.";
                return;
            }

            if (!selectedDrives.Any())
            {
                StatusText = "?? Please select at least one USB drive.";
                return;
            }

            // Combine games and software for space checking
            var allSelectedItems = selectedGames.Cast<object>().Concat(selectedSoftware.Cast<object>()).ToList();
            var totalSize = selectedGames.Sum(g => g.SizeInBytes) + selectedSoftware.Sum(s => s.SizeInBytes);

            // Check if drives have sufficient space
            foreach (var drive in selectedDrives)
            {
                if (drive.FreeSizeInBytes < totalSize)
                {
                    StatusText = $"?? USB drive {drive.Name} does not have sufficient space for selected items.";
                    return;
                }
            }

            try
            {
                IsDeploymentRunning = true;
                _driveMonitorTimer.Stop(); // Stop monitoring during deployment
                DeploymentJobs.Clear();

                // Queue games and software for deployment
                _deploymentService.QueueMultipleDeployments(selectedGames, selectedDrives);
                // Note: You'll need to extend DeploymentService to handle Software as well
                        
                var totalItems = selectedGames.Count + selectedSoftware.Count;
                StatusText = $"?? Starting deployment of {totalItems} items to {selectedDrives.Count} USB drives...";
                System.Diagnostics.Debug.WriteLine($"?? Starting deployment: {totalItems} items to {selectedDrives.Count} USB drives");
                
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

        private async Task AddSoftwareFolderAsync()
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
                await _libraryService.AddSoftwareFolderAsync(folder.Path);
                await LoadSoftwareAsync(); // Refresh the software list
            }
        }

        private void OpenGameFolder(Game? game)
        {
            if (game == null || string.IsNullOrEmpty(game.FolderPath))
            {
                System.Diagnostics.Debug.WriteLine("? Cannot open folder: Game or FolderPath is null/empty");
                return;
            }

            OpenFolderInExplorer(game.FolderPath);
        }

        private void OpenSoftwareFolder(Software? software)
        {
            if (software == null || string.IsNullOrEmpty(software.FolderPath))
            {
                System.Diagnostics.Debug.WriteLine("? Cannot open folder: Software or FolderPath is null/empty");
                return;
            }

            OpenFolderInExplorer(software.FolderPath);
        }

        private void OpenFolderInExplorer(string folderPath)
        {
            try
            {
                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.Diagnostics.Debug.WriteLine($"? Folder does not exist: {folderPath}");
                    StatusText = $"? Folder not found: {folderPath}";
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"?? Opening folder in Explorer: {folderPath}");
                
                // Use Process.Start to open the folder in Windows Explorer
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{folderPath}\"",
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(startInfo);
                
                StatusText = $"?? Opened folder: {System.IO.Path.GetFileName(folderPath)}";
                
                // Reset status after a moment
                _ = Task.Delay(2000).ContinueWith(_ =>
                {
                    _uiDispatcher?.TryEnqueue(() => 
                    {
                        if (StatusText.Contains("Opened folder"))
                        {
                            UpdateStatusText();
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error opening folder in Explorer: {ex.Message}");
                StatusText = $"? Error opening folder: {ex.Message}";
            }
        }

        private void FilterGames()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"?? FilterGames called with SearchText: '{SearchText}'");
                
                FilteredGames.Clear();
                
                var filtered = string.IsNullOrWhiteSpace(SearchText) 
                    ? Games 
                    : _libraryService.SearchGames(SearchText);

                System.Diagnostics.Debug.WriteLine($"?? FilterGames: Found {filtered.Count()} games matching '{SearchText}'");

                foreach (var game in filtered)
                {
                    FilteredGames.Add(game);
                }
                
                // Notify search results text changed
                OnPropertyChanged(nameof(GameSearchResultsText));
                
                System.Diagnostics.Debug.WriteLine($"?? FilterGames completed: {FilteredGames.Count} games in filtered list");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error in FilterGames: {ex.Message}");
            }
        }

        private void FilterSoftware()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"?? FilterSoftware called with SoftwareSearchText: '{SoftwareSearchText}'");
                
                FilteredSoftware.Clear();
                
                var filtered = string.IsNullOrWhiteSpace(SoftwareSearchText) 
                    ? Software 
                    : _libraryService.SearchSoftware(SoftwareSearchText);

                System.Diagnostics.Debug.WriteLine($"?? FilterSoftware: Found {filtered.Count()} software matching '{SoftwareSearchText}'");

                foreach (var softwareItem in filtered)
                {
                    FilteredSoftware.Add(softwareItem);
                }
                
                // Notify search results text changed
                OnPropertyChanged(nameof(SoftwareSearchResultsText));
                
                System.Diagnostics.Debug.WriteLine($"?? FilterSoftware completed: {FilteredSoftware.Count} software in filtered list");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error in FilterSoftware: {ex.Message}");
            }
        }

        private void UpdateStatusText()
        {
            var selectedGame = Games.FirstOrDefault(g => g.IsSelected);
            var selectedSoftware = Software.FirstOrDefault(s => s.IsSelected);
            var selectedDrive = AvailableDrives.FirstOrDefault(d => d.IsSelected);
            var mostRecentDrive = AvailableDrives.FirstOrDefault(d => d.IsRecentlyPlugged);

            var hasSelectedItem = selectedGame != null || selectedSoftware != null;
            var totalItems = Games.Count + Software.Count;

            if (hasSelectedItem && selectedDrive != null)
            {
                var selectedItem = selectedGame ?? (object)selectedSoftware!;
                var itemName = selectedGame?.Name ?? selectedSoftware!.Name;
                var itemSize = selectedGame?.SizeInBytes ?? selectedSoftware!.SizeInBytes;
                var sizeText = FormatBytes(itemSize);
                StatusText = $"? {itemName} ({sizeText}) selected for {selectedDrive.Name}. Ready to add to queue!";
            }
            else if (hasSelectedItem)
            {
                var itemName = selectedGame?.Name ?? selectedSoftware!.Name;
                var itemSize = selectedGame?.SizeInBytes ?? selectedSoftware!.SizeInBytes;
                var sizeText = FormatBytes(itemSize);
                StatusText = $"?? {itemName} ({sizeText}) selected. Please select a USB drive.";
            }
            else if (selectedDrive != null)
            {
                StatusText = $"?? {selectedDrive.Name} selected. Please select a game or software to deploy.";
            }
            else
            {
                var driveText = AvailableDrives.Count > 0 ? $"{AvailableDrives.Count} USB drives" : "No USB drives";
                var eventText = _usbEventCount > 0 ? $" | {_usbEventCount} USB events detected" : "";
                var recentText = mostRecentDrive != null ? $" | Most recent: {mostRecentDrive.DriveLetter}" : "";
                StatusText = $"?? {totalItems} items available ({Games.Count} games, {Software.Count} software), {driveText} detected{eventText}{recentText}. USB-only monitoring active!";
            }

            // Update command states based on selections
            ((RelayCommand)AddToQueueCommand).RaiseCanExecuteChanged();
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
                    // Subscribe to PropertyChanged to update command states
                    drive.PropertyChanged += OnDrivePropertyChanged;
                    
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

        public async Task RefreshDrivesAfterSettingsChange()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("?? Refreshing drives after settings change...");
                
                // Force reload drives with updated settings
                await LoadDrivesAsync();
                
                System.Diagnostics.Debug.WriteLine("? Drive refresh completed after settings change");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error refreshing drives after settings change: {ex.Message}");
                StatusText = $"? Error refreshing drives: {ex.Message}";
            }
        }

        #region Queue Management

        private void AddToQueue()
        {
            try
            {
                var selectedGame = Games.FirstOrDefault(g => g.IsSelected);
                var selectedSoftware = Software.FirstOrDefault(s => s.IsSelected);
                var selectedDrive = AvailableDrives.FirstOrDefault(d => d.IsSelected);

                if (selectedGame == null && selectedSoftware == null)
                {
                    StatusText = "?? Please select a game or software to add to queue.";
                    return;
                }

                if (selectedDrive == null)
                {
                    StatusText = "?? Please select a USB drive.";
                    return;
                }

                var jobsAdded = 0;

                // Create deployment job for the selected game
                if (selectedGame != null)
                {
                    // Check if enough space is available
                    if (selectedDrive.FreeSizeInBytes < selectedGame.SizeInBytes)
                    {
                        StatusText = $"?? Not enough space on {selectedDrive.Name} for {selectedGame.Name}";
                        return;
                    }

                    var job = new DeploymentJob
                    {
                        Game = selectedGame,
                        TargetDrive = selectedDrive,
                        Status = DeploymentJobStatus.Pending
                    };

                    DeploymentJobs.Add(job);
                    jobsAdded++;
                    System.Diagnostics.Debug.WriteLine($"?? Added to queue: {selectedGame.Name} ? {selectedDrive.Name}");
                }

                // Add software support if enabled
                if (selectedSoftware != null)
                {
                    // For now, create a dummy game from software
                    var softwareAsGame = new Game
                    {
                        Name = selectedSoftware.Name,
                        SizeInBytes = selectedSoftware.SizeInBytes,
                        FolderPath = selectedSoftware.FolderPath
                    };

                    if (selectedDrive.FreeSizeInBytes < selectedSoftware.SizeInBytes)
                    {
                        StatusText = $"?? Not enough space on {selectedDrive.Name} for {selectedSoftware.Name}";
                        return;
                    }

                    var job = new DeploymentJob
                    {
                        Game = softwareAsGame,
                        TargetDrive = selectedDrive,
                        Status = DeploymentJobStatus.Pending
                    };

                    DeploymentJobs.Add(job);
                    jobsAdded++;
                    System.Diagnostics.Debug.WriteLine($"?? Added to queue: {selectedSoftware.Name} ? {selectedDrive.Name}");
                }

                if (jobsAdded > 0)
                {
                    var itemName = selectedGame?.Name ?? selectedSoftware!.Name;
                    StatusText = $"? Added {itemName} to copy queue!";

                    // Update command states
                    ((RelayCommand)AddToQueueCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)ClearQueueCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)StartQueueCommand).RaiseCanExecuteChanged();

                    // ?? AUTO-START: Automatically start copying when adding to queue
                    // Clear selections AFTER auto-start to avoid issues
                    _ = Task.Run(async () => 
                    {
                        await AutoStartQueueAsync();
                        
                        // Clear selections after auto-start completes for better UX
                        if (_uiDispatcher != null)
                        {
                            _uiDispatcher.TryEnqueue(() => 
                            {
                                if (selectedGame != null) selectedGame.IsSelected = false;
                                if (selectedSoftware != null) selectedSoftware.IsSelected = false;
                                selectedDrive.IsSelected = false;
                                UpdateStatusText();
                            });
                        }
                    });
                }
                else
                {
                    StatusText = "? No jobs could be added to queue (insufficient space).";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"? Error adding to queue: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"? Error in AddToQueue: {ex.Message}");
            }
        }

        private async Task AutoStartQueueAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("?? AUTO-START: Beginning automatic queue execution...");
                
                // Wait a brief moment to ensure UI updates are complete
                await Task.Delay(500);
                
                // Check if we're not already running and have pending jobs
                if (IsDeploymentRunning)
                {
                    System.Diagnostics.Debug.WriteLine("?? AUTO-START: Deployment already running, skipping auto-start");
                    return;
                }
                
                var pendingJobs = DeploymentJobs.Where(j => j.Status == DeploymentJobStatus.Pending).ToList();
                if (!pendingJobs.Any())
                {
                    System.Diagnostics.Debug.WriteLine("?? AUTO-START: No pending jobs found, skipping auto-start");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"?? AUTO-START: Found {pendingJobs.Count} pending jobs, starting deployment...");
                
                // Update UI on the dispatcher thread
                if (_uiDispatcher != null)
                {
                    _uiDispatcher.TryEnqueue(() => 
                    {
                        StatusText = "?? Auto-starting copy process...";
                    });
                }
                
                // Start the queue
                await StartQueueAsync();
                
                System.Diagnostics.Debug.WriteLine("? AUTO-START: Queue execution completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? AUTO-START ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"? AUTO-START STACK TRACE: {ex.StackTrace}");
                
                // Update status on UI thread with more specific error
                if (_uiDispatcher != null)
                {
                    _uiDispatcher.TryEnqueue(() => 
                    {
                        StatusText = $"? Auto-start failed: {ex.Message}";
                    });
                }
            }
        }

        private async Task StartQueueAsync()
        {
            if (IsDeploymentRunning || !DeploymentJobs.Any()) return;

            try
            {
                IsDeploymentRunning = true;
                _driveMonitorTimer.Stop(); // Stop monitoring during deployment

                var pendingJobs = DeploymentJobs.Where(j => j.Status == DeploymentJobStatus.Pending).ToList();
                var totalJobs = pendingJobs.Count;

                StatusText = $"?? Starting queue: {totalJobs} copy jobs...";
                System.Diagnostics.Debug.WriteLine($"?? Starting queue deployment: {totalJobs} jobs");

                // ?? FIXED: Process jobs directly instead of re-queuing them
                // Start processing each job individually with real file operations
                foreach (var job in pendingJobs)
                {
                    System.Diagnostics.Debug.WriteLine($"?? Processing job: {job.DisplayName}");
                    
                    // Update job status to InProgress
                    job.Status = DeploymentJobStatus.InProgress;
                    job.StartedAt = DateTime.Now;
                    
                    try
                    {
                        // Perform the actual file copy operation
                        await CopyJobAsync(job);
                        
                        // Mark as completed
                        job.Status = DeploymentJobStatus.Completed;
                        job.Progress = 100.0;
                        job.CompletedAt = DateTime.Now;
                        
                        System.Diagnostics.Debug.WriteLine($"? Job completed: {job.DisplayName}");
                    }
                    catch (Exception ex)
                    {
                        job.Status = DeploymentJobStatus.Failed;
                        job.ErrorMessage = ex.Message;
                        System.Diagnostics.Debug.WriteLine($"? Job failed: {job.DisplayName} - {ex.Message}");
                    }
                }

                StatusText = "? Queue completed successfully!";
                System.Diagnostics.Debug.WriteLine("? Queue deployment completed successfully");

                // Update command states
                ((RelayCommand)PauseQueueCommand).RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                StatusText = $"? Queue deployment error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"? Queue deployment error: {ex.Message}");
            }
            finally
            {
                IsDeploymentRunning = false;
                OverallProgress = 0;
                _driveMonitorTimer.Start(); // Resume monitoring
                
                // Update command states
                ((RelayCommand)StartQueueCommand).RaiseCanExecuteChanged();
                ((RelayCommand)PauseQueueCommand).RaiseCanExecuteChanged();
            }
        }

        private async Task CopyJobAsync(DeploymentJob job)
        {
            var sourceDir = job.Game.FolderPath;
            var targetDir = Path.Combine(job.TargetDrive.DriveLetter, job.Game.Name);

            System.Diagnostics.Debug.WriteLine($"?? Copying from {sourceDir} to {targetDir}");

            if (!Directory.Exists(sourceDir))
            {
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
            }

            // Initialize cancellation for this job
            job.InitializeCancellation();

            // Create target directory
            Directory.CreateDirectory(targetDir);

            // Get all files to copy
            var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            var totalFiles = files.Length;
            var processedFiles = 0;

            System.Diagnostics.Debug.WriteLine($"?? Found {totalFiles} files to copy");

            // Copy each file with progress updates and cancellation support
            foreach (var sourceFile in files)
            {
                // Check for cancellation before each file
                job.CancellationToken.ThrowIfCancellationRequested();
                
                // Check for pause state
                while (job.Status == DeploymentJobStatus.Paused)
                {
                    System.Diagnostics.Debug.WriteLine($"?? Job paused, waiting for resume: {job.DisplayName}");
                    await Task.Delay(500, job.CancellationToken); // Wait while paused
                }

                var relativePath = Path.GetRelativePath(sourceDir, sourceFile);
                var targetFile = Path.Combine(targetDir, relativePath);
                var targetFileDir = Path.GetDirectoryName(targetFile);

                if (!string.IsNullOrEmpty(targetFileDir))
                {
                    Directory.CreateDirectory(targetFileDir);
                }

                try
                {
                    // Use async streaming file copy to prevent UI freeze
                    await CopyFileAsyncWithProgress(sourceFile, targetFile, job);
                    
                    processedFiles++;
                    var progress = (double)processedFiles / totalFiles * 100;
                    job.Progress = Math.Min(progress, 100.0);
                    
                    System.Diagnostics.Debug.WriteLine($"?? Copied file {processedFiles}/{totalFiles}: {relativePath} ({progress:F1}%)");
                    
                    // Small delay to allow UI updates and cancellation checks
                    await Task.Delay(10, job.CancellationToken);
                }
                catch (OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine($"?? File copy cancelled: {relativePath}");
                    throw;
                }
            }

            System.Diagnostics.Debug.WriteLine($"? Successfully copied {processedFiles} files to {targetDir}");
        }

        /// <summary>
        /// Async file copy with progress updates and cancellation support to prevent UI freezing
        /// </summary>
        private async Task CopyFileAsyncWithProgress(string sourceFile, string targetFile, DeploymentJob job)
        {
            const int bufferSize = 65536; // 64KB buffer for smooth progress
            System.Diagnostics.Debug.WriteLine($"?? Starting async file copy: {sourceFile} ? {targetFile} ({bufferSize / 1024} KB buffer)");

            using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan);
            using var targetStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan);
            
            var buffer = new byte[bufferSize];
            long totalBytes = sourceStream.Length;
            long copiedBytes = 0;
            
            while (copiedBytes < totalBytes)
            {
                // Check for cancellation
                job.CancellationToken.ThrowIfCancellationRequested();
                
                // Check for pause state
                while (job.Status == DeploymentJobStatus.Paused)
                {
                    await Task.Delay(100, job.CancellationToken);
                }
                
                int bytesToRead = (int)Math.Min(bufferSize, totalBytes - copiedBytes);
                int bytesRead = await sourceStream.ReadAsync(buffer, 0, bytesToRead, job.CancellationToken);
                
                if (bytesRead == 0)
                    break;
                    
                await targetStream.WriteAsync(buffer, 0, bytesRead, job.CancellationToken);
                copiedBytes += bytesRead;
                
                // Update progress more frequently for large files
                if (totalBytes > 1024 * 1024) // Files larger than 1MB
                {
                    var fileProgress = (double)copiedBytes / totalBytes * 100;
                    job.Progress = Math.Max(job.Progress, fileProgress);
                    
                    // Yield control back to UI thread periodically
                    if (copiedBytes % (bufferSize * 10) == 0)
                    {
                        await Task.Delay(1, job.CancellationToken);
                    }
                }
            }
            
            await targetStream.FlushAsync(job.CancellationToken);

            System.Diagnostics.Debug.WriteLine($"? File copy completed: {targetFile} ({copiedBytes}/{totalBytes} bytes)");
        }

        private bool CanAddToQueue()
        {
            if (IsDeploymentRunning) return false;
            
            var hasSelectedItem = Games.Any(g => g.IsSelected) || Software.Any(s => s.IsSelected);
            var hasSelectedDrive = AvailableDrives.Any(d => d.IsSelected);
            
            return hasSelectedItem && hasSelectedDrive;
        }

        private void RemoveFromQueue(DeploymentJob? job)
        {
            if (job == null) return;

            try
            {
                if (job.Status == DeploymentJobStatus.InProgress)
                {
                    StatusText = "?? Cannot remove job that is currently in progress.";
                    return;
                }

                DeploymentJobs.Remove(job);
                StatusText = $"??? Removed {job.DisplayName} from queue";
                System.Diagnostics.Debug.WriteLine($"??? Removed from queue: {job.DisplayName}");

                // Update command states
                ((RelayCommand)ClearQueueCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StartQueueCommand).RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                StatusText = $"? Error removing from queue: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"? Error in RemoveFromQueue: {ex.Message}");
            }
        }

        private void ClearQueue()
        {
            try
            {
                if (IsDeploymentRunning)
                {
                    StatusText = "?? Cannot clear queue while deployment is running.";
                    return;
                }

                var jobCount = DeploymentJobs.Count;
                DeploymentJobs.Clear();
                StatusText = $"??? Cleared {jobCount} jobs from queue";
                System.Diagnostics.Debug.WriteLine($"??? Cleared queue: {jobCount} jobs removed");

                // Update command states
                ((RelayCommand)ClearQueueCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StartQueueCommand).RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                StatusText = $"? Error clearing queue: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"? Error in ClearQueue: {ex.Message}");
            }
        }

        private bool CanStartQueue()
        {
            return !IsDeploymentRunning && DeploymentJobs.Any(j => j.Status == DeploymentJobStatus.Pending);
        }

        private void PauseQueue()
        {
            if (!IsDeploymentRunning) return;

            try
            {
                _deploymentService.CancelAllJobs();
                StatusText = "?? Queue paused by user.";
                IsDeploymentRunning = false;
                OverallProgress = 0;
                _driveMonitorTimer.Start(); // Resume monitoring
                System.Diagnostics.Debug.WriteLine("?? Queue paused by user");

                // Update command states
                ((RelayCommand)StartQueueCommand).RaiseCanExecuteChanged();
                ((RelayCommand)PauseQueueCommand).RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                StatusText = $"? Error pausing queue: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"? Error in PauseQueue: {ex.Message}");
            }
        }

        #endregion

        #region Individual Job Controls

        private void PauseJob(DeploymentJob? job)
        {
            if (job == null || !job.CanPause) return;

            try
            {
                job.Pause();
                StatusText = $"?? Paused job: {job.DisplayName}";
                System.Diagnostics.Debug.WriteLine($"?? User paused job: {job.DisplayName}");
            }
            catch (Exception ex)
            {
                StatusText = $"? Error pausing job: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"? Error in PauseJob: {ex.Message}");
            }
        }

        private void ResumeJob(DeploymentJob? job)
        {
            if (job == null || !job.CanResume) return;

            try
            {
                job.Resume();
                StatusText = $"?? Resumed job: {job.DisplayName}";
                System.Diagnostics.Debug.WriteLine($"?? User resumed job: {job.DisplayName}");
            }
            catch (Exception ex)
            {
                StatusText = $"? Error resuming job: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"? Error in ResumeJob: {ex.Message}");
            }
        }

        private void CancelJob(DeploymentJob? job)
        {
            if (job == null || !job.CanCancel) return;

            try
            {
                job.Cancel();
                StatusText = $"?? Cancelled job: {job.DisplayName}";
                System.Diagnostics.Debug.WriteLine($"?? User cancelled job: {job.DisplayName}");
            }
            catch (Exception ex)
            {
                StatusText = $"? Error cancelling job: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"? Error in CancelJob: {ex.Message}");
            }
        }

        #endregion

        private void OnGamePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Game.IsSelected))
            {
                // Handle radio button behavior for games - only one can be selected
                if (sender is Game selectedGame && selectedGame.IsSelected)
                {
                    // Clear other game selections
                    foreach (var game in Games.Where(g => g != selectedGame))
                    {
                        game.IsSelected = false;
                    }
                    
                    // Clear all software selections (mutual exclusion between games and software)
                    foreach (var software in Software)
                    {
                        software.IsSelected = false;
                    }
                }
                
                UpdateStatusText();
                System.Diagnostics.Debug.WriteLine($"?? Game selection changed, updating command states");
            }
        }

        private void OnSoftwarePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Models.Software.IsSelected))
            {
                // Handle radio button behavior for software - only one can be selected
                if (sender is Models.Software selectedSoftware && selectedSoftware.IsSelected)
                {
                    // Clear other software selections
                    foreach (var software in Software.Where(s => s != selectedSoftware))
                    {
                        software.IsSelected = false;
                    }
                    
                    // Clear all game selections (mutual exclusion between games and software)
                    foreach (var game in Games)
                    {
                        game.IsSelected = false;
                    }
                }
                
                UpdateStatusText();
                System.Diagnostics.Debug.WriteLine($"?? Software selection changed, updating command states");
            }
        }

        private void OnDrivePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Drive.IsSelected))
            {
                // Handle radio button behavior for drives - only one can be selected
                if (sender is Drive selectedDrive && selectedDrive.IsSelected)
                {
                    // Clear other drive selections
                    foreach (var drive in AvailableDrives.Where(d => d != selectedDrive))
                    {
                        drive.IsSelected = false;
                    }
                }
                
                UpdateStatusText();
                System.Diagnostics.Debug.WriteLine($"?? Drive selection changed, updating command states");
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

    // Generic RelayCommand implementation
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            if (parameter is T typedParameter)
                return _canExecute?.Invoke(typedParameter) ?? true;
            if (parameter == null && !typeof(T).IsValueType)
                return _canExecute?.Invoke(default(T)) ?? true;
            return false;
        }

        public void Execute(object? parameter)
        {
            if (parameter is T typedParameter)
                _execute(typedParameter);
            else if (parameter == null && !typeof(T).IsValueType)
                _execute(default(T));
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}