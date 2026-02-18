using FluentValidation;

namespace BankDds.Core.Validators;

public class AccountValidator : AbstractValidator<Models.Account>
{
    public AccountValidator()
    {
        // Account number validation - must be exactly 9 characters (nChar(9))
        RuleFor(x => x.SOTK)
            .NotEmpty().WithMessage("Account number (SOTK) is required")
            .Length(9).WithMessage("Account number must be exactly 9 characters")
            .Matches(@"^[A-Z0-9]+$").WithMessage("Account number must contain only uppercase letters and digits");

        // Customer CMND validation - 9-12 numeric digits
        RuleFor(x => x.CMND)
            .NotEmpty().WithMessage("Customer CMND is required")
            .Length(9, 12).WithMessage("CMND must be 9-12 digits")
            .Matches(@"^\d+$").WithMessage("CMND must contain only numeric digits");

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
