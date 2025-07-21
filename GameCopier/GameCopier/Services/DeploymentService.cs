using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameCopier.Models;

namespace GameCopier.Services
{
    public class DeploymentService
    {
        private readonly ConcurrentQueue<DeploymentJob> _jobQueue = new();
        private readonly ConcurrentDictionary<string, DeploymentJob> _activeJobs = new();
        private readonly SemaphoreSlim _concurrencyLimit;
        private readonly LoggingService _loggingService;
        private bool _isRunning = false;
        private CancellationTokenSource _cancellationTokenSource = new();

        public event EventHandler<DeploymentJob>? JobUpdated;
        public event EventHandler<double>? OverallProgressUpdated;

        // Configuration properties
        public CopyMethod PreferredCopyMethod { get; set; } = CopyMethod.ExplorerDialog;
        public bool UseLargeDiskBuffer { get; set; } = true;

        public DeploymentService(int maxConcurrentJobs = 2) // Reduced for Explorer copy
        {
            _concurrencyLimit = new SemaphoreSlim(maxConcurrentJobs, maxConcurrentJobs);
            _loggingService = new LoggingService();
        }

        public void QueueDeployment(Game game, Drive targetDrive)
        {
            var job = new DeploymentJob();
            job.Game = game;
            job.TargetDrive = targetDrive;
            job.Status = DeploymentJobStatus.Pending;

            _jobQueue.Enqueue(job);
            JobUpdated?.Invoke(this, job);
        }

        public void QueueJob(Game game, Drive targetDrive)
        {
            QueueDeployment(game, targetDrive);
        }

        public void QueueMultipleDeployments(IEnumerable<Game> games, IEnumerable<Drive> targetDrives)
        {
            var gamesList = games.ToList();
            var drivesList = targetDrives.ToList();

            foreach (var game in gamesList)
            {
                foreach (var drive in drivesList)
                {
                    QueueDeployment(game, drive);
                }
            }

            // Log deployment started
            var gamesStr = string.Join(", ", gamesList.Select(g => g.Name));
            var drivesStr = string.Join(", ", drivesList.Select(d => d.Name));
            _loggingService.LogDeploymentStarted(gamesStr, drivesStr);
        }

        public async Task StartDeploymentAsync()
        {
            if (_isRunning) return;

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();

            var processingTasks = new List<Task>();

            // Process jobs sequentially for Explorer dialog visibility
            while (_jobQueue.TryDequeue(out var job) || _activeJobs.Any())
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    break;

                if (_jobQueue.TryDequeue(out job))
                {
                    var task = ProcessJobAsync(job, _cancellationTokenSource.Token);
                    processingTasks.Add(task);
                }

                // Clean up completed tasks
                var completedTasks = processingTasks.Where(t => t.IsCompleted).ToList();
                foreach (var completedTask in completedTasks)
                {
                    processingTasks.Remove(completedTask);
                }

                // Small delay to prevent tight loop
                await Task.Delay(100, _cancellationTokenSource.Token);
            }

            // Wait for all remaining tasks to complete
            await Task.WhenAll(processingTasks);
            _isRunning = false;
        }

        public void CancelAllJobs()
        {
            _cancellationTokenSource.Cancel();

            // Mark all pending jobs as cancelled
            while (_jobQueue.TryDequeue(out var job))
            {
                job.Status = DeploymentJobStatus.Cancelled;
                JobUpdated?.Invoke(this, job);
            }

            // Mark active jobs as cancelled
            foreach (var activeJob in _activeJobs.Values)
            {
                activeJob.Status = DeploymentJobStatus.Cancelled;
                JobUpdated?.Invoke(this, activeJob);
            }
        }

        public IEnumerable<DeploymentJob> GetAllJobs()
        {
            var allJobs = new List<DeploymentJob>();
            allJobs.AddRange(_jobQueue);
            allJobs.AddRange(_activeJobs.Values);
            return allJobs;
        }

        private async Task ProcessJobAsync(DeploymentJob job, CancellationToken cancellationToken)
        {
            await _concurrencyLimit.WaitAsync(cancellationToken);

            try
            {
                _activeJobs.TryAdd(job.Id, job);

                job.Status = DeploymentJobStatus.InProgress;
                job.StartedAt = DateTime.Now;
                JobUpdated?.Invoke(this, job);

                await CopyGameAsync(job, cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    job.Status = DeploymentJobStatus.Completed;
                    job.Progress = 100.0;
                    job.CompletedAt = DateTime.Now;

                    // Log successful completion
                    _loggingService.LogDeploymentCompleted(job.Id, job.Game.Name, job.TargetDrive.Name, true);
                }
            }
            catch (OperationCanceledException)
            {
                job.Status = DeploymentJobStatus.Cancelled;
            }
            catch (Exception ex)
            {
                job.Status = DeploymentJobStatus.Failed;
                job.ErrorMessage = ex.Message;

                // Log failure
                _loggingService.LogDeploymentCompleted(job.Id, job.Game.Name, job.TargetDrive.Name, false, ex.Message);
            }
            finally
            {
                _activeJobs.TryRemove(job.Id, out _);
                JobUpdated?.Invoke(this, job);
                UpdateOverallProgress();
                _concurrencyLimit.Release();
            }
        }

        private async Task CopyGameAsync(DeploymentJob job, CancellationToken cancellationToken)
        {
            var sourceDir = job.Game.FolderPath;
            var targetDir = Path.Combine(job.TargetDrive.DriveLetter, job.Game.Name);

            if (!Directory.Exists(sourceDir))
            {
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
            }

            Debug.WriteLine($"Using Windows Explorer copy for {sourceDir} -> {targetDir}");
            
            // Use Windows Explorer copy based on preference
            bool success = false;
            
            switch (PreferredCopyMethod)
            {
                case CopyMethod.ExplorerDialog:
                    success = await FastCopyService.CopyDirectoryWithExplorerDialogAsync(sourceDir, targetDir, IntPtr.Zero, cancellationToken);
                    break;
                    
                case CopyMethod.ExplorerSilent:
                    success = await FastCopyService.CopyDirectoryWithExplorerSilentAsync(sourceDir, targetDir, cancellationToken);
                    break;
                    
                case CopyMethod.Robocopy:
                    if (IsRobocopyAvailable())
                    {
                        await CopyDirectoryWithRobocopyAsync(sourceDir, targetDir, job, cancellationToken);
                        return;
                    }
                    else
                    {
                        // Fallback to Explorer copy
                        success = await FastCopyService.CopyDirectoryWithExplorerSilentAsync(sourceDir, targetDir, cancellationToken);
                    }
                    break;
                    
                case CopyMethod.Xcopy:
                    if (IsXcopyAvailable())
                    {
                        await CopyDirectoryWithXcopyAsync(sourceDir, targetDir, job, cancellationToken);
                        return;
                    }
                    else
                    {
                        // Fallback to Explorer copy
                        success = await FastCopyService.CopyDirectoryWithExplorerSilentAsync(sourceDir, targetDir, cancellationToken);
                    }
                    break;
                    
                default:
                    // Default to Explorer dialog
                    success = await FastCopyService.CopyDirectoryWithExplorerDialogAsync(sourceDir, targetDir, IntPtr.Zero, cancellationToken);
                    break;
            }

            if (!success)
            {
                throw new InvalidOperationException("Windows Explorer copy operation failed");
            }

            // Set progress to 100% since we can't track Explorer copy progress
            job.Progress = 100.0;
            JobUpdated?.Invoke(this, job);
        }

        private static bool IsRobocopyAvailable()
        {
            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "robocopy",
                    Arguments = "/?",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                });
                process?.WaitForExit(1000);
                return process?.ExitCode != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsXcopyAvailable()
        {
            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "xcopy",
                    Arguments = "/?",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                });
                process?.WaitForExit(1000);
                return process?.ExitCode != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task CopyDirectoryWithRobocopyAsync(string sourceDir, string targetDir, DeploymentJob job, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(targetDir);

            var arguments = $"\"{sourceDir}\" \"{targetDir}\" /E /MT:8 /NFL /NDL /NJH /NJS /NC /NS /NP";
            
            if (UseLargeDiskBuffer)
            {
                arguments += " /J"; // Use unbuffered I/O for large files
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = "robocopy",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = new Process { StartInfo = processInfo };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Monitor for cancellation
            while (!process.HasExited && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                try
                {
                    process.Kill();
                }
                catch { }
                throw new OperationCanceledException();
            }

            await process.WaitForExitAsync(cancellationToken);

            // Robocopy exit codes: 0-7 are success, 8+ are errors
            if (process.ExitCode >= 8)
            {
                throw new InvalidOperationException($"Robocopy failed with exit code {process.ExitCode}");
            }
            
            job.Progress = 100.0;
            JobUpdated?.Invoke(this, job);
        }

        private async Task CopyDirectoryWithXcopyAsync(string sourceDir, string targetDir, DeploymentJob job, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(targetDir);
            
            var arguments = $"\"{sourceDir}\" \"{targetDir}\" /E /I /Y /H /R";

            var processInfo = new ProcessStartInfo
            {
                FileName = "xcopy",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = new Process { StartInfo = processInfo };

            process.Start();
            process.BeginOutputReadLine();

            while (!process.HasExited && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                try
                {
                    process.Kill();
                }
                catch { }
                throw new OperationCanceledException();
            }

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Xcopy failed with exit code {process.ExitCode}");
            }
            
            job.Progress = 100.0;
            JobUpdated?.Invoke(this, job);
        }

        private void UpdateOverallProgress()
        {
            var allJobs = GetAllJobs().ToList();
            if (!allJobs.Any()) return;

            var totalProgress = allJobs.Average(j => j.Progress);
            OverallProgressUpdated?.Invoke(this, totalProgress);
        }
    }
}