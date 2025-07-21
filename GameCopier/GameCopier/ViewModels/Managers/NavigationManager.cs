using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace GameCopier.ViewModels.Managers
{
    /// <summary>
    /// Manages folder navigation and external operations
    /// </summary>
    public class NavigationManager
    {
        private readonly DispatcherQueue? _uiDispatcher;

        public event EventHandler<string>? StatusChanged;

        public NavigationManager(DispatcherQueue? uiDispatcher)
        {
            _uiDispatcher = uiDispatcher;
        }

        public void OpenFolderInExplorer(string folderPath)
        {
            try
            {
                if (string.IsNullOrEmpty(folderPath))
                {
                    System.Diagnostics.Debug.WriteLine("? NavigationManager: Folder path is null/empty");
                    return;
                }

                if (!Directory.Exists(folderPath))
                {
                    System.Diagnostics.Debug.WriteLine($"? NavigationManager: Folder does not exist: {folderPath}");
                    StatusChanged?.Invoke(this, $"? Folder not found: {folderPath}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"?? NavigationManager: Opening folder: {folderPath}");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{folderPath}\"",
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                
                var folderName = Path.GetFileName(folderPath);
                StatusChanged?.Invoke(this, $"?? Opened folder: {folderName}");
                
                // Reset status after a moment
                _ = Task.Delay(2000).ContinueWith(_ =>
                {
                    _uiDispatcher?.TryEnqueue(() => 
                    {
                        // Could add logic to reset status if needed
                    });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? NavigationManager: Error opening folder - {ex.Message}");
                StatusChanged?.Invoke(this, $"? Error opening folder: {ex.Message}");
            }
        }

        public void OpenGameFolder(Models.Game? game)
        {
            if (game == null || string.IsNullOrEmpty(game.FolderPath))
            {
                System.Diagnostics.Debug.WriteLine("? NavigationManager: Game or FolderPath is null/empty");
                return;
            }

            OpenFolderInExplorer(game.FolderPath);
        }

        public void OpenSoftwareFolder(Models.Software? software)
        {
            if (software == null || string.IsNullOrEmpty(software.FolderPath))
            {
                System.Diagnostics.Debug.WriteLine("? NavigationManager: Software or FolderPath is null/empty");
                return;
            }

            OpenFolderInExplorer(software.FolderPath);
        }
    }
}