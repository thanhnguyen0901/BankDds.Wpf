using FluentValidation;

namespace BankDds.Core.Validators;

public class EmployeeValidator : AbstractValidator<Models.Employee>
{
    public EmployeeValidator()
    {
        // Employee ID validation - must be exactly 10 characters (nChar(10))
        RuleFor(x => x.MANV)
            .NotEmpty().WithMessage("Employee ID (MANV) is required")
            .Length(10).WithMessage("Employee ID must be exactly 10 characters");

        // Last name validation
        RuleFor(x => x.HO)
            .NotEmpty().WithMessage("Last name (HO) is required")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters");

        // First name validation
        RuleFor(x => x.TEN)
            .NotEmpty().WithMessage("First name (TEN) is required")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters");

        // CMND validation: 9-12 digits, numeric only
        RuleFor(x => x.CMND)
            .NotEmpty().WithMessage("CMND is required")
            .Length(9, 12).WithMessage("CMND must be 9-12 digits")
            .Matches(@"^\d+$").WithMessage("CMND must contain only numeric digits");

        // Phone validation: Optional, but if provided must be 10-11 digits starting with 0
        RuleFor(x => x.SDT)
            .Matches(@"^(0\d{9,10})$").WithMessage("Phone number must be 10-11 digits and start with 0")
            .When(x => !string.IsNullOrEmpty(x.SDT));

        // Gender validation: Must be "Nam" or "Nu"
        RuleFor(x => x.PHAI)
            .NotEmpty().WithMessage("Gender (PHAI) is required")
            .Must(x => x == "Nam" || x == "Nu").WithMessage("Gender must be 'Nam' or 'Nu'");

        // Address validation: REQUIRED, limited length
        RuleFor(x => x.DIACHI)
            .NotEmpty().WithMessage("Address (DIACHI) is required")
            .MaximumLength(200).WithMessage("Address cannot exceed 200 characters");

        // Branch code validation
        RuleFor(x => x.MACN)
            .NotEmpty().WithMessage("Branch code (MACN) is required");
    }
}
