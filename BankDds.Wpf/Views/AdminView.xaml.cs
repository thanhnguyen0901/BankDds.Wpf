using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using BankDds.Wpf.ViewModels;

namespace BankDds.Wpf.Views
{
    /// <summary>
    /// Interaction logic for the user administration view.
    /// </summary>
    public partial class AdminView : UserControl
    {
        private AdminViewModel? _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminView"/> class.
        /// </summary>
        public AdminView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
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

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _viewModel = e.NewValue as AdminViewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                SyncPasswordBoxesFromViewModel();
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AdminViewModel.NewPassword) ||
                e.PropertyName == nameof(AdminViewModel.ConfirmPassword))
            {
                SyncPasswordBoxesFromViewModel();
            }
        }

        private void SyncPasswordBoxesFromViewModel()
        {
            if (_viewModel == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(_viewModel.NewPassword) && !string.IsNullOrEmpty(NewPasswordBox.Password))
            {
                NewPasswordBox.Password = string.Empty;
            }

            if (string.IsNullOrEmpty(_viewModel.ConfirmPassword) && !string.IsNullOrEmpty(ConfirmPasswordBox.Password))
            {
                ConfirmPasswordBox.Password = string.Empty;
            }
        }
    }
}
