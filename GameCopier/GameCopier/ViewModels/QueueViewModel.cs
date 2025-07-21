using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using GameCopier.Models;
using GameCopier.ViewModels.Managers;

namespace GameCopier.ViewModels
{
    /// <summary>
    /// Manages the copy queue and job operations
    /// </summary>
    public class QueueViewModel : INotifyPropertyChanged
    {
        private readonly DeploymentManager _deploymentManager;
        private double _overallProgress = 0.0;

        public ObservableCollection<DeploymentJob> DeploymentJobs { get; } = new();

        public double OverallProgress
        {
            get => _overallProgress;
            set
            {
                _overallProgress = value;
                OnPropertyChanged();
            }
        }

        public bool IsDeploymentRunning => _deploymentManager.IsRunning;

        public event EventHandler<string>? StatusChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public QueueViewModel(DeploymentManager deploymentManager)
        {
            _deploymentManager = deploymentManager;
            _deploymentManager.JobUpdated += OnJobUpdated;
            _deploymentManager.StatusChanged += (s, status) => StatusChanged?.Invoke(this, status);
            _deploymentManager.PropertyChanged += (s, e) => 
            {
                if (e.PropertyName == nameof(DeploymentManager.IsRunning))
                    OnPropertyChanged(nameof(IsDeploymentRunning));
            };
        }

        public bool CanAddToQueue(Game? selectedGame, Software? selectedSoftware, Drive? selectedDrive)
        {
            if (IsDeploymentRunning) return false;
            
            var hasSelectedItem = selectedGame != null || selectedSoftware != null;
            var hasSelectedDrive = selectedDrive != null;
            
            return hasSelectedItem && hasSelectedDrive;
        }

        public bool AddToQueue(Game? selectedGame, Software? selectedSoftware, Drive? selectedDrive)
        {
            try
            {
                if (selectedGame == null && selectedSoftware == null)
                {
                    StatusChanged?.Invoke(this, "?? Please select a game or software to add to queue.");
                    return false;
                }

                if (selectedDrive == null)
                {
                    StatusChanged?.Invoke(this, "?? Please select a USB drive.");
                    return false;
                }

                var jobsAdded = 0;

                // Add game job
                if (selectedGame != null)
                {
                    if (!_deploymentManager.ValidateSpaceRequirement(new DeploymentJob { Game = selectedGame, TargetDrive = selectedDrive }))
                    {
                        StatusChanged?.Invoke(this, $"?? Not enough space on {selectedDrive.Name} for {selectedGame.Name}");
                        return false;
                    }

                    var job = new DeploymentJob
                    {
                        Game = selectedGame,
                        TargetDrive = selectedDrive,
                        Status = DeploymentJobStatus.Pending
                    };

                    DeploymentJobs.Add(job);
                    jobsAdded++;
                    System.Diagnostics.Debug.WriteLine($"?? QueueViewModel: Added game job - {selectedGame.Name} ? {selectedDrive.Name}");
                }

                // Add software job (convert to game format for compatibility)
                if (selectedSoftware != null)
                {
                    var softwareAsGame = new Game
                    {
                        Name = selectedSoftware.Name,
                        SizeInBytes = selectedSoftware.SizeInBytes,
                        FolderPath = selectedSoftware.FolderPath
                    };

                    if (!_deploymentManager.ValidateSpaceRequirement(new DeploymentJob { Game = softwareAsGame, TargetDrive = selectedDrive }))
                    {
                        StatusChanged?.Invoke(this, $"?? Not enough space on {selectedDrive.Name} for {selectedSoftware.Name}");
                        return false;
                    }

                    var job = new DeploymentJob
                    {
                        Game = softwareAsGame,
                        TargetDrive = selectedDrive,
                        Status = DeploymentJobStatus.Pending
                    };

                    DeploymentJobs.Add(job);
                    jobsAdded++;
                    System.Diagnostics.Debug.WriteLine($"?? QueueViewModel: Added software job - {selectedSoftware.Name} ? {selectedDrive.Name}");
                }

                if (jobsAdded > 0)
                {
                    var itemName = selectedGame?.Name ?? selectedSoftware!.Name;
                    StatusChanged?.Invoke(this, $"? Added {itemName} to copy queue! Windows Explorer dialog will appear during copy.");
                    
                    // Trigger queue analysis update
                    OnPropertyChanged("QueueAnalysis");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"? Error adding to queue: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"? QueueViewModel: Error in AddToQueue - {ex.Message}");
                return false;
            }
        }

        public async System.Threading.Tasks.Task<bool> StartQueueAsync()
        {
            if (IsDeploymentRunning || !DeploymentJobs.Any()) return false;

            try
            {
                var pendingJobs = DeploymentJobs.Where(j => j.Status == DeploymentJobStatus.Pending).ToList();
                if (!pendingJobs.Any()) return false;

                // Analyze the queue for parallel processing potential
                var jobsByDrive = pendingJobs.GroupBy(j => j.TargetDrive.DriveLetter).ToList();
                var driveCount = jobsByDrive.Count;
                
                if (driveCount > 1)
                {
                    StatusChanged?.Invoke(this, $"?? Starting {pendingJobs.Count} jobs across {driveCount} drives - parallel processing enabled!");
                    System.Diagnostics.Debug.WriteLine($"?? QueueViewModel: Parallel processing - {driveCount} drives will run simultaneously");
                    
                    foreach (var driveGroup in jobsByDrive)
                    {
                        var jobCount = driveGroup.Count();
                        System.Diagnostics.Debug.WriteLine($"   ?? Drive {driveGroup.Key}: {jobCount} jobs");
                    }
                }
                else
                {
                    StatusChanged?.Invoke(this, $"?? Starting {pendingJobs.Count} jobs to single drive - sequential processing");
                    System.Diagnostics.Debug.WriteLine($"?? QueueViewModel: Single drive processing - jobs will run sequentially");
                }

                System.Diagnostics.Debug.WriteLine($"?? QueueViewModel: Starting queue with {pendingJobs.Count} jobs");
                
                bool success = await _deploymentManager.ProcessQueueAsync(pendingJobs);
                UpdateOverallProgress();
                
                return success;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"? Error starting queue: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"? QueueViewModel: Error in StartQueueAsync - {ex.Message}");
                return false;
            }
        }

        public bool CanStartQueue()
        {
            return !IsDeploymentRunning && DeploymentJobs.Any(j => j.Status == DeploymentJobStatus.Pending);
        }

        public string GetQueueAnalysis()
        {
            var pendingJobs = DeploymentJobs.Where(j => j.Status == DeploymentJobStatus.Pending).ToList();
            if (!pendingJobs.Any()) return "No pending jobs in queue";
            
            var jobsByDrive = pendingJobs.GroupBy(j => j.TargetDrive.DriveLetter).ToList();
            var driveCount = jobsByDrive.Count;
            
            if (driveCount == 1)
            {
                var drive = jobsByDrive.First().Key;
                return $"{pendingJobs.Count} jobs ? {drive} (sequential)";
            }
            else
            {
                var driveDetails = jobsByDrive.Select(g => $"{g.Key} ({g.Count()})").ToList();
                return $"{pendingJobs.Count} jobs ? {string.Join(", ", driveDetails)} (parallel by drive)";
            }
        }

        /// <summary>
        /// Gets detailed information about the parallel processing strategy for the current queue
        /// </summary>
        public string GetParallelProcessingInfo()
        {
            var pendingJobs = DeploymentJobs.Where(j => j.Status == DeploymentJobStatus.Pending).ToList();
            if (!pendingJobs.Any()) return "No jobs to process";
            
            var jobsByDrive = pendingJobs.GroupBy(j => j.TargetDrive.DriveLetter).ToList();
            var driveCount = jobsByDrive.Count;
            
            if (driveCount == 1)
            {
                return $"Single drive mode: All {pendingJobs.Count} jobs will copy to {jobsByDrive.First().Key} sequentially for optimal performance.";
            }
            else
            {
                var details = new List<string>();
                details.Add($"Smart parallel mode: {driveCount} drives will be processed simultaneously:");
                
                foreach (var driveGroup in jobsByDrive.OrderBy(g => g.Key))
                {
                    var jobNames = driveGroup.Select(j => j.Game.Name).Take(3).ToList();
                    var remaining = driveGroup.Count() - jobNames.Count;
                    var jobList = string.Join(", ", jobNames);
                    if (remaining > 0) jobList += $" (+{remaining} more)";
                    
                    details.Add($"  ?? {driveGroup.Key}: {driveGroup.Count()} jobs ({jobList})");
                }
                
                details.Add($"Total time savings: ~{Math.Round((driveCount - 1) * 100.0 / driveCount, 0)}% faster than sequential processing");
                
                return string.Join("\n", details);
            }
        }

        public void RemoveFromQueue(DeploymentJob? job)
        {
            if (job == null) return;

            try
            {
                if (job.Status == DeploymentJobStatus.InProgress)
                {
                    StatusChanged?.Invoke(this, "?? Cannot remove job that is currently in progress.");
                    return;
                }

                DeploymentJobs.Remove(job);
                StatusChanged?.Invoke(this, $"??? Removed {job.DisplayName} from queue");
                System.Diagnostics.Debug.WriteLine($"??? QueueViewModel: Removed job - {job.DisplayName}");
                
                // Trigger queue analysis update
                OnPropertyChanged("QueueAnalysis");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"? Error removing from queue: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"? QueueViewModel: Error in RemoveFromQueue - {ex.Message}");
            }
        }

        public void ClearQueue()
        {
            try
            {
                if (IsDeploymentRunning)
                {
                    StatusChanged?.Invoke(this, "?? Cannot clear queue while deployment is running.");
                    return;
                }

                var jobCount = DeploymentJobs.Count;
                DeploymentJobs.Clear();
                StatusChanged?.Invoke(this, $"?? Cleared {jobCount} jobs from queue");
                System.Diagnostics.Debug.WriteLine($"?? QueueViewModel: Cleared {jobCount} jobs");
                
                // Trigger queue analysis update
                OnPropertyChanged("QueueAnalysis");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"? Error clearing queue: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"? QueueViewModel: Error in ClearQueue - {ex.Message}");
            }
        }

        private void OnJobUpdated(object? sender, DeploymentJob job)
        {
            // Update existing job or add new one
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

            UpdateOverallProgress();
            
            // Trigger queue analysis update by notifying property changed
            OnPropertyChanged("QueueAnalysis");
        }

        private void UpdateOverallProgress()
        {
            if (!DeploymentJobs.Any())
            {
                OverallProgress = 0;
                return;
            }

            var totalProgress = DeploymentJobs.Average(j => j.Progress);
            OverallProgress = totalProgress;

            var completedJobs = DeploymentJobs.Count(j => j.Status == DeploymentJobStatus.Completed);
            var totalJobs = DeploymentJobs.Count;
            
            if (completedJobs == totalJobs && totalJobs > 0)
            {
                StatusChanged?.Invoke(this, $"? All copy operations completed! ({completedJobs}/{totalJobs})");
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}