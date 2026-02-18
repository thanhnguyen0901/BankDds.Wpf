using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Core.Validators;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

public class CustomersViewModel : BaseViewModel
{
    private readonly ICustomerService _customerService;
    private readonly IUserSession _userSession;
    private readonly IDialogService _dialogService;
    private readonly CustomerValidator _validator;
    
    private ObservableCollection<Customer> _customers = new();
    private Customer? _selectedCustomer;
    private Customer _editingCustomer = new();
    private bool _isEditing;
    private string _errorMessage = string.Empty;

    public CustomersViewModel(ICustomerService customerService, IUserSession userSession, IDialogService dialogService, CustomerValidator validator)
    {
        _customerService = customerService;
        _userSession = userSession;
        _dialogService = dialogService;
        _validator = validator;
        DisplayName = "Customer Management";
    }

    public ObservableCollection<Customer> Customers
    {
        get => _customers;
        set
        {
            _customers = value;
            NotifyOfPropertyChange(() => Customers);
        }
    }

    public Customer? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            _selectedCustomer = value;
            NotifyOfPropertyChange(() => SelectedCustomer);
            NotifyOfPropertyChange(() => CanEdit);
            NotifyOfPropertyChange(() => CanDelete);
            NotifyOfPropertyChange(() => CanRestore);
        }
    }

    public Customer EditingCustomer
    {
        get => _editingCustomer;
        set
        {
            _editingCustomer = value;
            NotifyOfPropertyChange(() => EditingCustomer);
        }
    }

    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            _isEditing = value;
            NotifyOfPropertyChange(() => IsEditing);
            NotifyOfPropertyChange(() => CanAdd);
            NotifyOfPropertyChange(() => CanEdit);
            NotifyOfPropertyChange(() => CanDelete);
            NotifyOfPropertyChange(() => CanRestore);
            NotifyOfPropertyChange(() => CanSave);
            NotifyOfPropertyChange(() => CanCancel);
        }
    }

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

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    // CanExecute properties - Standard CRUD pattern with soft delete
    public bool CanAdd => !IsEditing;
    public bool CanEdit => SelectedCustomer != null && SelectedCustomer.TrangThaiXoa == 0 && !IsEditing;
    public bool CanDelete => SelectedCustomer != null && SelectedCustomer.TrangThaiXoa == 0 && !IsEditing;
    public bool CanRestore => SelectedCustomer != null && SelectedCustomer.TrangThaiXoa == 1 && !IsEditing;
    public bool CanSave => IsEditing && !string.IsNullOrWhiteSpace(EditingCustomer.CMND);
    public bool CanCancel => IsEditing;

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            List<Customer> customers;
            
            if (_userSession.UserGroup == UserGroup.NganHang)
            {
                customers = await _customerService.GetAllCustomersAsync();
            }
            else
            {
                customers = await _customerService.GetCustomersByBranchAsync(_userSession.SelectedBranch);
            }

            Customers = new ObservableCollection<Customer>(customers);
        });
    }

    public void Add()
    {
        EditingCustomer = new Customer { MaCN = _userSession.SelectedBranch };
        SelectedCustomer = null;
        IsEditing = true;
        ErrorMessage = string.Empty;
    }

    public void Edit()
    {
        if (SelectedCustomer == null) return;
        
        EditingCustomer = new Customer
        {
            CMND = SelectedCustomer.CMND,
            Ho = SelectedCustomer.Ho,
            Ten = SelectedCustomer.Ten,
            DiaChi = SelectedCustomer.DiaChi,
            SDT = SelectedCustomer.SDT,
            Phai = SelectedCustomer.Phai,
            MaCN = SelectedCustomer.MaCN
        };
        IsEditing = true;
        ErrorMessage = string.Empty;
    }

    public async Task Delete()
    {
        if (SelectedCustomer == null) return;

        // Show confirmation dialog
        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Are you sure you want to delete customer '{SelectedCustomer.FullName}'?",
            "Delete Confirmation"
        );

        if (!confirmed) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _customerService.DeleteCustomerAsync(SelectedCustomer.CMND);
            if (result)
            {
                await LoadCustomersAsync();
                SelectedCustomer = null;
                SuccessMessage = "Customer deleted successfully.";
            }
            else
            {
                ErrorMessage = "Failed to delete customer";
            }
        });
    }

    public async Task Restore()
    {
        if (SelectedCustomer == null) return;

        // Show confirmation dialog
        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Are you sure you want to restore customer '{SelectedCustomer.FullName}'?",
            "Restore Confirmation"
        );

        if (!confirmed) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _customerService.RestoreCustomerAsync(SelectedCustomer.CMND);
            if (result)
            {
                await LoadCustomersAsync();
                SuccessMessage = "Customer restored successfully.";
            }
            else
            {
                ErrorMessage = "Failed to restore customer";
            }
        });
    }

    public async Task Save()
    {
        // Validate before saving
        var validationResult = await _validator.ValidateAsync(EditingCustomer);
        if (!validationResult.IsValid)
        {
            // Aggregate all validation errors
            ErrorMessage = string.Join(Environment.NewLine, 
                validationResult.Errors.Select(e => e.ErrorMessage));
            return;
        }

        await ExecuteWithLoadingAsync(async () =>
        {
            bool result;
            
            if (SelectedCustomer == null) // Adding new
            {
                result = await _customerService.AddCustomerAsync(EditingCustomer);
            }
            else // Updating existing
            {
                result = await _customerService.UpdateCustomerAsync(EditingCustomer);
            }

            if (result)
            {
                IsEditing = false;
                await LoadCustomersAsync();
                SelectedCustomer = null;
                SuccessMessage = "Customer saved successfully.";
            }
            else
            {
                ErrorMessage = "Failed to save customer";
            }
        });
    }

    public void Cancel()
    {
        IsEditing = false;
        EditingCustomer = new Customer();
        ErrorMessage = string.Empty;
    }
}
