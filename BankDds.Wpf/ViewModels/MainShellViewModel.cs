using Caliburn.Micro;

namespace BankDds.Wpf.ViewModels;

public class MainShellViewModel : Conductor<Screen>.Collection.OneActive
{
    public MainShellViewModel()
    {
        DisplayName = "Distributed Banking System";
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        await base.OnInitializeAsync(cancellationToken);
        
        // Start with Login screen
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
