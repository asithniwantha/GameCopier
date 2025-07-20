using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using GameCopier.Models;

namespace GameCopier.Converters
{
    public class StatusToProgressVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DeploymentJobStatus status)
            {
                return status == DeploymentJobStatus.InProgress ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}