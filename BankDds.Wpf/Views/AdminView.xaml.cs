using System.Windows.Controls;
using BankDds.Wpf.ViewModels;

namespace BankDds.Wpf.Views;

public partial class AdminView : UserControl
{
    public AdminView()
    {
        InitializeComponent();
    }

    private void OnNewPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is AdminViewModel vm && sender is PasswordBox passwordBox)
        {
            vm.NewPassword = passwordBox.Password;
        }
    }

    private void OnConfirmPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is AdminViewModel vm && sender is PasswordBox passwordBox)
        {
            vm.ConfirmPassword = passwordBox.Password;
        }
    }
}
