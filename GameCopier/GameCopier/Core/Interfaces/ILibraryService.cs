using GameCopier.Models.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameCopier.Core.Interfaces
{
    /// <summary>
    /// Interface for library management operations
    /// </summary>
    public interface ILibraryService
    {
        Task<IEnumerable<Game>> GetGamesAsync();
        Task<IEnumerable<Software>> GetSoftwareAsync();
        Task<IEnumerable<Game>> ScanGameLibraryAsync();
        Task<IEnumerable<Software>> ScanSoftwareLibraryAsync();
        
        Task AddGameFolderAsync(string folderPath);
        Task AddSoftwareFolderAsync(string folderPath);
        Task RemoveGameFolderAsync(string folderPath);
        Task RemoveSoftwareFolderAsync(string folderPath);
        
        List<string> GetGameFolders();
        List<string> GetSoftwareFolders();
        void SaveGameFolders(List<string> folders);
        void SaveSoftwareFolders(List<string> folders);
    }
}