using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace OsEngine.OsaExtension.MVVM.Converters
{
    public class ConverterColorToSide : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush color = Brushes.White;

            if (value is Side)
            {
                if ((Side)value == Side.Buy)
                {
                    color = Brushes.DarkGreen;
                }
                else
                {
                    color = Brushes.Red;
                }
            }
            return color;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ConverterIsRunToBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = "СТАРТ";

            if (value is bool)
            {
                if ((bool)value == true)
                {
                    str = "СТОП";
                }
            }
            return str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
