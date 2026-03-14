using System.Globalization;
using System.Windows.Data;

namespace BankDds.Wpf.Converters
{
    /// <summary>
    /// Converts null checks into boolean values for enabling and disabling UI elements.
    /// </summary>
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
