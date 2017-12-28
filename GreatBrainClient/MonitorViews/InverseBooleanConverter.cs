// *************************************************************************************
// SCICHART © Copyright ABT Software Services Ltd. 2011-2012. All rights reserved.
//  
// Web: http://www.scichart.com
// Support: info@abtsoftware.co.uk
//  
// InverseBooleanConverter.cs is part of SciChart Examples, a High Performance WPF & Silverlight Chart. 
// For full terms and conditions of the SciChart license, see http://www.scichart.com/scichart-eula/
//  
// SciChart Examples source code is provided free of charge on an "As-Is" basis to support
// and provide examples of how to use the SciChart component. You bear the risk of using it. 
// The authors give no express warranties, guarantees or conditions. You may have additional 
// consumer rights under your local laws which this license cannot change. To the extent 
// permitted under your local laws, the contributors exclude the implied warranties of 
// merchantability, fitness for a particular purpose and non-infringement. 
// *************************************************************************************
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace Abt.Controls.SciChart.Example.Common
{
    public enum ConvertTo
    {
        Double,
        DateTime
    }

    public class ToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double result = (double) System.Convert.ToDouble(value);
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IComparableConverter : IValueConverter
    {
        public ConvertTo ConvertTo { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (ConvertTo == ConvertTo.Double)
                return (double) System.Convert.ToDouble(value);

            if (ConvertTo == ConvertTo.DateTime)
                return (DateTime)System.Convert.ToDateTime(value);

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !((bool) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}