using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using GameCopier.ViewModels;
using System;
using System.Threading.Tasks;

namespace GameCopier
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
            var dialog = new SettingsDialog();
            
            // Show the dialog and wait for it to close
            await dialog.ShowAsync();
            
            // After the settings dialog closes, refresh the drives to apply any changes
            System.Diagnostics.Debug.WriteLine("?? Settings dialog closed, refreshing drives...");
            await ViewModel.RefreshDrivesAfterSettingsChange();
        }
    }
}
