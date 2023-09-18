using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using static ViewModel.MeasurementEditorViewModel;

namespace View.Converters
{
    public class OperationSequenceStateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush brush = new SolidColorBrush();

            if (value == null)
            {
                return brush;
            }

            var state = (OperationSequenceItem.States)value;
            switch (state)
            {
                case OperationSequenceItem.States.Default:
                    brush.Color = Colors.LightGray;
                    break;
                case OperationSequenceItem.States.Queued:
                    brush.Color = Colors.Yellow;
                    break;
                case OperationSequenceItem.States.Running:
                    brush.Color = Colors.Orange;
                    break;
            }

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
