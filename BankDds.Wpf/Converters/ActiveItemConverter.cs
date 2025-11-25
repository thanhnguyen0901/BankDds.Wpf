using System.Globalization;
using System.Windows.Data;

namespace BankDds.Wpf.Converters;

public class ActiveItemConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2 || values[0] == null || values[1] == null)
            return "Inactive";

        var activeItem = values[0];
        var expectedType = values[1] as string;

        if (expectedType == null)
            return "Inactive";

        var activeItemTypeName = activeItem.GetType().Name;

        // Check if active item matches the expected type
        return activeItemTypeName.Contains(expectedType, StringComparison.OrdinalIgnoreCase) 
            ? "Active" 
            : "Inactive";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
