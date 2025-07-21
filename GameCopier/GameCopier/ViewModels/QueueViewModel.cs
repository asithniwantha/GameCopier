using System;
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