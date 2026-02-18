using BankDds.Core.Models;
using FluentValidation;

namespace BankDds.Core.Validators;

public class BranchValidator : AbstractValidator<Branch>
{
    public BranchValidator()
    {
        // MACN: required, max 10 chars, uppercase alphanumeric (nChar(10) in DB)
        RuleFor(x => x.MACN)
            .NotEmpty().WithMessage("Branch code (MACN) is required")
            .MaximumLength(10).WithMessage("Branch code cannot exceed 10 characters")
            .Matches(@"^[A-Z0-9]+$").WithMessage("Branch code must use uppercase letters or digits only");

        // TENCN: required, max 50 chars
        RuleFor(x => x.TENCN)
            .NotEmpty().WithMessage("Branch name (TENCN) is required")
            .MaximumLength(50).WithMessage("Branch name cannot exceed 50 characters");

        // DiaChi: optional, max 100 chars
        RuleFor(x => x.DiaChi)
            .MaximumLength(100).WithMessage("Address cannot exceed 100 characters");

        // SoDT: optional; if provided must start with 0 and be 10–15 digits
        RuleFor(x => x.SoDT)
            .MaximumLength(15).WithMessage("Phone number cannot exceed 15 characters")
            .Matches(@"^0\d{9,14}$").WithMessage("Phone number must start with 0 and be 10–15 digits")
            .When(x => !string.IsNullOrEmpty(x.SoDT));
    }
}
