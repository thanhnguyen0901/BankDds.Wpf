using System.Globalization;
using System.Windows.Data;

namespace BankDds.Wpf.Converters;

public class NullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Returns false if value is null (used for IsEnabled when adding new customer)
        return value == null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
