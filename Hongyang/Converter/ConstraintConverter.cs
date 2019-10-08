using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Hongyang.Converter
{
    public class ConstraintConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType.Name == "Boolean")
            {
                return value.ToString() == "unspecified" ? false : true;
            }
            else
            {
                return value.ToString() == "unspecified" ? Visibility.Hidden : Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return int.Parse(parameter.ToString());
        }
    }
}
