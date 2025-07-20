using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GameCopier.Models
{
    public enum DeploymentJobStatus
    {
        Pending,
        InProgress,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    public class DeploymentJob : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private Game _game = new Game();
        private Drive _targetDrive = new Drive();
        private DeploymentJobStatus _status = DeploymentJobStatus.Pending;
        private double _progress = 0.0;
        private string? _errorMessage;
        private DateTime _createdAt = DateTime.Now;
        private DateTime? _startedAt;
        private DateTime? _completedAt;
        private DateTime? _pausedAt;
        private CancellationTokenSource? _cancellationTokenSource;

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public Game Game
        {
            get => _game;
            set
            {
                _game = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public Drive TargetDrive
        {
            get => _targetDrive;
            set
            {
                _targetDrive = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public DeploymentJobStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusDisplay));
                OnPropertyChanged(nameof(CanPause));
                OnPropertyChanged(nameof(CanCancel));
                OnPropertyChanged(nameof(CanResume));
            }
        }

        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusDisplay));
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusDisplay));
            }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set
            {
                _createdAt = value;
                OnPropertyChanged();
            }
        }

        public DateTime? StartedAt
        {
            get => _startedAt;
            set
            {
                _startedAt = value;
                OnPropertyChanged();
            }
        }

        public DateTime? CompletedAt
        {
            get => _completedAt;
            set
            {
                _completedAt = value;
                OnPropertyChanged();
            }
        }

        public DateTime? PausedAt
        {
            get => _pausedAt;
            set
            {
                _pausedAt = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Individual cancellation token source for this job
        /// </summary>
        public CancellationTokenSource? CancellationTokenSource
        {
            get => _cancellationTokenSource;
            set
            {
                _cancellationTokenSource = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Cancellation token for this specific job
        /// </summary>
        public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;

        public string DisplayName => $"{Game.Name} ? {TargetDrive.Name}";
        
        public string StatusDisplay => Status switch
        {
            DeploymentJobStatus.Pending => "Pending",
            DeploymentJobStatus.InProgress => $"Copying... {Progress:F0}%",
            DeploymentJobStatus.Paused => $"?? Paused at {Progress:F0}%",
            DeploymentJobStatus.Completed => "? Completed",
            DeploymentJobStatus.Failed => $"? Failed: {ErrorMessage}",
            DeploymentJobStatus.Cancelled => "?? Cancelled",
            _ => "Unknown"
        };

        // UI Control Properties
        public bool CanPause => Status == DeploymentJobStatus.InProgress;
        public bool CanCancel => Status == DeploymentJobStatus.Pending || Status == DeploymentJobStatus.InProgress || Status == DeploymentJobStatus.Paused;
        public bool CanResume => Status == DeploymentJobStatus.Paused;

        /// <summary>
        /// Pause this individual job
        /// </summary>
        public void Pause()
        {
            if (Status == DeploymentJobStatus.InProgress)
            {
                Status = DeploymentJobStatus.Paused;
                PausedAt = DateTime.Now;
                System.Diagnostics.Debug.WriteLine($"?? Job paused: {DisplayName}");
            }
        }

        /// <summary>
        /// Resume this individual job
        /// </summary>
        public void Resume()
        {
            if (Status == DeploymentJobStatus.Paused)
            {
                Status = DeploymentJobStatus.InProgress;
                PausedAt = null;
                System.Diagnostics.Debug.WriteLine($"?? Job resumed: {DisplayName}");
            }
        }

        /// <summary>
        /// Cancel this individual job
        /// </summary>
        public void Cancel()
        {
            if (CanCancel)
            {
                _cancellationTokenSource?.Cancel();
                Status = DeploymentJobStatus.Cancelled;
                System.Diagnostics.Debug.WriteLine($"?? Job cancelled: {DisplayName}");
            }
        }

        /// <summary>
        /// Initialize cancellation token for this job
        /// </summary>
        public void InitializeCancellation()
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }
    }
}