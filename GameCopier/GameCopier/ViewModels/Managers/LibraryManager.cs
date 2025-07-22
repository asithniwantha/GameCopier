using GameCopier.Models.Domain;
using GameCopier.Services.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GameCopier.ViewModels.Managers
{
    /// <summary>
    /// Manages games and software library operations
    /// </summary>
    public class LibraryManager : INotifyPropertyChanged
    {
        private readonly LibraryService _libraryService;

        public event EventHandler<List<Game>>? GamesUpdated;
        public event EventHandler<List<Software>>? SoftwareUpdated;
        public event EventHandler<string>? StatusChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public LibraryManager()
        {
            _libraryService = new LibraryService();
        }

        public async Task<List<Game>> LoadGamesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📂 LibraryManager: Loading games...");
                var games = await _libraryService.GetGamesAsync();

                // Create test data if no games found
                if (!games.Any())
                {
                    games = await CreateTestGamesAsync();
                }

                System.Diagnostics.Debug.WriteLine($"📂 LibraryManager: Loaded {games.Count()} games");
                GamesUpdated?.Invoke(this, games.ToList());
                return games.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LibraryManager: Error loading games - {ex.Message}");
                StatusChanged?.Invoke(this, $"Error loading games: {ex.Message}");
                return new List<Game>();
            }
        }

        public async Task<List<Software>> LoadSoftwareAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📂 LibraryManager: Loading software...");
                var software = await _libraryService.GetSoftwareAsync();

                // Create test data if no software found
                if (!software.Any())
                {
                    software = await CreateTestSoftwareAsync();
                }

                System.Diagnostics.Debug.WriteLine($"📂 LibraryManager: Loaded {software.Count()} software items");
                SoftwareUpdated?.Invoke(this, software.ToList());
                return software.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LibraryManager: Error loading software - {ex.Message}");
                StatusChanged?.Invoke(this, $"Error loading software: {ex.Message}");
                return new List<Software>();
            }
        }

        public async Task<List<Game>> RefreshGamesAsync()
        {
            try
            {
                StatusChanged?.Invoke(this, "🔄 Scanning game library...");
                var games = await _libraryService.ScanGameLibraryAsync();

                System.Diagnostics.Debug.WriteLine($"🔄 LibraryManager: Refreshed {games.Count()} games");
                GamesUpdated?.Invoke(this, games.ToList());
                return games.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LibraryManager: Error refreshing games - {ex.Message}");
                StatusChanged?.Invoke(this, $"Error refreshing library: {ex.Message}");
                return new List<Game>();
            }
        }

        public async Task<List<Software>> RefreshSoftwareAsync()
        {
            try
            {
                StatusChanged?.Invoke(this, "🔄 Scanning software library...");
                var software = await _libraryService.ScanSoftwareLibraryAsync();

                System.Diagnostics.Debug.WriteLine($"🔄 LibraryManager: Refreshed {software.Count()} software items");
                SoftwareUpdated?.Invoke(this, software.ToList());
                return software.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LibraryManager: Error refreshing software - {ex.Message}");
                StatusChanged?.Invoke(this, $"Error refreshing software library: {ex.Message}");
                return new List<Software>();
            }
        }

        public async Task AddGameFolderAsync(string folderPath)
        {
            try
            {
                await _libraryService.AddGameFolderAsync(folderPath);
                StatusChanged?.Invoke(this, $"✅ Added game folder: {Path.GetFileName(folderPath)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LibraryManager: Error adding game folder - {ex.Message}");
                StatusChanged?.Invoke(this, $"Error adding game folder: {ex.Message}");
            }
        }

        public async Task AddSoftwareFolderAsync(string folderPath)
        {
            try
            {
                await _libraryService.AddSoftwareFolderAsync(folderPath);
                StatusChanged?.Invoke(this, $"✅ Added software folder: {Path.GetFileName(folderPath)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LibraryManager: Error adding software folder - {ex.Message}");
                StatusChanged?.Invoke(this, $"Error adding software folder: {ex.Message}");
            }
        }

        public IEnumerable<Game> SearchGames(string searchText)
        {
            return _libraryService.SearchGames(searchText);
        }

        public IEnumerable<Software> SearchSoftware(string searchText)
        {
            return _libraryService.SearchSoftware(searchText);
        }

        private async Task<List<Game>> CreateTestGamesAsync()
        {
            System.Diagnostics.Debug.WriteLine("🧪 LibraryManager: Creating test games...");

            var testBaseDir = Path.Combine(Path.GetTempPath(), "GameCopierTest");
            Directory.CreateDirectory(testBaseDir);

            var sampleGames = new List<Game>();
            var gameNames = new[] { "TestGame1", "TestGame2", "TestGame3" };

            foreach (var gameName in gameNames)
            {
                var gameDir = Path.Combine(testBaseDir, gameName);
                Directory.CreateDirectory(gameDir);

                // Create test files
                var testFile1 = Path.Combine(gameDir, "game.exe");
                var testFile2 = Path.Combine(gameDir, "data.txt");
                var subDir = Path.Combine(gameDir, "assets");
                Directory.CreateDirectory(subDir);
                var testFile3 = Path.Combine(subDir, "image.png");

                await File.WriteAllTextAsync(testFile1, $"Test executable for {gameName}");
                await File.WriteAllTextAsync(testFile2, $"Test data file for {gameName} with more content to make it bigger.");
                await File.WriteAllTextAsync(testFile3, $"Test asset file for {gameName}");

                // Calculate size
                long size = Directory.GetFiles(gameDir, "*", SearchOption.AllDirectories)
                                   .Sum(f => new FileInfo(f).Length);

                sampleGames.Add(new Game
                {
                    Name = gameName,
                    SizeInBytes = Math.Max(size, 1024),
                    FolderPath = gameDir
                });

                System.Diagnostics.Debug.WriteLine($"🧪 Created test game: {gameName} ({size} bytes)");
            }

            return sampleGames;
        }

        private async Task<List<Software>> CreateTestSoftwareAsync()
        {
            System.Diagnostics.Debug.WriteLine("🧪 LibraryManager: Creating test software...");

            var testBaseDir = Path.Combine(Path.GetTempPath(), "GameCopierTest", "Software");
            Directory.CreateDirectory(testBaseDir);

            var sampleSoftware = new List<Software>();
            var softwareNames = new[] { "TestApp1", "TestApp2", "TestApp3" };

            foreach (var appName in softwareNames)
            {
                var appDir = Path.Combine(testBaseDir, appName);
                Directory.CreateDirectory(appDir);

                // Create test files
                var testFile1 = Path.Combine(appDir, "app.exe");
                var testFile2 = Path.Combine(appDir, "config.ini");
                var libDir = Path.Combine(appDir, "lib");
                Directory.CreateDirectory(libDir);
                var testFile3 = Path.Combine(libDir, "library.dll");

                await File.WriteAllTextAsync(testFile1, $"Test application executable for {appName}");
                await File.WriteAllTextAsync(testFile2, $"[Settings]\nAppName={appName}\nVersion=1.0");
                await File.WriteAllTextAsync(testFile3, $"Test library file for {appName}");

                // Calculate size
                long size = Directory.GetFiles(appDir, "*", SearchOption.AllDirectories)
                                   .Sum(f => new FileInfo(f).Length);

                sampleSoftware.Add(new Software
                {
                    Name = appName,
                    SizeInBytes = Math.Max(size, 1024),
                    FolderPath = appDir
                });

                System.Diagnostics.Debug.WriteLine($"🧪 Created test software: {appName} ({size} bytes)");
            }

            return sampleSoftware;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}