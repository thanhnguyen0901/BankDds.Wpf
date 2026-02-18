using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Core.Validators;
using Caliburn.Micro;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

public class BranchesViewModel : BaseViewModel
{
    private readonly IBranchService _branchService;
    private readonly IUserSession _userSession;
    private readonly BranchValidator _validator;

    private ObservableCollection<Branch> _branches = new();
    private Branch? _selectedBranch;
    private Branch _editingBranch = new();
    private bool _isEditing;

    public BranchesViewModel(
        IBranchService branchService,
        IUserSession userSession,
        BranchValidator validator)
    {
        _branchService = branchService;
        _userSession   = userSession;
        _validator     = validator;
        DisplayName    = "Branch Management";
    }

    public ObservableCollection<Branch> Branches
    {
        get => _branches;
        set { _branches = value; NotifyOfPropertyChange(() => Branches); }
    }

    public Branch? SelectedBranch
    {
        get => _selectedBranch;
        set
        {
            _selectedBranch = value;
            NotifyOfPropertyChange(() => SelectedBranch);
            NotifyOfPropertyChange(() => CanEdit);
            NotifyOfPropertyChange(() => CanDelete);
        }
    }

    public Branch EditingBranch
    {
        get => _editingBranch;
        set
        {
            _editingBranch = value;
            NotifyOfPropertyChange(() => EditingBranch);
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
            NotifyOfPropertyChange(() => CanSave);
            NotifyOfPropertyChange(() => CanCancel);
        }
    }

    public bool CanAdd    => !IsEditing;
    public bool CanEdit   => SelectedBranch != null && !IsEditing;
    public bool CanDelete => SelectedBranch != null && !IsEditing;
    public bool CanSave   => IsEditing
                             && !string.IsNullOrWhiteSpace(EditingBranch.MACN)
                             && !string.IsNullOrWhiteSpace(EditingBranch.TENCN);
    public bool CanCancel => IsEditing;

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);

        if (_userSession.UserGroup != UserGroup.NganHang)
        {
            ErrorMessage = "Access Denied: Only Bank-level administrators can manage branches.";
            return;
        }

        await LoadBranchesAsync();
    }

    private async Task LoadBranchesAsync()
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            var branches = await _branchService.GetAllBranchesAsync();
            Branches = new ObservableCollection<Branch>(branches);
        });
    }

    public void Add()
    {
        EditingBranch  = new Branch();
        IsEditing      = true;
        SelectedBranch = null;
        ErrorMessage   = string.Empty;
        SuccessMessage = string.Empty;
    }

    public void Edit()
    {
        if (SelectedBranch == null) return;

        EditingBranch = new Branch
        {
            MACN   = SelectedBranch.MACN,
            TENCN  = SelectedBranch.TENCN,
            DiaChi = SelectedBranch.DiaChi,
            SoDT   = SelectedBranch.SoDT
        };
        IsEditing      = true;
        ErrorMessage   = string.Empty;
        SuccessMessage = string.Empty;
    }

    public async Task Save()
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            var validationResult = await _validator.ValidateAsync(EditingBranch);
            if (!validationResult.IsValid)
            {
                ErrorMessage = string.Join(Environment.NewLine,
                    validationResult.Errors.Select(e => e.ErrorMessage));
                return;
            }

            bool result;

            if (SelectedBranch == null)
            {
                // Adding new branch
                result = await _branchService.AddBranchAsync(EditingBranch);
                if (result)
                    SuccessMessage = $"Branch '{EditingBranch.MACN}' added successfully.";
                else
                    ErrorMessage = $"Failed to add branch — code '{EditingBranch.MACN}' may already exist.";
            }
            else
            {
                // Updating existing branch (MACN is the PK, cannot be changed here)
                result = await _branchService.UpdateBranchAsync(EditingBranch);
                if (result)
                    SuccessMessage = $"Branch '{EditingBranch.MACN}' updated successfully.";
                else
                    ErrorMessage = "Failed to update branch.";
            }

            if (result)
            {
                await LoadBranchesAsync();
                Cancel();
            }
        });
    }

    public async Task Delete()
    {
        if (SelectedBranch == null) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _branchService.DeleteBranchAsync(SelectedBranch.MACN);
            if (result)
            {
                await LoadBranchesAsync();
                SelectedBranch = null;
                SuccessMessage = "Branch deleted successfully.";
            }
            else
            {
                ErrorMessage = "Failed to delete branch — it may have linked accounts or customers.";
            }
        });
    }

    public void Cancel()
    {
        IsEditing      = false;
        EditingBranch  = new Branch();
        ErrorMessage   = string.Empty;
        SuccessMessage = string.Empty;
    }
}
