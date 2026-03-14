using System;
using System.Globalization;
using System.Windows.Data;
using BankDds.Core.Models;

namespace BankDds.Wpf.Converters
{
    /// <summary>
    /// Converts user group enum values to Vietnamese display labels in the UI.
    /// </summary>
    public class UserGroupDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not UserGroup group)
            {
                return string.Empty;
            }
            return group switch
            {
                UserGroup.NganHang => "Ngân hàng",
                UserGroup.ChiNhanh => "Chi nhánh",
                UserGroup.KhachHang => "Khách hàng",
                _ => group.ToString()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
