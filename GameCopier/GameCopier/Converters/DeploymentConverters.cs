using Microsoft.UI.Xaml.Data;
using System;

namespace GameCopier.Converters
{
    public class DeploymentIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isRunning)
            {
                return isRunning ? "\uE769" : "\uE768"; // Pause or Play icon
            }
            return "\uE768"; // Play icon
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class DeploymentButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isRunning)
            {
                return isRunning ? "DEPLOYING..." : "START DEPLOYMENT";
            }
            return "START DEPLOYMENT";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}