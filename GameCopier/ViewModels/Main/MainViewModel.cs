        private void OnSoftwarePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Models.Domain.Software.IsSelected))
            {
                if (sender is Software selectedSoftware && selectedSoftware.IsSelected)
                {
                    // Clear other selections (radio button behavior)
                    foreach (var software in Software.Where(s => s != selectedSoftware))
                        software.IsSelected = false;
                    foreach (var game in Games)
                        game.IsSelected = false;
                }
                UpdateStatusText();
                UpdateCommandStates();
            }
        }
