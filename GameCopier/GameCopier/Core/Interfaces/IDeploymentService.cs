using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameCopier.Core.Interfaces
{
    /// <summary>
    /// Interface for deployment operations
    /// </summary>
    public interface IDeploymentService
    {
        Task<bool> DeployAsync(string sourcePath, string destinationPath, 
            Action<string>? statusCallback = null, CancellationToken cancellationToken = default);
        
        Task<bool> CopyDirectoryWithDialogAsync(string sourcePath, string destinationPath,
            IntPtr parentWindowHandle, Action<string>? statusCallback = null, 
            CancellationToken cancellationToken = default);
    }
}