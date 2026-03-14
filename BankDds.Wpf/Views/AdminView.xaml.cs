using System.Windows.Controls;
using BankDds.Wpf.ViewModels;

namespace BankDds.Wpf.Views
{
    /// <summary>
    /// Interaction logic for the user administration view.
    /// </summary>
    public partial class AdminView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdminView"/> class.
        /// </summary>
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
}
