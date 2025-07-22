using GameCopier.Models.Domain;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace GameCopier.Converters
{
    public class PendingStatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DeploymentJobStatus status)
            {
                return status == DeploymentJobStatus.Pending ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}