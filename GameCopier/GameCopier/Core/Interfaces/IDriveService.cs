using GameCopier.Models.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameCopier.Core.Interfaces
{
    /// <summary>
    /// Interface for drive detection and management operations
    /// </summary>
    public interface IDriveService
    {
        Task<IEnumerable<Drive>> GetRemovableDrivesAsync();
        Task<IEnumerable<Drive>> GetRemovableDrivesWithHighlightAsync(string? mostRecentDrive);
        bool HasSufficientSpace(Drive drive, IEnumerable<Game> games);
    }
}