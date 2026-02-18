using Caliburn.Micro;

namespace BankDds.Wpf.ViewModels;

public abstract class BaseViewModel : Screen
{
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            NotifyOfPropertyChange(() => IsLoading);
        }
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            NotifyOfPropertyChange(() => ErrorMessage);
            NotifyOfPropertyChange(() => HasError);
        }
    }

    private string _successMessage = string.Empty;
    public string SuccessMessage
    {
        get => _successMessage;
        set
        {
            _successMessage = value;
            NotifyOfPropertyChange(() => SuccessMessage);
            NotifyOfPropertyChange(() => HasSuccess);
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    protected async Task ExecuteWithLoadingAsync(Func<Task> action)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
