using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Core.Validators;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels
{
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
            DisplayName = "Quản lý nhân viên";
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
        private bool CanModifyEmployees => _userSession.UserGroup == UserGroup.ChiNhanh;
        public bool CanAdd => CanModifyEmployees && !IsEditing;
        public bool CanEdit => CanModifyEmployees && SelectedEmployee != null && !IsEditing;
        public bool CanDelete => CanModifyEmployees && SelectedEmployee != null && !IsEditing && SelectedEmployee.TrangThaiXoa == 0;
        public bool CanRestore => CanModifyEmployees && SelectedEmployee != null && !IsEditing && SelectedEmployee.TrangThaiXoa == 1;
        public bool CanSave => IsEditing && !string.IsNullOrWhiteSpace(EditingEmployee.HO) && !string.IsNullOrWhiteSpace(EditingEmployee.TEN);
        public bool CanCancel => IsEditing;
        public bool CanExecuteTransferBranch => CanModifyEmployees && SelectedEmployee != null && !IsEditing && SelectedEmployee.TrangThaiXoa == 0 && !string.IsNullOrWhiteSpace(TransferBranch);

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
                ErrorMessage = $"Lỗi tải danh sách nhân viên: {ex.Message}";
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
            var validationResult = await _validator.ValidateAsync(EditingEmployee);
            if (!validationResult.IsValid)
            {
                ErrorMessage = string.Join(Environment.NewLine,
                    validationResult.Errors.Select(e => e.ErrorMessage));
                return;
            }
            try
            {
                bool result;
                if (SelectedEmployee == null)
                {
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
                    ErrorMessage = "Không thể lưu nhân viên.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi lưu nhân viên: {ex.Message}";
            }
        }

        public async Task Delete()
        {
            if (SelectedEmployee == null) return;
            var confirmed = await _dialogService.ShowConfirmationAsync(
                $"Bạn có chắc muốn xóa nhân viên '{SelectedEmployee.FullName}'?",
                "Xác nhận xóa"
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
                    ErrorMessage = "Không thể xóa nhân viên.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi xóa nhân viên: {ex.Message}";
            }
        }

        public async Task Restore()
        {
            if (SelectedEmployee == null) return;
            var confirmed = await _dialogService.ShowConfirmationAsync(
                $"Bạn có chắc muốn khôi phục nhân viên '{SelectedEmployee.FullName}'?",
                "Xác nhận khôi phục"
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
                    ErrorMessage = "Không thể khôi phục nhân viên.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi khôi phục nhân viên: {ex.Message}";
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
                $"Bạn có chắc muốn chuyển nhân viên '{SelectedEmployee.FullName}' sang chi nhánh '{TransferBranch}'?",
                "Xác nhận chuyển chi nhánh"
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
                    ErrorMessage = "Không thể chuyển nhân viên.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi chuyển nhân viên: {ex.Message}";
            }
        }
    }
}