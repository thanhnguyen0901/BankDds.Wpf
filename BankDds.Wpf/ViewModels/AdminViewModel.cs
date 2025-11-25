using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

public class AdminViewModel : Screen
{
    private readonly IUserService _userService;
    
    private ObservableCollection<User> _users = new();
    private User? _selectedUser;

    public AdminViewModel(IUserService userService)
    {
        _userService = userService;
        DisplayName = "User Administration";
    }

    public ObservableCollection<User> Users
    {
        get => _users;
        set
        {
            _users = value;
            NotifyOfPropertyChange(() => Users);
        }
    }

    public User? SelectedUser
    {
        get => _selectedUser;
        set
        {
            _selectedUser = value;
            NotifyOfPropertyChange(() => SelectedUser);
        }
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        var users = await _userService.GetAllUsersAsync();
        Users = new ObservableCollection<User>(users);
    }
}
