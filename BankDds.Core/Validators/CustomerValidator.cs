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

        // Birth date validation: Required, must be in past, reasonable age range
        RuleFor(x => x.NgaySinh)
            .NotNull().WithMessage("Date of birth (NgaySinh) is required")
            .LessThan(DateTime.Today).WithMessage("Date of birth must be in the past")
            .GreaterThan(DateTime.Today.AddYears(-150)).WithMessage("Date of birth must be within last 150 years");

        // ID card issue date: always required
        RuleFor(x => x.NgayCap)
            .NotNull().WithMessage("ID card issue date (NgayCap) is required");

        // ID card issue date: range checks only when a value is present
        RuleFor(x => x.NgayCap)
            .LessThanOrEqualTo(DateTime.Today).WithMessage("ID card issue date cannot be in the future")
            .When(x => x.NgayCap.HasValue);

        // ID card must be after date of birth (only when both dates are provided)
        RuleFor(x => x.NgayCap)
            .GreaterThan(x => x.NgaySinh ?? DateTime.MinValue).WithMessage("ID card issue date must be after date of birth")
            .When(x => x.NgayCap.HasValue && x.NgaySinh.HasValue);

        // Phone validation: Optional, but if provided must be 10-11 digits starting with 0
        RuleFor(x => x.SDT)
            .Matches(@"^(0\d{9,10})$").WithMessage("Phone number must be 10-11 digits and start with 0")
            .When(x => !string.IsNullOrEmpty(x.SDT));

        // Gender validation: Must be "Nam" or "Nu"
        RuleFor(x => x.Phai)
            .NotEmpty().WithMessage("Gender (Phai) is required")
            .Must(x => x == "Nam" || x == "Nu").WithMessage("Gender must be 'Nam' or 'Nu'");

        // Address validation: REQUIRED, limited length
        RuleFor(x => x.DiaChi)
            .NotEmpty().WithMessage("Address (DiaChi) is required")
            .MaximumLength(200).WithMessage("Address cannot exceed 200 characters");

        // Branch code validation
        RuleFor(x => x.MaCN)
            .NotEmpty().WithMessage("Branch code (MaCN) is required");
    }
}
