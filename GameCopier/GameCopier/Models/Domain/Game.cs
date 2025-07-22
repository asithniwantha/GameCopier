using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GameCopier.Models.Domain
{
    public class Game : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _folderPath = string.Empty;
        private long _sizeInBytes;
        private bool _isSelected;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string FolderPath
        {
            get => _folderPath;
            set
            {
                _folderPath = value;
                OnPropertyChanged();
            }
        }

        public long SizeInBytes
        {
            get => _sizeInBytes;
            set
            {
                _sizeInBytes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SizeDisplay));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public string SizeDisplay => FormatBytes(SizeInBytes);

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
    }
}