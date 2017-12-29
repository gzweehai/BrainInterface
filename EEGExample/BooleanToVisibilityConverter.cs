using System;
using System.Windows;
using System.Windows.Data;

namespace Abt.Controls.SciChart.Example.Common
{
    public class BooleanToVisibilityConverter:IValueConverter
    {
        private const string InvertionFlag = "INVERSE";

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var stringParam = parameter as string;
            var inverse = string.Equals(stringParam, InvertionFlag, StringComparison.InvariantCultureIgnoreCase);

            var onTrue = inverse ? Visibility.Collapsed : Visibility.Visible;
            var onFalse = inverse ? Visibility.Visible : Visibility.Collapsed;

            return (bool)value ? onTrue : onFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
