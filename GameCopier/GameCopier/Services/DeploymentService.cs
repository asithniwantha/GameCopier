using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public DeploymentService(int maxConcurrentJobs = 4)
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

            // Process jobs concurrently
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

            await CopyDirectoryAsync(sourceDir, targetDir, job, cancellationToken);
        }

        private async Task CopyDirectoryAsync(string sourceDir, string targetDir, DeploymentJob job, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(targetDir);

            var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            var totalFiles = files.Length;
            var processedFiles = 0;

            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            await Parallel.ForEachAsync(files, parallelOptions, async (sourceFile, ct) =>
            {
                var relativePath = Path.GetRelativePath(sourceDir, sourceFile);
                var targetFile = Path.Combine(targetDir, relativePath);
                var targetFileDir = Path.GetDirectoryName(targetFile);

                if (!string.IsNullOrEmpty(targetFileDir))
                {
                    Directory.CreateDirectory(targetFileDir);
                }

                await CopyFileAsync(sourceFile, targetFile, ct);

                var completed = Interlocked.Increment(ref processedFiles);
                var progress = (double)completed / totalFiles * 100;

                job.Progress = Math.Min(progress, 100.0);
                JobUpdated?.Invoke(this, job);
            });
        }

        private static async Task CopyFileAsync(string sourceFile, string targetFile, CancellationToken cancellationToken)
        {
            const int bufferSize = 81920; // 80KB buffer

            using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan);
            using var targetStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan);

            await sourceStream.CopyToAsync(targetStream, bufferSize, cancellationToken);
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