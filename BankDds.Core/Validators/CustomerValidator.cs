using FluentValidation;

namespace BankDds.Core.Validators;

public class CustomerValidator : AbstractValidator<Models.Customer>
{
    public CustomerValidator()
    {
        // CMND validation: Required, 9-12 digits, numeric only
        RuleFor(x => x.CMND)
            .NotEmpty().WithMessage("CMND is required")
            .Length(9, 12).WithMessage("CMND must be 9-12 digits")
            .Matches(@"^\d+$").WithMessage("CMND must contain only numeric digits");

        // Last name validation
        RuleFor(x => x.Ho)
            .NotEmpty().WithMessage("Last name (Ho) is required")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters");

        // First name validation
        RuleFor(x => x.Ten)
            .NotEmpty().WithMessage("First name (Ten) is required")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters");

        // Phone validation: Optional, but if provided must be 10-11 digits starting with 0
        RuleFor(x => x.SDT)
            .Matches(@"^(0\d{9,10})$").WithMessage("Phone number must be 10-11 digits and start with 0")
            .When(x => !string.IsNullOrEmpty(x.SDT));

        // Gender validation: Must be "Nam" or "Nu"
        RuleFor(x => x.Phai)
            .NotEmpty().WithMessage("Gender (Phai) is required")
            .Must(x => x == "Nam" || x == "Nu").WithMessage("Gender must be 'Nam' or 'Nu'");

        // Address validation: Optional but limited length
        RuleFor(x => x.DiaChi)
            .MaximumLength(200).WithMessage("Address cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.DiaChi));

        // Branch code validation
        RuleFor(x => x.MaCN)
            .NotEmpty().WithMessage("Branch code (MaCN) is required");
    }
}
