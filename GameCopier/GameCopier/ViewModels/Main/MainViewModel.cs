using GameCopier.Models.Domain;
using GameCopier.Services.Data;
using GameCopier.ViewModels.Managers;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace GameCopier.ViewModels.Main
{
    /// <summary>
    /// Simplified MainViewModel that orchestrates managers and handles UI coordination
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        // Managers
        private readonly UsbDriveManager _usbDriveManager;
        private readonly LibraryManager _libraryManager;
        private readonly DeploymentManager _deploymentManager;
        private readonly NavigationManager _navigationManager;
        private readonly SettingsService _settingsService;
        private readonly Queue.QueueViewModel _queueViewModel;

        // UI Threading
        private readonly DispatcherQueue? _uiDispatcher;
        private readonly System.Timers.Timer _driveMonitorTimer;

        // Properties
        private string _searchText = string.Empty;
        private string _softwareSearchText = string.Empty;
        private string _statusText = "Ready";

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
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
                    OnPropertyChanged();
                    FilterSoftware();
                }
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

        // Collections
        public ObservableCollection<Game> Games { get; } = new();
        public ObservableCollection<Game> FilteredGames { get; } = new();
        public ObservableCollection<Software> Software { get; } = new();
        public ObservableCollection<Software> FilteredSoftware { get; } = new();
        public ObservableCollection<Drive> AvailableDrives { get; } = new();

        // Queue properties from QueueViewModel
        public ObservableCollection<DeploymentJob> DeploymentJobs => _queueViewModel.DeploymentJobs;
        public double OverallProgress => _queueViewModel.OverallProgress;
        public bool IsDeploymentRunning => _queueViewModel.IsDeploymentRunning;
        public string QueueAnalysis => _queueViewModel.GetQueueAnalysis();

        // Search result properties
        public string GameSearchResultsText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SearchText)) return "";
                var count = FilteredGames.Count;
                var total = Games.Count;
                return count == 1 ? $"Found 1 game out of {total}" : $"Found {count} games out of {total}";
            }
        }

        public string SoftwareSearchResultsText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SoftwareSearchText)) return "";
                var count = FilteredSoftware.Count;
                var total = Software.Count;
                return count == 1 ? $"Found 1 software out of {total}" : $"Found {count} software out of {total}";
            }
        }

        // Commands
        public ICommand LoadGamesCommand { get; }
        public ICommand RefreshLibraryCommand { get; }
        public ICommand LoadSoftwareCommand { get; }
        public ICommand RefreshSoftwareLibraryCommand { get; }
        public ICommand LoadDrivesCommand { get; }
        public ICommand RefreshDrivesCommand { get; } // Add manual refresh command
        public ICommand ForceShowDrivesCommand { get; } // Force show drives for testing
        public ICommand TestCopyDialogCommand { get; } // Test copy dialog visibility
        public ICommand TestSequentialDialogsCommand { get; } // Test multiple dialogs
        public ICommand ShowSettingsCommand { get; }
        public ICommand AddGameFolderCommand { get; }
        public ICommand AddSoftwareFolderCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand ClearSoftwareSearchCommand { get; }
        public ICommand OpenGameFolderCommand { get; }
        public ICommand OpenSoftwareFolderCommand { get; }

        // Queue commands
        public ICommand AddToQueueCommand { get; }
        public ICommand RemoveFromQueueCommand { get; }
        public ICommand ClearQueueCommand { get; }
        public ICommand StartQueueCommand { get; }
        public ICommand DebugStartQueueCommand { get; } // Debug command to manually start queue

        // Events
        public event EventHandler? RequestSettingsDialog;
        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel()
        {
            System.Diagnostics.Debug.WriteLine("🚀 MainViewModel: Initializing with modular architecture...");

            // Capture UI dispatcher
            _uiDispatcher = DispatcherQueue.GetForCurrentThread();
            if (_uiDispatcher == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ MainViewModel: Could not get UI dispatcher!");
            }

            // Initialize services
            _settingsService = new SettingsService();

            // Initialize managers
            _usbDriveManager = new UsbDriveManager(_uiDispatcher);
            _libraryManager = new LibraryManager();
            _deploymentManager = new DeploymentManager(_uiDispatcher);
            _navigationManager = new NavigationManager(_uiDispatcher);
            _queueViewModel = new Queue.QueueViewModel(_deploymentManager);

            // Subscribe to manager events
            _usbDriveManager.DrivesUpdated += OnDrivesUpdated;
            _usbDriveManager.StatusChanged += OnStatusChanged;
            _libraryManager.GamesUpdated += OnGamesUpdated;
            _libraryManager.SoftwareUpdated += OnSoftwareUpdated;
            _libraryManager.StatusChanged += OnStatusChanged;
            _deploymentManager.StatusChanged += OnStatusChanged;
            _navigationManager.StatusChanged += OnStatusChanged;
            _queueViewModel.StatusChanged += OnStatusChanged;
            _queueViewModel.PropertyChanged += OnQueuePropertyChanged;

            // Initialize commands
            LoadGamesCommand = new Base.RelayCommand(async () => await LoadGamesAsync());
            RefreshLibraryCommand = new Base.RelayCommand(async () => await RefreshLibraryAsync());
            LoadSoftwareCommand = new Base.RelayCommand(async () => await LoadSoftwareAsync());
            RefreshSoftwareLibraryCommand = new Base.RelayCommand(async () => await RefreshSoftwareLibraryAsync());
            LoadDrivesCommand = new Base.RelayCommand(async () => await LoadDrivesAsync());
            RefreshDrivesCommand = new Base.RelayCommand(async () => await RefreshDrivesManuallyAsync());
            ForceShowDrivesCommand = new Base.RelayCommand(async () => await ForceShowDrivesAsync());
            TestCopyDialogCommand = new Base.RelayCommand(async () => await TestCopyDialogAsync());
            TestSequentialDialogsCommand = new Base.RelayCommand(async () => await TestSequentialDialogsAsync());
            ShowSettingsCommand = new Base.RelayCommand(ShowSettings);
            AddGameFolderCommand = new Base.RelayCommand(async () => await AddGameFolderAsync());
            AddSoftwareFolderCommand = new Base.RelayCommand(async () => await AddSoftwareFolderAsync());
            ClearSearchCommand = new Base.RelayCommand(() => SearchText = string.Empty);
            ClearSoftwareSearchCommand = new Base.RelayCommand(() => SoftwareSearchText = string.Empty);
            OpenGameFolderCommand = new Base.RelayCommand<Game>(game => _navigationManager.OpenGameFolder(game));
            OpenSoftwareFolderCommand = new Base.RelayCommand<Software>(software => _navigationManager.OpenSoftwareFolder(software));

            // Queue commands
            AddToQueueCommand = new Base.RelayCommand(AddToQueue, CanAddToQueue);
            RemoveFromQueueCommand = new Base.RelayCommand<DeploymentJob>(job => _queueViewModel.RemoveFromQueue(job));
            ClearQueueCommand = new Base.RelayCommand(() => _queueViewModel.ClearQueue(), () => DeploymentJobs.Count > 0);
            StartQueueCommand = new Base.RelayCommand(async () => await StartQueueAsync(), () => _queueViewModel.CanStartQueue());
            DebugStartQueueCommand = new Base.RelayCommand(async () => await DebugStartQueueAsync()); // Debug command

            // Backup timer for drive monitoring
            _driveMonitorTimer = new System.Timers.Timer(30000); // 30 seconds
            _driveMonitorTimer.Elapsed += OnDriveMonitorTimer;
            _driveMonitorTimer.AutoReset = true;

            System.Diagnostics.Debug.WriteLine("✅ MainViewModel: Initialization complete");

            // Start initialization
            _ = Task.Run(async () => await InitializeAsync());
        }

        // ...rest of the methods remain the same...
        private async Task InitializeAsync()
        {
            try
            {
                StatusText = "🚀 Initializing GameDeploy Kiosk...";

                await LoadGamesAsync();
                await LoadSoftwareAsync();
                await LoadDrivesAsync();

                StatusText = "✅ Initialization complete - Ready for operation!";
                _driveMonitorTimer.Start();

                await Task.Delay(2000);
                UpdateStatusText();
            }
            catch (Exception ex)
            {
                StatusText = $"❌ Initialization error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ MainViewModel: Initialization error - {ex.Message}");
                _driveMonitorTimer.Start(); // Start timer anyway
            }
        }

        private async Task LoadGamesAsync()
        {
            var games = await _libraryManager.LoadGamesAsync();
            // Games updated via event handler
        }

        private async Task RefreshLibraryAsync()
        {
            var games = await _libraryManager.RefreshGamesAsync();
            // Games updated via event handler
        }

        private async Task LoadSoftwareAsync()
        {
            var software = await _libraryManager.LoadSoftwareAsync();
            // Software updated via event handler
        }

        private async Task RefreshSoftwareLibraryAsync()
        {
            var software = await _libraryManager.RefreshSoftwareAsync();
            // Software updated via event handler
        }

        private async Task LoadDrivesAsync()
        {
            var drives = await _usbDriveManager.LoadDrivesAsync();
            // Drives updated via event handler
        }

        private async Task RefreshDrivesManuallyAsync()
        {
            try
            {
                StatusText = "🔄 Manually refreshing USB drives...";
                System.Diagnostics.Debug.WriteLine("🔄 MainViewModel: Manual drive refresh requested");

                // Force a fresh load of drives
                var drives = await _usbDriveManager.LoadDrivesAsync();

                if (drives.Count > 0)
                {
                    StatusText = $"✅ Found {drives.Count} drives";
                }
                else
                {
                    StatusText = "⚠️ No drives found - check settings or connect a USB drive";
                }

                // Update the UI regardless
                _uiDispatcher?.TryEnqueue(() =>
                {
                    UpdateDriveListDirectly(drives);
                });

                System.Diagnostics.Debug.WriteLine($"🔄 MainViewModel: Manual refresh complete - {drives.Count} drives found");
            }
            catch (Exception ex)
            {
                StatusText = $"❌ Error refreshing drives: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ MainViewModel: Manual refresh error - {ex.Message}");
            }
        }

        private async Task ForceShowDrivesAsync()
        {
            try
            {
                StatusText = "🔧 Force showing all available drives...";
                System.Diagnostics.Debug.WriteLine("🔧 MainViewModel: Force show drives requested");

                // Create a basic drive list that shows all non-C drives
                var allDrives = System.IO.DriveInfo.GetDrives()
                    .Where(d => d.IsReady && !d.Name.ToUpper().StartsWith("C:"))
                    .ToList();

                var forcedDrives = new List<Drive>();

                foreach (var driveInfo in allDrives)
                {
                    var drive = new Drive
                    {
                        Name = $"🔧 {driveInfo.VolumeLabel ?? "Drive"} ({driveInfo.Name}) - FORCED",
                        DriveLetter = driveInfo.Name.TrimEnd('\\'),
                        Label = driveInfo.VolumeLabel ?? "Drive",
                        TotalSizeInBytes = driveInfo.TotalSize,
                        FreeSizeInBytes = driveInfo.AvailableFreeSpace,
                        IsRemovable = driveInfo.DriveType == DriveType.Removable,
                        DetectedAt = DateTime.Now,
                        DeviceDescription = $"Forced {driveInfo.DriveType} Drive",
                        FileSystem = driveInfo.DriveFormat ?? "Unknown"
                    };

                    forcedDrives.Add(drive);
                    System.Diagnostics.Debug.WriteLine($"🔧 Forced drive: {drive.DriveLetter} - {drive.Name}");
                }

                // If no drives found, create demo drives
                if (forcedDrives.Count == 0)
                {
                    forcedDrives.Add(new Drive
                    {
                        Name = "🔧 TEST USB Drive (Demo)",
                        DriveLetter = "X:",
                        Label = "TEST_USB",
                        TotalSizeInBytes = 32L * 1024 * 1024 * 1024, // 32GB
                        FreeSizeInBytes = 30L * 1024 * 1024 * 1024,  // 30GB free
                        IsRemovable = true,
                        DetectedAt = DateTime.Now,
                        DeviceDescription = "Forced Demo Drive",
                        FileSystem = "NTFS"
                    });
                }

                StatusText = $"🔧 Force showing {forcedDrives.Count} drives";

                // Update the UI
                _uiDispatcher?.TryEnqueue(() =>
                {
                    UpdateDriveListDirectly(forcedDrives);
                });

                System.Diagnostics.Debug.WriteLine($"🔧 MainViewModel: Force show complete - {forcedDrives.Count} drives forced");
            }
            catch (Exception ex)
            {
                StatusText = $"❌ Error force showing drives: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ MainViewModel: Force show error - {ex.Message}");
            }
        }

        private async Task TestCopyDialogAsync()
        {
            try
            {
                StatusText = "🧪 Testing dual progress copy operation...";
                System.Diagnostics.Debug.WriteLine("🧪 MainViewModel: Testing dual progress copy");

                // Create a temporary test setup
                var tempSource = Path.Combine(Path.GetTempPath(), "GameCopierTest");
                var tempTarget = Path.Combine(Path.GetTempPath(), "GameCopierTestTarget");

                // Create test source directory with multiple files for better progress testing
                Directory.CreateDirectory(tempSource);
                for (int i = 1; i <= 10; i++)
                {
                    var testFilePath = Path.Combine(tempSource, $"test{i}.txt");
                    await File.WriteAllTextAsync(testFilePath, $"This is test file #{i} for dual progress testing. ".PadRight(1000, 'X'));
                }

                StatusText = "🧪 Test files created, attempting dual progress copy operation...";

                // Create status callback for real-time feedback
                Action<string> statusCallback = (status) =>
                {
                    _uiDispatcher?.TryEnqueue(() =>
                    {
                        StatusText = $"🧪 Dual Progress Test: {status}";
                    });
                };

                // Create progress callback for real-time progress updates
                double testProgress = 0;
                Action<double> progressCallback = (progress) =>
                {
                    _uiDispatcher?.TryEnqueue(() =>
                    {
                        testProgress = progress;
                        StatusText = $"🧪 Dual Progress Test: {progress:F1}% complete";
                    });
                };

                // Test the NEW dual progress method
                bool success = await Services.Business.FastCopyService.CopyDirectoryWithDualProgressAsync(
                    tempSource,
                    tempTarget,
                    IntPtr.Zero,
                    statusCallback,
                    progressCallback,
                    CancellationToken.None);

                // Cleanup
                try
                {
                    if (Directory.Exists(tempSource)) Directory.Delete(tempSource, true);
                    if (Directory.Exists(tempTarget)) Directory.Delete(tempTarget, true);
                }
                catch { } // Ignore cleanup errors

                if (success)
                {
                    StatusText = $"✅ Dual progress test completed successfully! Final progress: {testProgress:F1}%";
                }
                else
                {
                    StatusText = "⚠️ Dual progress test failed";
                }

                // Reset status after delay
                await Task.Delay(5000);
                UpdateStatusText();

                System.Diagnostics.Debug.WriteLine($"🧪 MainViewModel: Dual progress copy test completed - Success: {success}");
            }
            catch (Exception ex)
            {
                StatusText = $"❌ Dual progress test error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ MainViewModel: Dual progress test error - {ex.Message}");

                await Task.Delay(3000);
                UpdateStatusText();
            }
        }

        private async Task TestSequentialDialogsAsync()
        {
            try
            {
                StatusText = "🧪 Testing sequential dual progress operations...";
                System.Diagnostics.Debug.WriteLine("🧪 MainViewModel: Testing sequential dual progress operations");

                // Create multiple test sources
                var tempBase = Path.Combine(Path.GetTempPath(), "GameCopierDualProgressTest");
                var testSources = new List<string>();
                var testTargets = new List<string>();

                for (int i = 1; i <= 3; i++)
                {
                    var sourceDir = Path.Combine(tempBase, $"Source{i}");
                    var targetDir = Path.Combine(tempBase, $"Target{i}");

                    Directory.CreateDirectory(sourceDir);
                    
                    // Create multiple files for better progress testing
                    for (int j = 1; j <= 5; j++)
                    {
                        var testFilePath = Path.Combine(sourceDir, $"test{i}_{j}.txt");
                        await File.WriteAllTextAsync(testFilePath, $"This is test file #{j} in source {i} for dual progress testing. ".PadRight(500, 'X'));
                    }

                    testSources.Add(sourceDir);
                    testTargets.Add(targetDir);
                }

                StatusText = "🧪 Created 3 test sources with multiple files, starting dual progress sequential tests...";

                // Test sequential copies with dual progress
                for (int i = 0; i < testSources.Count; i++)
                {
                    var operationNumber = i + 1;
                    StatusText = $"🧪 Dual Progress Test {operationNumber}/3: Starting copy operation...";

                    double testProgress = 0;

                    // Create status callback for real-time feedback
                    Action<string> statusCallback = (status) =>
                    {
                        _uiDispatcher?.TryEnqueue(() =>
                        {
                            StatusText = $"🧪 Dual Progress Test {operationNumber}/3: {status}";
                        });
                    };

                    // Create progress callback for real-time progress updates
                    Action<double> progressCallback = (progress) =>
                    {
                        _uiDispatcher?.TryEnqueue(() =>
                        {
                            testProgress = progress;
                            StatusText = $"🧪 Dual Progress Test {operationNumber}/3: {progress:F1}% complete";
                        });
                    };

                    // Use the NEW dual progress method
                    bool success = await Services.Business.FastCopyService.CopyDirectoryWithDualProgressAsync(
                        testSources[i],
                        testTargets[i],
                        IntPtr.Zero,
                        statusCallback,
                        progressCallback,
                        CancellationToken.None);

                    if (success)
                    {
                        StatusText = $"✅ Dual Progress Test {operationNumber}/3: Copy completed successfully! Progress: {testProgress:F1}%";
                    }
                    else
                    {
                        StatusText = $"❌ Dual Progress Test {operationNumber}/3: Copy failed";
                    }

                    // Delay between operations to show results
                    await Task.Delay(2000);
                }

                // Cleanup
                try
                {
                    if (Directory.Exists(tempBase)) Directory.Delete(tempBase, true);
                }
                catch { } // Ignore cleanup errors

                StatusText = "✅ Dual progress sequential test completed! All operations should have shown both visible progress and UI progress.";

                // Reset status after delay
                await Task.Delay(5000);
                UpdateStatusText();

                System.Diagnostics.Debug.WriteLine("🧪 MainViewModel: Dual progress sequential copy test completed");
            }
            catch (Exception ex)
            {
                StatusText = $"❌ Dual progress sequential test error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ MainViewModel: Dual progress sequential test error - {ex.Message}");

                await Task.Delay(3000);
                UpdateStatusText();
            }
        }

        private async Task AddGameFolderAsync()
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            folderPicker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(GameCopier.App.MainWindow);
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                await _libraryManager.AddGameFolderAsync(folder.Path);
                await LoadGamesAsync();
            }
        }

        private async Task AddSoftwareFolderAsync()
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            folderPicker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(GameCopier.App.MainWindow);
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                await _libraryManager.AddSoftwareFolderAsync(folder.Path);
                await LoadSoftwareAsync();
            }
        }

        private void AddToQueue()
        {
            var selectedGame = Games.FirstOrDefault(g => g.IsSelected);
            var selectedSoftware = Software.FirstOrDefault(s => s.IsSelected);
            var selectedDrive = AvailableDrives.FirstOrDefault(d => d.IsSelected);

            bool success = _queueViewModel.AddToQueue(selectedGame, selectedSoftware, selectedDrive);

            if (success)
            {
                var itemName = selectedGame?.Name ?? selectedSoftware?.Name ?? "Unknown";
                System.Diagnostics.Debug.WriteLine($"🚀 MainViewModel: Successfully added {itemName} to queue, attempting auto-start...");
                
                // Auto-start queue with better error handling
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(500); // Brief delay for UI updates
                        System.Diagnostics.Debug.WriteLine($"🚀 MainViewModel: Starting auto-start for {itemName}...");
                        
                        bool startSuccess = await StartQueueAsync();
                        System.Diagnostics.Debug.WriteLine($"🚀 MainViewModel: Auto-start result for {itemName}: {startSuccess}");
                        
                        if (!startSuccess)
                        {
                            _uiDispatcher?.TryEnqueue(() =>
                            {
                                StatusText = $"⚠️ {itemName} added to queue but auto-start failed. Click the item in the queue to manually start.";
                            });
                        }

                        // Clear selections after auto-start attempt
                        _uiDispatcher?.TryEnqueue(() =>
                        {
                            if (selectedGame != null) selectedGame.IsSelected = false;
                            if (selectedSoftware != null) selectedSoftware.IsSelected = false;
                            if (selectedDrive != null) selectedDrive.IsSelected = false;
                            UpdateStatusText();
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ MainViewModel: Auto-start error for {itemName}: {ex.Message}");
                        _uiDispatcher?.TryEnqueue(() =>
                        {
                            StatusText = $"❌ Auto-start failed for {itemName}: {ex.Message}. Check queue manually.";
                        });
                    }
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ MainViewModel: Failed to add item to queue");
            }

            UpdateCommandStates();
        }

        private bool CanAddToQueue()
        {
            var selectedGame = Games.FirstOrDefault(g => g.IsSelected);
            var selectedSoftware = Software.FirstOrDefault(s => s.IsSelected);
            var selectedDrive = AvailableDrives.FirstOrDefault(d => d.IsSelected);

            return _queueViewModel.CanAddToQueue(selectedGame, selectedSoftware, selectedDrive);
        }

        private async Task<bool> StartQueueAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🚀 MainViewModel: StartQueueAsync called");
                
                if (!_queueViewModel.CanStartQueue())
                {
                    System.Diagnostics.Debug.WriteLine("❌ MainViewModel: CanStartQueue returned false");
                    var reason = IsDeploymentRunning ? "deployment already running" : "no pending jobs";
                    System.Diagnostics.Debug.WriteLine($"❌ MainViewModel: Reason: {reason}");
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine("✅ MainViewModel: Starting queue execution...");
                bool success = await _queueViewModel.StartQueueAsync();
                System.Diagnostics.Debug.WriteLine($"🚀 MainViewModel: Queue execution result: {success}");
                
                UpdateCommandStates();
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ MainViewModel: StartQueueAsync exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ MainViewModel: Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private async Task DebugStartQueueAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🐛 MainViewModel: DEBUG - Force starting queue...");
                StatusText = "🐛 DEBUG: Force starting queue...";
                
                // Force debug information
                System.Diagnostics.Debug.WriteLine($"🐛 DEBUG: Current queue count: {DeploymentJobs.Count}");
                System.Diagnostics.Debug.WriteLine($"🐛 DEBUG: IsDeploymentRunning: {IsDeploymentRunning}");
                System.Diagnostics.Debug.WriteLine($"🐛 DEBUG: CanStartQueue: {_queueViewModel.CanStartQueue()}");
                
                var pendingJobs = DeploymentJobs.Where(j => j.Status == DeploymentJobStatus.Pending).ToList();
                System.Diagnostics.Debug.WriteLine($"🐛 DEBUG: Pending jobs: {pendingJobs.Count}");
                
                foreach (var job in pendingJobs)
                {
                    System.Diagnostics.Debug.WriteLine($"🐛 DEBUG: Job - {job.DisplayName} ({job.Status})");
                }
                
                if (pendingJobs.Any())
                {
                    bool success = await _queueViewModel.StartQueueAsync();
                    StatusText = $"🐛 DEBUG: Queue start result: {success}";
                    System.Diagnostics.Debug.WriteLine($"🐛 DEBUG: Force start result: {success}");
                }
                else
                {
                    StatusText = "🐛 DEBUG: No pending jobs to start";
                    System.Diagnostics.Debug.WriteLine("🐛 DEBUG: No pending jobs found");
                }
                
                await Task.Delay(3000);
                UpdateStatusText();
            }
            catch (Exception ex)
            {
                StatusText = $"🐛 DEBUG: Error - {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"🐛 DEBUG: Exception - {ex.Message}");
                
                await Task.Delay(3000);
                UpdateStatusText();
            }
        }

        private void ShowSettings()
        {
            RequestSettingsDialog?.Invoke(this, EventArgs.Empty);
        }

        public async Task RefreshDrivesAfterSettingsChange()
        {
            await LoadDrivesAsync();
        }

        // Event Handlers
        private void OnDrivesUpdated(object? sender, List<Drive> drives)
        {
            _uiDispatcher?.TryEnqueue(() =>
            {
                UpdateDriveListDirectly(drives);
            });
        }

        private void OnGamesUpdated(object? sender, List<Game> games)
        {
            _uiDispatcher?.TryEnqueue(() =>
            {
                Games.Clear();
                foreach (var game in games)
                {
                    game.PropertyChanged += OnGamePropertyChanged;
                    Games.Add(game);
                }
                FilterGames();
                UpdateStatusText();
                OnPropertyChanged(nameof(GameSearchResultsText));
            });
        }

        private void OnSoftwareUpdated(object? sender, List<Software> software)
        {
            _uiDispatcher?.TryEnqueue(() =>
            {
                Software.Clear();
                foreach (var softwareItem in software)
                {
                    softwareItem.PropertyChanged += OnSoftwarePropertyChanged;
                    Software.Add(softwareItem);
                }
                FilterSoftware();
                UpdateStatusText();
                OnPropertyChanged(nameof(SoftwareSearchResultsText));
            });
        }

        private void OnStatusChanged(object? sender, string status)
        {
            _uiDispatcher?.TryEnqueue(() =>
            {
                StatusText = status;
            });
        }

        private void OnQueuePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Queue.QueueViewModel.OverallProgress))
                OnPropertyChanged(nameof(OverallProgress));
            else if (e.PropertyName == nameof(Queue.QueueViewModel.IsDeploymentRunning))
                OnPropertyChanged(nameof(IsDeploymentRunning));

            // Always update queue analysis when queue changes
            OnPropertyChanged(nameof(QueueAnalysis));
        }

        private void OnGamePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Game.IsSelected))
            {
                if (sender is Game selectedGame && selectedGame.IsSelected)
                {
                    // Clear other selections (radio button behavior)
                    foreach (var game in Games.Where(g => g != selectedGame))
                        game.IsSelected = false;
                    foreach (var software in Software)
                        software.IsSelected = false;
                }
                UpdateStatusText();
                UpdateCommandStates();
            }
        }

        private void OnSoftwarePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelected")
            {
                if (sender is Software selectedSoftware && selectedSoftware.IsSelected)
                {
                    // Clear other selections (radio button behavior)
                    foreach (var software in Software.Where(s => s != selectedSoftware))
                        software.IsSelected = false;
                    foreach (var game in Games)
                        game.IsSelected = false;
                }
                UpdateStatusText();
                UpdateCommandStates();
            }
        }

        private void OnDrivePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Drive.IsSelected))
            {
                if (sender is Drive selectedDrive && selectedDrive.IsSelected)
                {
                    // Clear other drive selections (radio button behavior)
                    foreach (var drive in AvailableDrives.Where(d => d != selectedDrive))
                        drive.IsSelected = false;
                }
                UpdateStatusText();
                UpdateCommandStates();
            }
        }

        private async void OnDriveMonitorTimer(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (!IsDeploymentRunning)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("⏰ MainViewModel: Backup drive monitoring...");
                    await LoadDrivesAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ MainViewModel: Backup timer error - {ex.Message}");
                }
            }
        }

        // Helper Methods
        private void FilterGames()
        {
            try
            {
                FilteredGames.Clear();
                var filtered = string.IsNullOrWhiteSpace(SearchText)
                    ? Games
                    : _libraryManager.SearchGames(SearchText);

                foreach (var game in filtered)
                    FilteredGames.Add(game);

                OnPropertyChanged(nameof(GameSearchResultsText));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ MainViewModel: Error filtering games - {ex.Message}");
            }
        }

        private void FilterSoftware()
        {
            try
            {
                FilteredSoftware.Clear();
                var filtered = string.IsNullOrWhiteSpace(SoftwareSearchText)
                    ? Software
                    : _libraryManager.SearchSoftware(SoftwareSearchText);

                foreach (var softwareItem in filtered)
                    FilteredSoftware.Add(softwareItem);

                OnPropertyChanged(nameof(SoftwareSearchResultsText));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ MainViewModel: Error filtering software - {ex.Message}");
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
                var itemName = selectedGame?.Name ?? selectedSoftware!.Name;
                var itemSize = selectedGame?.SizeInBytes ?? selectedSoftware!.SizeInBytes;
                var sizeText = FormatBytes(itemSize);
                StatusText = $"🎯 {itemName} ({sizeText}) selected for {selectedDrive.Name}. Windows copy dialog will appear during copy.";
            }
            else if (hasSelectedItem)
            {
                var itemName = selectedGame?.Name ?? selectedSoftware!.Name;
                var itemSize = selectedGame?.SizeInBytes ?? selectedSoftware!.SizeInBytes;
                var sizeText = FormatBytes(itemSize);
                StatusText = $"⚠️ {itemName} ({sizeText}) selected. Please select a USB drive.";
            }
            else if (selectedDrive != null)
            {
                StatusText = $"⚠️ {selectedDrive.Name} selected. Please select a game or software to deploy.";
            }
            else
            {
                var driveText = AvailableDrives.Count > 0 ? $"{AvailableDrives.Count} USB drives" : "No USB drives";
                var recentText = mostRecentDrive != null ? $" | Most recent: {mostRecentDrive.DriveLetter}" : "";
                var copyInfo = IsDeploymentRunning ? " | Copy operations running in background" : "";
                StatusText = $"📊 {totalItems} items available ({Games.Count} games, {Software.Count} software), {driveText} detected{recentText}{copyInfo}";
            }

            UpdateCommandStates();
        }

        private void UpdateDriveListDirectly(List<Drive> drives)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📋 UpdateDriveListDirectly: Processing {drives.Count} drives ON UI THREAD");

                if (drives.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ No drives provided to UpdateDriveListDirectly!");
                    StatusText = "⚠️ No USB drives detected - try refreshing or check settings";
                    return;
                }

                // Preserve selections
                var previousSelections = AvailableDrives.Where(d => d.IsSelected).Select(d => d.DriveLetter).ToList();
                System.Diagnostics.Debug.WriteLine($"📋 Preserving selections for: {string.Join(", ", previousSelections)}");

                AvailableDrives.Clear();
                System.Diagnostics.Debug.WriteLine("📋 Cleared existing drives from UI");

                foreach (var drive in drives)
                {
                    drive.PropertyChanged += OnDrivePropertyChanged;

                    if (previousSelections.Contains(drive.DriveLetter))
                        drive.IsSelected = true;

                    AvailableDrives.Add(drive);
                    System.Diagnostics.Debug.WriteLine($"📋 Added drive to UI: {drive.DriveLetter} - {drive.Name}");
                }

                System.Diagnostics.Debug.WriteLine($"📋 UpdateDriveListDirectly: Complete - {AvailableDrives.Count} drives now in UI");
                UpdateStatusText();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ UpdateDriveListDirectly: Error - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ UpdateDriveListDirectly: Stack trace - {ex.StackTrace}");
                StatusText = $"❌ Error updating drive list: {ex.Message}";
            }
        }

        private void UpdateCommandStates()
        {
            ((Base.RelayCommand)AddToQueueCommand).RaiseCanExecuteChanged();
            ((Base.RelayCommand)ClearQueueCommand).RaiseCanExecuteChanged();
            ((Base.RelayCommand)StartQueueCommand).RaiseCanExecuteChanged();
            ((Base.RelayCommand)DebugStartQueueCommand).RaiseCanExecuteChanged();
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

        public void Dispose()
        {
            System.Diagnostics.Debug.WriteLine("🗑️ MainViewModel: Disposing...");
            _driveMonitorTimer?.Stop();
            _driveMonitorTimer?.Dispose();
            _usbDriveManager?.Dispose();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}