using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels
{
    public class CustomerLookupViewModel : Screen
    {
        private readonly ICustomerLookupService _customerLookupService;
        private readonly IDialogService _dialogService;

        public CustomerLookupViewModel(ICustomerLookupService customerLookupService, IDialogService dialogService)
        {
            _customerLookupService = customerLookupService;
            _dialogService = dialogService;
            DisplayName = "Tra cứu khách hàng";
        }
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(() => CanSearch);
            }
        }
        private bool _searchByCmnd = true;
        public bool SearchByCmnd
        {
            get => _searchByCmnd;
            set
            {
                _searchByCmnd = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(() => SearchByName);
                NotifyOfPropertyChange(() => SearchPlaceholder);
            }
        }
        public bool SearchByName
        {
            get => !_searchByCmnd;
            set => SearchByCmnd = !value;
        }
        public string SearchPlaceholder => SearchByCmnd
            ? "Nhập CMND (ví dụ: 0123456789)"
            : "Nhập tên hoặc họ (ví dụ: Nguyễn Văn)";
        public ObservableCollection<Customer> Results { get; } = new();
        private Customer? _selectedCustomer;
        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                NotifyOfPropertyChange();
            }
        }
        private bool _isSearching;
        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                _isSearching = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(() => CanSearch);
            }
        }
        private string _statusMessage = "Nhập CMND hoặc tên khách hàng để tra cứu toàn hệ thống.";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                NotifyOfPropertyChange();
            }
        }
        public bool CanSearch => !IsSearching && !string.IsNullOrWhiteSpace(SearchText);

        public async Task Search()
        {
            if (!CanSearch) return;
            IsSearching = true;
            StatusMessage = "Đang tìm kiếm...";
            Results.Clear();
            SelectedCustomer = null;
            try
            {
                if (SearchByCmnd)
                {
                    var customer = await _customerLookupService.GetCustomerByCmndAsync(SearchText.Trim());
                    if (customer != null)
                    {
                        Results.Add(customer);
                        StatusMessage = $"Tìm thấy 1 khách hàng (CMND: {customer.CMND}).";
                    }
                    else
                    {
                        StatusMessage = $"Không tìm thấy khách hàng với CMND '{SearchText.Trim()}'.";
                    }
                }
                else
                {
                    var list = await _customerLookupService.SearchCustomersByNameAsync(SearchText.Trim());
                    foreach (var c in list)
                        Results.Add(c);
                    StatusMessage = list.Count > 0
                        ? $"Tìm thấy {list.Count} khách hàng."
                        : $"Không tìm thấy khách hàng có tên chứa '{SearchText.Trim()}'.";
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                await _dialogService.ShowErrorAsync(ex.Message, "Không đủ quyền");
                StatusMessage = "Lỗi: không đủ quyền truy cập tra cứu.";
            }
            catch (InvalidOperationException ex)
            {
                await _dialogService.ShowErrorAsync(ex.Message, "Lỗi kết nối");
                StatusMessage = "Lỗi kết nối đến cơ sở dữ liệu tra cứu.";
            }
            finally
            {
                IsSearching = false;
            }
        }
    }
}