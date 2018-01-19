using System;
using System.Globalization;
using System.Windows.Data;

namespace SciChart.Examples.Examples.SeeFeaturedApplication.ECGMonitor
{
    /// <summary>
    /// ValueConverter which converts a boolean (IsBeat) to scale, used 
    /// to simulate the beating heart graphic 
    /// </summary>
    public class BeatToScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value) ? 2.2 : 1.5;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}