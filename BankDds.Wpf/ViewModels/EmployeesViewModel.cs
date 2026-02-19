using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Core.Validators;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

public class EmployeesViewModel : Screen
{
    private readonly IEmployeeService _employeeService;
    private readonly IUserSession _userSession;
    private readonly IDialogService _dialogService;
    private readonly EmployeeValidator _validator;
    
    private ObservableCollection<Employee> _employees = new();
    private Employee? _selectedEmployee;
    private Employee _editingEmployee = new();
    private bool _isEditing;
    private string _errorMessage = string.Empty;
    private string _transferBranch = string.Empty;

    public EmployeesViewModel(
        IEmployeeService employeeService, 
        IUserSession userSession, 
        IDialogService dialogService,
        EmployeeValidator validator)
    {
        _employeeService = employeeService;
        _userSession = userSession;
        _dialogService = dialogService;
        _validator = validator;
        DisplayName = "Employee Management";
    }

    public ObservableCollection<Employee> Employees
    {
        get => _employees;
        set
        {
            _employees = value;
            NotifyOfPropertyChange(() => Employees);
        }
    }

    public Employee? SelectedEmployee
    {
        get => _selectedEmployee;
        set
        {
            _selectedEmployee = value;
            NotifyOfPropertyChange(() => SelectedEmployee);
            NotifyOfPropertyChange(() => CanEdit);
            NotifyOfPropertyChange(() => CanDelete);
            NotifyOfPropertyChange(() => CanRestore);
            NotifyOfPropertyChange(() => CanExecuteTransferBranch);
        }
    }

    public Employee EditingEmployee
    {
        get => _editingEmployee;
        set
        {
            _editingEmployee = value;
            NotifyOfPropertyChange(() => EditingEmployee);
            NotifyOfPropertyChange(() => CanSave);
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
            NotifyOfPropertyChange(() => CanExecuteTransferBranch);
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

    public string TransferBranch
    {
        get => _transferBranch;
        set
        {
            _transferBranch = value;
            NotifyOfPropertyChange(() => TransferBranch);
            NotifyOfPropertyChange(() => CanExecuteTransferBranch);
        }
    }

    public ObservableCollection<string> AvailableBranches { get; } = new() { "BENTHANH", "TANDINH" };

    public bool CanAdd => !IsEditing;
    public bool CanEdit => SelectedEmployee != null && !IsEditing;
    public bool CanDelete => SelectedEmployee != null && !IsEditing && SelectedEmployee.TrangThaiXoa == 0;
    public bool CanRestore => SelectedEmployee != null && !IsEditing && SelectedEmployee.TrangThaiXoa == 1;
    public bool CanSave => IsEditing && !string.IsNullOrWhiteSpace(EditingEmployee.HO) && !string.IsNullOrWhiteSpace(EditingEmployee.TEN);
    public bool CanCancel => IsEditing;
    public bool CanExecuteTransferBranch => SelectedEmployee != null && !IsEditing && SelectedEmployee.TrangThaiXoa == 0 && !string.IsNullOrWhiteSpace(TransferBranch);

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        await LoadEmployeesAsync();
    }

    private async Task LoadEmployeesAsync()
    {
        try
        {
            List<Employee> employees;
            
            if (_userSession.UserGroup == UserGroup.NganHang)
            {
                employees = await _employeeService.GetAllEmployeesAsync();
            }
            else
            {
                employees = await _employeeService.GetEmployeesByBranchAsync(_userSession.SelectedBranch);
            }

            Employees = new ObservableCollection<Employee>(employees);
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading employees: {ex.Message}";
        }
    }

    public async Task Add()
    {
        EditingEmployee = new Employee
        {
            MANV = await _employeeService.GenerateEmployeeIdAsync(),
            MACN = _userSession.SelectedBranch,
            TrangThaiXoa = 0
        };
        IsEditing = true;
        SelectedEmployee = null;
        ErrorMessage = string.Empty;
    }

    public void Edit()
    {
        if (SelectedEmployee == null) return;

        EditingEmployee = new Employee
        {
            MANV = SelectedEmployee.MANV,
            HO = SelectedEmployee.HO,
            TEN = SelectedEmployee.TEN,
            DIACHI = SelectedEmployee.DIACHI,
            CMND = SelectedEmployee.CMND,
            PHAI = SelectedEmployee.PHAI,
            SODT = SelectedEmployee.SODT,
            MACN = SelectedEmployee.MACN,
            TrangThaiXoa = SelectedEmployee.TrangThaiXoa
        };
        IsEditing = true;
        ErrorMessage = string.Empty;
    }

    public async Task Save()
    {
        // Validate before saving
        var validationResult = await _validator.ValidateAsync(EditingEmployee);
        if (!validationResult.IsValid)
        {
            // Aggregate all validation errors
            ErrorMessage = string.Join(Environment.NewLine, 
                validationResult.Errors.Select(e => e.ErrorMessage));
            return;
        }

        try
        {
            bool result;

            if (SelectedEmployee == null)
            {
                // Guard: reject if MANV is already in use (covers manual edits after auto-generation)
                if (await _employeeService.EmployeeExistsAsync(EditingEmployee.MANV))
                {
                    ErrorMessage = $"Mã nhân viên '{EditingEmployee.MANV}' đã tồn tại. Vui lòng nhập mã khác.";
                    return;
                }
                result = await _employeeService.AddEmployeeAsync(EditingEmployee);
            }
            else
            {
                result = await _employeeService.UpdateEmployeeAsync(EditingEmployee);
            }

            if (result)
            {
                IsEditing = false;
                await LoadEmployeesAsync();
                SelectedEmployee = null;
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = "Failed to save employee.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving employee: {ex.Message}";
        }
    }

    public async Task Delete()
    {
        if (SelectedEmployee == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Are you sure you want to delete employee '{SelectedEmployee.FullName}'?",
            "Delete Confirmation"
        );

        if (!confirmed) return;

        try
        {
            var result = await _employeeService.DeleteEmployeeAsync(SelectedEmployee.MANV);
            if (result)
            {
                await LoadEmployeesAsync();
                SelectedEmployee = null;
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = "Failed to delete employee.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting employee: {ex.Message}";
        }
    }

    public async Task Restore()
    {
        if (SelectedEmployee == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Are you sure you want to restore employee '{SelectedEmployee.FullName}'?",
            "Restore Confirmation"
        );

        if (!confirmed) return;

        try
        {
            var result = await _employeeService.RestoreEmployeeAsync(SelectedEmployee.MANV);
            if (result)
            {
                await LoadEmployeesAsync();
                SelectedEmployee = null;
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = "Failed to restore employee.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error restoring employee: {ex.Message}";
        }
    }

    public void Cancel()
    {
        IsEditing = false;
        EditingEmployee = new Employee();
        ErrorMessage = string.Empty;
    }

    public async Task ExecuteTransferBranch()
    {
        if (SelectedEmployee == null || string.IsNullOrWhiteSpace(TransferBranch)) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Are you sure you want to transfer employee '{SelectedEmployee.FullName}' to branch '{TransferBranch}'?",
            "Transfer Confirmation"
        );

        if (!confirmed) return;

        try
        {
            var result = await _employeeService.TransferEmployeeAsync(SelectedEmployee.MANV, TransferBranch);
            if (result)
            {
                await LoadEmployeesAsync();
                TransferBranch = string.Empty;
                SelectedEmployee = null;
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = "Failed to transfer employee.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error transferring employee: {ex.Message}";
        }
    }
}
