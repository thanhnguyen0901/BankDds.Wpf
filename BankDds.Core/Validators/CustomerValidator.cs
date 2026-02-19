using FluentValidation;
using BankDds.Core.Interfaces;

namespace BankDds.Core.Validators;

public class CustomerValidator : AbstractValidator<Models.Customer>
{
    public CustomerValidator(IBranchRepository branchRepository)
    {
        // CMND validation: Required, exactly 10 digits (nChar(10) in DB — GAP-08)
        RuleFor(x => x.CMND)
            .NotEmpty().WithMessage("CMND is required")
            .Length(10).WithMessage("CMND must be exactly 10 digits")
            .Matches(@"^\d+$").WithMessage("CMND must contain only numeric digits");

        // Last name validation
        RuleFor(x => x.Ho)
            .NotEmpty().WithMessage("Last name (Ho) is required")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters");

        // First name validation (nvarchar(10) in DB — GAP-09)
        RuleFor(x => x.Ten)
            .NotEmpty().WithMessage("First name (Ten) is required")
            .MaximumLength(10).WithMessage("First name cannot exceed 10 characters");

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
        RuleFor(x => x.SODT)
            .Matches(@"^(0\d{9,10})$").WithMessage("Phone number must be 10-11 digits and start with 0")
            .When(x => !string.IsNullOrEmpty(x.SODT));

        // Gender validation: Must be "Nam" or "Nữ" (GAP-10: diacritic required per DE3 §I.2)
        RuleFor(x => x.Phai)
            .NotEmpty().WithMessage("Gender (Phai) is required")
            .Must(x => x == "Nam" || x == "Nữ").WithMessage("Gender must be 'Nam' or 'Nữ'");

        // Address validation: REQUIRED, limited length (nvarchar(100) in DB — GAP-09)
        RuleFor(x => x.DiaChi)
            .NotEmpty().WithMessage("Address (DiaChi) is required")
            .MaximumLength(100).WithMessage("Address cannot exceed 100 characters");

        // Branch code validation: required and must exist in the branch repository
        RuleFor(x => x.MaCN)
            .NotEmpty().WithMessage("Branch code (MaCN) is required")
            .MustAsync((macn, ct) => branchRepository.BranchExistsAsync(macn))
            .WithMessage("Branch code does not match any registered branch");
    }
}
