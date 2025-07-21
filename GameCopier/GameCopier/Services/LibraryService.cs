using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GameCopier.Models;

namespace GameCopier.Services
{
    [JsonSerializable(typeof(List<string>))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    internal partial class LibraryJsonContext : JsonSerializerContext
    {
    }

    public class LibraryService
    {
        private List<string> _gameFolders;
        private List<string> _softwareFolders;
        private readonly string _gameConfigFilePath;
        private readonly string _softwareConfigFilePath;
        private List<Game> _gameCache = new();
        private List<Software> _softwareCache = new();

        public LibraryService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configFolder = Path.Combine(appDataPath, "GameCopier");
            Directory.CreateDirectory(configFolder);
            _gameConfigFilePath = Path.Combine(configFolder, "gamefolders.json");
            _softwareConfigFilePath = Path.Combine(configFolder, "softwarefolders.json");
            System.Diagnostics.Debug.WriteLine($"[LibraryService] Game config file path: {_gameConfigFilePath}");
            System.Diagnostics.Debug.WriteLine($"[LibraryService] Software config file path: {_softwareConfigFilePath}");
            _gameFolders = LoadGameFolders();
            _softwareFolders = LoadSoftwareFolders();
        }

        #region Game Management
        private List<string> LoadGameFolders()
        {
            try
            {
                if (!File.Exists(_gameConfigFilePath))
                {
                    // Default folder if config doesn't exist
                    var defaultFolders = new List<string> { "C:\\GameLibrary" };
                    SaveGameFolders(defaultFolders);
                    System.Diagnostics.Debug.WriteLine($"[LibraryService] No game config file, created default at: {_gameConfigFilePath}");
                    return defaultFolders;
                }
                var json = File.ReadAllText(_gameConfigFilePath);
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Game folders loaded from: {_gameConfigFilePath}");
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Game folders loaded: {json}");
                return JsonSerializer.Deserialize(json, LibraryJsonContext.Default.ListString) ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Error loading game folders: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Exception details: {ex}");
                return new List<string>();
            }
        }

        public void SaveGameFolders(List<string> folders)
        {
            try
            {
                var json = JsonSerializer.Serialize(folders, LibraryJsonContext.Default.ListString);
                File.WriteAllText(_gameConfigFilePath, json);
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Game folders saved to: {_gameConfigFilePath}");
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Game folders saved: {json}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Error saving game folders: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Exception details: {ex}");
            }
        }

        public async Task AddGameFolderAsync(string folderPath)
        {
            if (!_gameFolders.Contains(folderPath))
            {
                _gameFolders.Add(folderPath);
                SaveGameFolders(_gameFolders);
                await ScanGameLibraryAsync(); // Rescan library after adding a new folder
            }
        }

        public async Task RemoveGameFolderAsync(string folderPath)
        {
            if (_gameFolders.Contains(folderPath))
            {
                _gameFolders.Remove(folderPath);
                SaveGameFolders(_gameFolders);
                await ScanGameLibraryAsync(); // Rescan library after removing a folder
            }
        }

        public IEnumerable<string> GetGameFolders()
        {
            return _gameFolders.AsEnumerable();
        }

        public async Task<IEnumerable<Game>> GetGamesAsync()
        {
            if (!_gameCache.Any())
            {
                await ScanGameLibraryAsync();
            }
            return _gameCache;
        }

        public async Task<IEnumerable<Game>> ScanGameLibraryAsync()
        {
            return await Task.Run(() =>
            {
                var games = new List<Game>();
                foreach (var libraryPath in _gameFolders)
                {
                    if (!Directory.Exists(libraryPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Game library path not found: {libraryPath}");
                        continue;
                    }

                    try
                    {
                        var gameDirectories = Directory.GetDirectories(libraryPath);

                        foreach (var gameDir in gameDirectories)
                        {
                            var dirInfo = new DirectoryInfo(gameDir);
                            var size = GetDirectorySize(dirInfo);

                            var game = new Game();
                            game.Name = dirInfo.Name;
                            game.FolderPath = gameDir;
                            game.SizeInBytes = size;
                            games.Add(game);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error scanning game library path {libraryPath}: {ex.Message}");
                    }
                }
                _gameCache = games.OrderBy(g => g.Name).ToList();
                return _gameCache;
            });
        }

        public IEnumerable<Game> SearchGames(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return _gameCache;

            return _gameCache.Where(g => g.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }
        #endregion

        #region Software Management
        private List<string> LoadSoftwareFolders()
        {
            try
            {
                if (!File.Exists(_softwareConfigFilePath))
                {
                    // Default folder if config doesn't exist
                    var defaultFolders = new List<string> { "C:\\SoftwareLibrary" };
                    SaveSoftwareFolders(defaultFolders);
                    System.Diagnostics.Debug.WriteLine($"[LibraryService] No software config file, created default at: {_softwareConfigFilePath}");
                    return defaultFolders;
                }
                var json = File.ReadAllText(_softwareConfigFilePath);
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Software folders loaded from: {_softwareConfigFilePath}");
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Software folders loaded: {json}");
                return JsonSerializer.Deserialize(json, LibraryJsonContext.Default.ListString) ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Error loading software folders: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Exception details: {ex}");
                return new List<string>();
            }
        }

        public void SaveSoftwareFolders(List<string> folders)
        {
            try
            {
                var json = JsonSerializer.Serialize(folders, LibraryJsonContext.Default.ListString);
                File.WriteAllText(_softwareConfigFilePath, json);
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Software folders saved to: {_softwareConfigFilePath}");
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Software folders saved: {json}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Error saving software folders: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Exception details: {ex}");
            }
        }

        public async Task AddSoftwareFolderAsync(string folderPath)
        {
            if (!_softwareFolders.Contains(folderPath))
            {
                _softwareFolders.Add(folderPath);
                SaveSoftwareFolders(_softwareFolders);
                await ScanSoftwareLibraryAsync(); // Rescan library after adding a new folder
            }
        }

        public async Task RemoveSoftwareFolderAsync(string folderPath)
        {
            if (_softwareFolders.Contains(folderPath))
            {
                _softwareFolders.Remove(folderPath);
                SaveSoftwareFolders(_softwareFolders);
                await ScanSoftwareLibraryAsync(); // Rescan library after removing a folder
            }
        }

        public IEnumerable<string> GetSoftwareFolders()
        {
            return _softwareFolders.AsEnumerable();
        }

        public async Task<IEnumerable<Software>> GetSoftwareAsync()
        {
            if (!_softwareCache.Any())
            {
                await ScanSoftwareLibraryAsync();
            }
            return _softwareCache;
        }

        public async Task<IEnumerable<Software>> ScanSoftwareLibraryAsync()
        {
            return await Task.Run(() =>
            {
                var software = new List<Software>();
                foreach (var libraryPath in _softwareFolders)
                {
                    if (!Directory.Exists(libraryPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Software library path not found: {libraryPath}");
                        continue;
                    }

                    try
                    {
                        var softwareDirectories = Directory.GetDirectories(libraryPath);

                        foreach (var softwareDir in softwareDirectories)
                        {
                            var dirInfo = new DirectoryInfo(softwareDir);
                            var size = GetDirectorySize(dirInfo);

                            var softwareItem = new Software();
                            softwareItem.Name = dirInfo.Name;
                            softwareItem.FolderPath = softwareDir;
                            softwareItem.SizeInBytes = size;
                            software.Add(softwareItem);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error scanning software library path {libraryPath}: {ex.Message}");
                    }
                }
                _softwareCache = software.OrderBy(s => s.Name).ToList();
                return _softwareCache;
            });
        }

        public IEnumerable<Software> SearchSoftware(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return _softwareCache;

            return _softwareCache.Where(s => s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }
        #endregion

        #region Legacy Methods (for backward compatibility)
        [Obsolete("Use ScanGameLibraryAsync instead")]
        public async Task<IEnumerable<Game>> ScanLibraryAsync()
        {
            return await ScanGameLibraryAsync();
        }
        #endregion

        private static long GetDirectorySize(DirectoryInfo dirInfo)
        {
            long size = 0;

            try
            {
                var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
                size = files.Sum(f => f.Length);
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we can't access
            }
            catch (DirectoryNotFoundException)
            {
                // Skip if directory doesn't exist
            }

            return size;
        }
    }
}