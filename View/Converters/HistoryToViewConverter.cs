using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace View.Converters
{
    public class HistoryToViewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush brush = new SolidColorBrush();
            brush.Color = Colors.Black;

            if (value == null)
            {
                return brush;
            }

            bool isActive = (bool)value;
            if (isActive)
            {
                brush.Color = Colors.DarkRed;
            }
            else
            {
                brush.Opacity = 0.5;
            }

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
