using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

public class EmployeesViewModel : Screen
{
    private readonly IEmployeeService _employeeService;
    private readonly IUserSession _userSession;
    
    private ObservableCollection<Employee> _employees = new();
    private Employee? _selectedEmployee;

    public EmployeesViewModel(IEmployeeService employeeService, IUserSession userSession)
    {
        _employeeService = employeeService;
        _userSession = userSession;
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
        }
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        await LoadEmployeesAsync();
    }

    private async Task LoadEmployeesAsync()
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
    }
}
