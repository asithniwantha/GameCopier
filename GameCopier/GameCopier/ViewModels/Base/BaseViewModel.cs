using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GameCopier.ViewModels.Base
{
    /// <summary>
    /// Base class for all ViewModels providing common functionality
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected bool SetProperty<T>(ref T backingStore, T value, Action onChanged, [CallerMemberName] string? propertyName = null)
        {
            if (SetProperty(ref backingStore, value, propertyName))
            {
                onChanged?.Invoke();
                return true;
            }
            return false;
        }
    }
}