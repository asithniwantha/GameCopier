using GameCopier.Models.Domain;
using GameCopier.Services.Business;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GameCopier.ViewModels.Managers
{
    /// <summary>
    /// Manages copy queue operations and Windows Explorer copy process
    /// </summary>
    public class DeploymentManager : INotifyPropertyChanged
    {
        private readonly DispatcherQueue? _uiDispatcher;
        private bool _isRunning = false;
        private static int _copyOperationCounter = 0; // Track copy operations

        public event EventHandler<DeploymentJob>? JobUpdated;
        public event EventHandler<string>? StatusChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                _isRunning = value;
                OnPropertyChanged();
            }
        }

        public DeploymentManager(DispatcherQueue? uiDispatcher)
        {
            _uiDispatcher = uiDispatcher;
        }

        public async Task<bool> ProcessJobAsync(DeploymentJob job)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🚀 DeploymentManager: Processing job {job.DisplayName}");

                // Mark job as in progress
                job.Status = DeploymentJobStatus.InProgress;
                job.StartedAt = DateTime.Now;
                JobUpdated?.Invoke(this, job);

                // Prepare paths
                var sourceDir = job.Game.FolderPath;
                var targetDir = Path.Combine(job.TargetDrive.DriveLetter, job.Game.Name);

                System.Diagnostics.Debug.WriteLine($"📋 Copy: {sourceDir} -> {targetDir}");

                // Create a status callback for real-time feedback
                Action<string> statusCallback = (status) =>
                {
                    _uiDispatcher?.TryEnqueue(() =>
                    {
                        StatusChanged?.Invoke(this, $"{job.Game.Name} → {job.TargetDrive.DriveLetter}: {status}");
                    });
                };

                // Initial status with detailed information
                StatusChanged?.Invoke(this, $"🚀 Starting copy: {job.Game.Name} → {job.TargetDrive.DriveLetter}");

                // Use enhanced Windows Explorer copy with better dialog control
                bool success = await FastCopyService.CopyDirectoryWithDialogNotificationAsync(
                    sourceDir,
                    targetDir,
                    IntPtr.Zero,
                    statusCallback,
                    System.Threading.CancellationToken.None);

                // Update job status
                if (success)
                {
                    job.Status = DeploymentJobStatus.Completed;
                    job.Progress = 100.0;
                    job.CompletedAt = DateTime.Now;
                    StatusChanged?.Invoke(this, $"✅ Completed: {job.Game.Name} → {job.TargetDrive.DriveLetter}");
                    System.Diagnostics.Debug.WriteLine($"✅ Job completed: {job.DisplayName}");
                }
                else
                {
                    job.Status = DeploymentJobStatus.Failed;
                    job.ErrorMessage = "Windows Explorer copy operation failed";
                    StatusChanged?.Invoke(this, $"❌ Failed: {job.Game.Name} → {job.TargetDrive.DriveLetter}");
                    System.Diagnostics.Debug.WriteLine($"❌ Job failed: {job.DisplayName}");
                }

                JobUpdated?.Invoke(this, job);
                return success;
            }
            catch (Exception ex)
            {
                job.Status = DeploymentJobStatus.Failed;
                job.ErrorMessage = ex.Message;
                StatusChanged?.Invoke(this, $"❌ Error copying {job.Game.Name} to {job.TargetDrive.DriveLetter}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Job exception: {job.DisplayName} - {ex.Message}");

                JobUpdated?.Invoke(this, job);
                return false;
            }
        }

        public async Task<bool> ProcessQueueAsync(List<DeploymentJob> jobs)
        {
            System.Diagnostics.Debug.WriteLine($"🚀 DeploymentManager: ProcessQueueAsync called with {jobs.Count} jobs");
            
            if (IsRunning)
            {
                System.Diagnostics.Debug.WriteLine("❌ DeploymentManager: Already running, rejecting new queue");
                return false;
            }
            
            if (!jobs.Any())
            {
                System.Diagnostics.Debug.WriteLine("❌ DeploymentManager: No jobs provided");
                return false;
            }

            try
            {
                IsRunning = true;
                System.Diagnostics.Debug.WriteLine("🚀 DeploymentManager: Set IsRunning to true");
                
                var pendingJobs = jobs.Where(j => j.Status == DeploymentJobStatus.Pending).ToList();
                var totalJobs = pendingJobs.Count;

                System.Diagnostics.Debug.WriteLine($"🚀 DeploymentManager: Found {totalJobs} pending jobs to process");

                StatusChanged?.Invoke(this, $"🚀 Starting {totalJobs} copy operation(s) with enhanced dialog control...");
                System.Diagnostics.Debug.WriteLine($"🚀 DeploymentManager: Processing {totalJobs} jobs with dialog-aware parallel strategy");

                // Group jobs by target drive for smart parallel processing
                var jobsByDrive = pendingJobs.GroupBy(j => j.TargetDrive.DriveLetter).ToList();
                System.Diagnostics.Debug.WriteLine($"📊 Jobs grouped into {jobsByDrive.Count} drive groups:");

                foreach (var driveGroup in jobsByDrive)
                {
                    var jobNames = driveGroup.Select(j => j.Game.Name).ToList();
                    System.Diagnostics.Debug.WriteLine($"   📊 {driveGroup.Key}: {string.Join(", ", jobNames)}");
                }

                var successfulJobs = 0;
                var failedJobs = 0;

                // ENHANCED: Use a semaphore to control dialog display timing
                using var dialogSemaphore = new SemaphoreSlim(1, 1); // Only one dialog at a time

                // Process each drive group in parallel, but dialogs appear sequentially
                var driveGroupTasks = jobsByDrive.Select(async driveGroup =>
                {
                    var driveLetter = driveGroup.Key;
                    var driveJobs = driveGroup.ToList();

                    System.Diagnostics.Debug.WriteLine($"🚀 Starting enhanced processing for drive {driveLetter} with {driveJobs.Count} jobs");

                    var driveSuccessCount = 0;
                    var driveFailCount = 0;

                    // Process jobs for this specific drive sequentially with dialog control
                    foreach (var job in driveJobs)
                    {
                        System.Diagnostics.Debug.WriteLine($"📋 Processing job: {job.Game.Name} → {driveLetter}");

                        // Acquire dialog semaphore to ensure only one dialog at a time
                        await dialogSemaphore.WaitAsync();

                        try
                        {
                            bool success = await ProcessJobWithDialogControlAsync(job);
                            if (success)
                                driveSuccessCount++;
                            else
                                driveFailCount++;

                            // Strategic delay to allow Shell API to reset between operations
                            await Task.Delay(1000); // 1 second delay between operations
                        }
                        finally
                        {
                            dialogSemaphore.Release();
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"✅ Drive {driveLetter} completed: {driveSuccessCount} succeeded, {driveFailCount} failed");
                    return new { Success = driveSuccessCount, Failed = driveFailCount };
                }).ToList();

                // Wait for all drive groups to complete
                System.Diagnostics.Debug.WriteLine("🚀 DeploymentManager: Waiting for all drive groups to complete...");
                var results = await Task.WhenAll(driveGroupTasks);

                // Aggregate results
                successfulJobs = results.Sum(r => r.Success);
                failedJobs = results.Sum(r => r.Failed);

                // Final status
                if (failedJobs == 0)
                {
                    StatusChanged?.Invoke(this, $"✅ All {successfulJobs} copy operations completed successfully!");
                }
                else
                {
                    StatusChanged?.Invoke(this, $"⚠️ Copy completed: {successfulJobs} succeeded, {failedJobs} failed");
                }

                System.Diagnostics.Debug.WriteLine($"🚀 DeploymentManager: Enhanced queue completed - {successfulJobs} succeeded, {failedJobs} failed");
                return failedJobs == 0;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"❌ Copy operation error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ DeploymentManager: Queue error - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ DeploymentManager: Stack trace - {ex.StackTrace}");
                return false;
            }
            finally
            {
                IsRunning = false;
                System.Diagnostics.Debug.WriteLine("🚀 DeploymentManager: Set IsRunning to false");
            }
        }

        /// <summary>
        /// Enhanced job processing with dialog control to ensure dialogs appear reliably
        /// </summary>
        private async Task<bool> ProcessJobWithDialogControlAsync(DeploymentJob job)
        {
            try
            {
                // Increment copy operation counter for tracking
                var operationNumber = Interlocked.Increment(ref _copyOperationCounter);
                System.Diagnostics.Debug.WriteLine($"🚀 DeploymentManager: Processing copy operation #{operationNumber} - {job.DisplayName}");

                // Mark job as in progress
                job.Status = DeploymentJobStatus.InProgress;
                job.StartedAt = DateTime.Now;
                JobUpdated?.Invoke(this, job);

                // Prepare paths
                var sourceDir = job.Game.FolderPath;
                var targetDir = Path.Combine(job.TargetDrive.DriveLetter, job.Game.Name);

                System.Diagnostics.Debug.WriteLine($"📋 Enhanced copy #{operationNumber}: {sourceDir} -> {targetDir}");

                // Create a status callback for real-time feedback
                Action<string> statusCallback = (status) =>
                {
                    _uiDispatcher?.TryEnqueue(() =>
                    {
                        StatusChanged?.Invoke(this, $"#{operationNumber} {job.Game.Name} → {job.TargetDrive.DriveLetter}: {status}");
                    });
                };

                // Enhanced status with operation number and dialog assurance
                StatusChanged?.Invoke(this, $"🚀 Copy #{operationNumber} starting: {job.Game.Name} → {job.TargetDrive.DriveLetter} (Dialog guaranteed)");

                // Strategic delay based on operation number to prevent API conflicts
                var delayMs = Math.Min(500 + (operationNumber * 200), 2000); // Increasing delay, max 2s
                await Task.Delay(delayMs);

                System.Diagnostics.Debug.WriteLine($"📋 Copy #{operationNumber}: Starting with {delayMs}ms delay for dialog reliability");

                // Use enhanced copy method with forced dialog visibility
                bool success = await FastCopyService.CopyDirectoryWithForcedDialogAsync(
                    sourceDir,
                    targetDir,
                    IntPtr.Zero,
                    statusCallback,
                    System.Threading.CancellationToken.None);

                // Update job status
                if (success)
                {
                    job.Status = DeploymentJobStatus.Completed;
                    job.Progress = 100.0;
                    job.CompletedAt = DateTime.Now;
                    StatusChanged?.Invoke(this, $"✅ Copy #{operationNumber} completed: {job.Game.Name} → {job.TargetDrive.DriveLetter}");
                    System.Diagnostics.Debug.WriteLine($"✅ Copy operation #{operationNumber} completed: {job.DisplayName}");
                }
                else
                {
                    job.Status = DeploymentJobStatus.Failed;
                    job.ErrorMessage = "Windows Explorer copy operation failed";
                    StatusChanged?.Invoke(this, $"❌ Copy #{operationNumber} failed: {job.Game.Name} → {job.TargetDrive.DriveLetter}");
                    System.Diagnostics.Debug.WriteLine($"❌ Copy operation #{operationNumber} failed: {job.DisplayName}");
                }

                JobUpdated?.Invoke(this, job);
                return success;
            }
            catch (Exception ex)
            {
                job.Status = DeploymentJobStatus.Failed;
                job.ErrorMessage = ex.Message;
                StatusChanged?.Invoke(this, $"❌ Error in copy operation: {job.Game.Name} to {job.TargetDrive.DriveLetter}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Copy operation exception: {job.DisplayName} - {ex.Message}");

                JobUpdated?.Invoke(this, job);
                return false;
            }
        }

        public bool ValidateSpaceRequirement(DeploymentJob job)
        {
            var hasEnoughSpace = job.TargetDrive.FreeSizeInBytes >= job.Game.SizeInBytes;
            
            System.Diagnostics.Debug.WriteLine($"🔍 DeploymentManager: Space validation for {job.Game.Name}:");
            System.Diagnostics.Debug.WriteLine($"   📊 Required: {job.Game.SizeInBytes:N0} bytes ({FormatBytes(job.Game.SizeInBytes)})");
            System.Diagnostics.Debug.WriteLine($"   📊 Available: {job.TargetDrive.FreeSizeInBytes:N0} bytes ({FormatBytes(job.TargetDrive.FreeSizeInBytes)})");
            System.Diagnostics.Debug.WriteLine($"   📊 Result: {(hasEnoughSpace ? "✅ ENOUGH SPACE" : "❌ INSUFFICIENT SPACE")}");
            
            return hasEnoughSpace;
        }

        public bool ValidateSourceExists(DeploymentJob job)
        {
            var exists = Directory.Exists(job.Game.FolderPath);
            
            System.Diagnostics.Debug.WriteLine($"🔍 DeploymentManager: Source validation for {job.Game.Name}:");
            System.Diagnostics.Debug.WriteLine($"   📂 Path: {job.Game.FolderPath}");
            System.Diagnostics.Debug.WriteLine($"   📂 Result: {(exists ? "✅ EXISTS" : "❌ NOT FOUND")}");
            
            return exists;
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

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}