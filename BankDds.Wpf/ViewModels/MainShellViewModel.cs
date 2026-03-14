using Caliburn.Micro;

namespace BankDds.Wpf.ViewModels
{
    /// <summary>
    /// Controls transition between login screen and the main home workspace.
    /// </summary>
    public class MainShellViewModel : Conductor<Screen>.Collection.OneActive
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainShellViewModel"/> class.
        /// </summary>
        public MainShellViewModel()
        {
            DisplayName = "Hệ thống ngân hàng phân tán";
        }

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            await base.OnInitializeAsync(cancellationToken);
            await ShowLoginAsync();
        }

        public async Task ShowLoginAsync()
        {
            var loginViewModel = IoC.Get<LoginViewModel>();
            await ActivateItemAsync(loginViewModel, cancellationToken: default);
        }

        public async Task ShowHomeAsync()
        {
            var homeViewModel = IoC.Get<HomeViewModel>();
            await ActivateItemAsync(homeViewModel, cancellationToken: default);
        }
    }
}
