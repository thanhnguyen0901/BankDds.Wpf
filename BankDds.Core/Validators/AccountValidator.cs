using FluentValidation;
using BankDds.Core.Interfaces;

namespace BankDds.Core.Validators;

public class AccountValidator : AbstractValidator<Models.Account>
{
    public AccountValidator(IBranchRepository branchRepository)
    {
        // Account number validation - must be exactly 9 characters (nChar(9))
        RuleFor(x => x.SOTK)
            .NotEmpty().WithMessage("Account number (SOTK) is required")
            .Length(9).WithMessage("Account number must be exactly 9 characters")
            .Matches(@"^[A-Z0-9]+$").WithMessage("Account number must contain only uppercase letters and digits");

        // Customer CMND validation: exactly 10 digits (nChar(10) in DB — GAP-08)
        RuleFor(x => x.CMND)
            .NotEmpty().WithMessage("Customer CMND is required")
            .Length(10).WithMessage("CMND must be exactly 10 digits")
            .Matches(@"^\d+$").WithMessage("CMND must contain only numeric digits");

        // Balance validation: Must be >= 0
        RuleFor(x => x.SODU)
            .GreaterThanOrEqualTo(0).WithMessage("Balance (SODU) cannot be negative");

        // Branch code validation: required and must exist in the branch repository
        RuleFor(x => x.MACN)
            .NotEmpty().WithMessage("Branch code (MACN) is required")
            .MustAsync((macn, ct) => branchRepository.BranchExistsAsync(macn))
            .WithMessage("Branch code does not match any registered branch");

        // Opening date validation: Cannot be in the future
        RuleFor(x => x.NGAYMOTK)
            .LessThanOrEqualTo(DateTime.Now).WithMessage("Opening date cannot be in the future");
    }
}
