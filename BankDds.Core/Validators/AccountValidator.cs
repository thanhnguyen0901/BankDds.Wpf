using FluentValidation;

namespace BankDds.Core.Validators;

public class AccountValidator : AbstractValidator<Models.Account>
{
    public AccountValidator()
    {
        // Account number validation
        RuleFor(x => x.SOTK)
            .NotEmpty().WithMessage("Account number (SOTK) is required");

        // Customer CMND validation
        RuleFor(x => x.CMND)
            .NotEmpty().WithMessage("Customer CMND is required")
            .Length(9, 12).WithMessage("CMND must be 9-12 digits");

        // Balance validation: Must be >= 0
        RuleFor(x => x.SODU)
            .GreaterThanOrEqualTo(0).WithMessage("Balance (SODU) cannot be negative");

        // Branch code validation
        RuleFor(x => x.MACN)
            .NotEmpty().WithMessage("Branch code (MACN) is required");

        // Opening date validation: Cannot be in the future
        RuleFor(x => x.NGAYMOTK)
            .LessThanOrEqualTo(DateTime.Now).WithMessage("Opening date cannot be in the future");
    }
}
