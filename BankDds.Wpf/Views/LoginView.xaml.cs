using System.Windows;
using System.Windows.Controls;
using BankDds.Wpf.ViewModels;

namespace BankDds.Wpf.Views
{
    /// <summary>
    /// Handles LoginView responsibilities in the application.
    /// </summary>
    public partial class LoginView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoginView"/> class.
        /// </summary>
        public LoginView()
        {
            InitializeComponent();
        }

        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.Password = passwordBox.Password;
            }
        }
    }
}
