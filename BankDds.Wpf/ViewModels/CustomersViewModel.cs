using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

public class CustomersViewModel : Screen
{
    private readonly ICustomerService _customerService;
    private readonly IUserSession _userSession;
    
    private ObservableCollection<Customer> _customers = new();
    private Customer? _selectedCustomer;
    private Customer _editingCustomer = new();
    private bool _isEditing;
    private string _errorMessage = string.Empty;

    public CustomersViewModel(ICustomerService customerService, IUserSession userSession)
    {
        _customerService = customerService;
        _userSession = userSession;
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
        }
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        try
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
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading customers: {ex.Message}";
        }
    }

    public void Add()
    {
        EditingCustomer = new Customer { MaCN = _userSession.SelectedBranch };
        IsEditing = true;
    }

    public bool CanEdit => SelectedCustomer != null && !IsEditing;

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
    }

    public bool CanDelete => SelectedCustomer != null && !IsEditing;

    public async Task Delete()
    {
        if (SelectedCustomer == null) return;

        try
        {
            var result = await _customerService.DeleteCustomerAsync(SelectedCustomer.CMND);
            if (result)
            {
                await LoadCustomersAsync();
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = "Failed to delete customer";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
    }

    public bool CanSave => IsEditing && !string.IsNullOrWhiteSpace(EditingCustomer.CMND);

    public async Task Save()
    {
        try
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
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = "Failed to save customer";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
    }

    public bool CanCancel => IsEditing;

    public void Cancel()
    {
        IsEditing = false;
        EditingCustomer = new Customer();
        SelectedCustomer = null;
    }
}
