using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GameCopier.Models;
using GameCopier.Services;
using Microsoft.UI.Dispatching;

namespace GameCopier.ViewModels.Managers
{
    /// <summary>
    /// Manages copy queue operations and Windows Explorer copy process
    /// </summary>
    public class DeploymentManager : INotifyPropertyChanged
    {
        private readonly DispatcherQueue? _uiDispatcher;
        private bool _isRunning = false;

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
                System.Diagnostics.Debug.WriteLine($"?? DeploymentManager: Processing job {job.DisplayName}");
                
                // Mark job as in progress
                job.Status = DeploymentJobStatus.InProgress;
                job.StartedAt = DateTime.Now;
                JobUpdated?.Invoke(this, job);
                
                // Prepare paths
                var sourceDir = job.Game.FolderPath;
                var targetDir = Path.Combine(job.TargetDrive.DriveLetter, job.Game.Name);

                System.Diagnostics.Debug.WriteLine($"?? Copy: {sourceDir} -> {targetDir}");
                
                // Update status
                StatusChanged?.Invoke(this, $"?? Windows Explorer dialog opening for: {job.Game.Name}");
                
                // Use Windows Explorer copy with dialog
                bool success = await FastCopyService.CopyDirectoryWithExplorerDialogAsync(
                    sourceDir, 
                    targetDir, 
                    IntPtr.Zero, 
                    System.Threading.CancellationToken.None);

                // Update job status
                if (success)
                {
                    job.Status = DeploymentJobStatus.Completed;
                    job.Progress = 100.0;
                    job.CompletedAt = DateTime.Now;
                    StatusChanged?.Invoke(this, $"? Completed: {job.Game.Name}");
                    System.Diagnostics.Debug.WriteLine($"? Job completed: {job.DisplayName}");
                }
                else
                {
                    job.Status = DeploymentJobStatus.Failed;
                    job.ErrorMessage = "Windows Explorer copy operation failed";
                    StatusChanged?.Invoke(this, $"? Failed: {job.Game.Name}");
                    System.Diagnostics.Debug.WriteLine($"? Job failed: {job.DisplayName}");
                }
                
                JobUpdated?.Invoke(this, job);
                return success;
            }
            catch (Exception ex)
            {
                job.Status = DeploymentJobStatus.Failed;
                job.ErrorMessage = ex.Message;
                StatusChanged?.Invoke(this, $"? Error copying {job.Game.Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"? Job exception: {job.DisplayName} - {ex.Message}");
                
                JobUpdated?.Invoke(this, job);
                return false;
            }
        }

        public async Task<bool> ProcessQueueAsync(List<DeploymentJob> jobs)
        {
            if (IsRunning || !jobs.Any()) return false;

            try
            {
                IsRunning = true;
                var pendingJobs = jobs.Where(j => j.Status == DeploymentJobStatus.Pending).ToList();
                var totalJobs = pendingJobs.Count;

                StatusChanged?.Invoke(this, $"?? Starting {totalJobs} copy operation(s) with Windows Explorer...");
                System.Diagnostics.Debug.WriteLine($"?? DeploymentManager: Processing {totalJobs} jobs");

                var successfulJobs = 0;
                var failedJobs = 0;

                // Process each job sequentially for better dialog visibility
                foreach (var job in pendingJobs)
                {
                    bool success = await ProcessJobAsync(job);
                    if (success)
                        successfulJobs++;
                    else
                        failedJobs++;
                }

                // Final status
                if (failedJobs == 0)
                {
                    StatusChanged?.Invoke(this, $"? All {successfulJobs} copy operations completed successfully!");
                }
                else
                {
                    StatusChanged?.Invoke(this, $"?? Copy completed: {successfulJobs} succeeded, {failedJobs} failed");
                }
                
                System.Diagnostics.Debug.WriteLine($"? DeploymentManager: Queue completed - {successfulJobs} succeeded, {failedJobs} failed");
                return failedJobs == 0;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"? Copy operation error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"? DeploymentManager: Queue error - {ex.Message}");
                return false;
            }
            finally
            {
                IsRunning = false;
            }
        }

        public bool ValidateSpaceRequirement(DeploymentJob job)
        {
            return job.TargetDrive.FreeSizeInBytes >= job.Game.SizeInBytes;
        }

        public bool ValidateSourceExists(DeploymentJob job)
        {
            return Directory.Exists(job.Game.FolderPath);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}