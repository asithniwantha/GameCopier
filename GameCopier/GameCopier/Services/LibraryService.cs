using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GameCopier.Models;

namespace GameCopier.Services
{
    public class LibraryService
    {
        private List<string> _gameFolders;
        private readonly string _configFilePath;
        private List<Game> _gameCache = new();

        public LibraryService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configFolder = Path.Combine(appDataPath, "GameCopier");
            Directory.CreateDirectory(configFolder);
            _configFilePath = Path.Combine(configFolder, "gamefolders.json");
            _gameFolders = LoadGameFolders();
        }

        private List<string> LoadGameFolders()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    // Default folder if config doesn't exist
                    var defaultFolders = new List<string> { "C:\\GameLibrary" };
                    SaveGameFolders(defaultFolders);
                    return defaultFolders;
                }
                var json = File.ReadAllText(_configFilePath);
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading game folders: {ex.Message}");
                return new List<string>();
            }
        }

        private void SaveGameFolders(List<string> folders)
        {
            try
            {
                var json = JsonSerializer.Serialize(folders, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving game folders: {ex.Message}");
            }
        }

        public async Task AddGameFolderAsync(string folderPath)
        {
            if (!_gameFolders.Contains(folderPath))
            {
                _gameFolders.Add(folderPath);
                SaveGameFolders(_gameFolders);
                await ScanLibraryAsync(); // Rescan library after adding a new folder
            }
        }

        public async Task RemoveGameFolderAsync(string folderPath)
        {
            if (_gameFolders.Contains(folderPath))
            {
                _gameFolders.Remove(folderPath);
                SaveGameFolders(_gameFolders);
                await ScanLibraryAsync(); // Rescan library after removing a folder
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
                await ScanLibraryAsync();
            }
            return _gameCache;
        }

        public async Task<IEnumerable<Game>> ScanLibraryAsync()
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
                        System.Diagnostics.Debug.WriteLine($"Error scanning library path {libraryPath}: {ex.Message}");
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