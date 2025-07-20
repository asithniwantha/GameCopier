using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using GameCopier.ViewModels;
using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;

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
            await dialog.ShowAsync();
        }
    }
}
