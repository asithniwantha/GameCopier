using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GameCopier.Models
{
    public enum DeploymentJobStatus
    {
        Pending,
        InProgress,
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

        public string DisplayName => $"{Game.Name} ? {TargetDrive.Name}";
        public string StatusDisplay => Status switch
        {
            DeploymentJobStatus.Pending => "Pending",
            DeploymentJobStatus.InProgress => $"Copying... {Progress:F0}%",
            DeploymentJobStatus.Completed => "? Completed",
            DeploymentJobStatus.Failed => $"? Failed: {ErrorMessage}",
            DeploymentJobStatus.Cancelled => "Cancelled",
            _ => "Unknown"
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}