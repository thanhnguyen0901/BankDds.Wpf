using System;
using System.Globalization;
using System.Windows.Data;
using BankDds.Core.Formatting;

namespace BankDds.Wpf.Converters
{
    /// <summary>
    /// Converts branch codes to friendly Vietnamese names for display-only bindings.
    /// </summary>
    public class BranchDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            DisplayText.Branch(value?.ToString());

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value;
    }
}
