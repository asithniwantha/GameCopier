using GameCopier.Models.Domain;
using GameCopier.ViewModels.Main;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;

namespace GameCopier.Views.Main
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            ViewModel.RequestSettingsDialog += OnRequestSettingsDialog;

            // Set window properties for kiosk mode
            Title = "GameDeploy Kiosk";
        }

        private async void OnRequestSettingsDialog(object? sender, EventArgs e)
        {
            var dialog = new Views.Dialogs.SettingsDialog();

            // Show the dialog and wait for it to close
            await dialog.ShowAsync();

            // After the settings dialog closes, refresh the drives to apply any changes
            System.Diagnostics.Debug.WriteLine("🔄 Settings dialog closed, refreshing drives...");
            await ViewModel.RefreshDrivesAfterSettingsChange();
        }

        private void GameItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            try
            {
                if (sender is FrameworkElement element && element.DataContext is Game game)
                {
                    System.Diagnostics.Debug.WriteLine($"🎮 Double-tapped game: {game.Name} at {game.FolderPath}");
                    ViewModel.OpenGameFolderCommand.Execute(game);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error handling game double-tap: {ex.Message}");
            }
        }

        private void SoftwareItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            try
            {
                if (sender is FrameworkElement element && element.DataContext is Software software)
                {
                    System.Diagnostics.Debug.WriteLine($"💾 Double-tapped software: {software.Name} at {software.FolderPath}");
                    ViewModel.OpenSoftwareFolderCommand.Execute(software);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error handling software double-tap: {ex.Message}");
            }
        }
    }
}