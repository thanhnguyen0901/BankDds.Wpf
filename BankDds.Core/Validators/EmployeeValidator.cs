using FluentValidation;
using BankDds.Core.Interfaces;

namespace BankDds.Core.Validators;

public class EmployeeValidator : AbstractValidator<Models.Employee>
{
    public EmployeeValidator(IBranchRepository branchRepository)
    {
        // Employee ID validation - must be exactly 10 characters (nChar(10))
        RuleFor(x => x.MANV)
            .NotEmpty().WithMessage("Employee ID (MANV) is required")
            .Length(10).WithMessage("Employee ID must be exactly 10 characters");

        // Last name validation
        RuleFor(x => x.HO)
            .NotEmpty().WithMessage("Last name (HO) is required")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters");

        // First name validation (nvarchar(10) in DB — GAP-09)
        RuleFor(x => x.TEN)
            .NotEmpty().WithMessage("First name (TEN) is required")
            .MaximumLength(10).WithMessage("First name cannot exceed 10 characters");

        // CMND validation: exactly 10 digits (nChar(10) in DB — GAP-08)
        RuleFor(x => x.CMND)
            .NotEmpty().WithMessage("CMND is required")
            .Length(10).WithMessage("CMND must be exactly 10 digits")
            .Matches(@"^\d+$").WithMessage("CMND must contain only numeric digits");

        // Phone validation: Optional, but if provided must be 10-11 digits starting with 0
        RuleFor(x => x.SODT)
            .Matches(@"^(0\d{9,10})$").WithMessage("Phone number must be 10-11 digits and start with 0")
            .When(x => !string.IsNullOrEmpty(x.SODT));

        // Gender validation: Must be "Nam" or "Nữ" (GAP-10: diacritic required per DE3 §I.3)
        RuleFor(x => x.PHAI)
            .NotEmpty().WithMessage("Gender (PHAI) is required")
            .Must(x => x == "Nam" || x == "Nữ").WithMessage("Gender must be 'Nam' or 'Nữ'");

        // Address validation: REQUIRED, limited length (nvarchar(100) in DB — GAP-09)
        RuleFor(x => x.DIACHI)
            .NotEmpty().WithMessage("Address (DIACHI) is required")
            .MaximumLength(100).WithMessage("Address cannot exceed 100 characters");

        // Branch code validation: required and must exist in the branch repository
        RuleFor(x => x.MACN)
            .NotEmpty().WithMessage("Branch code (MACN) is required")
            .MustAsync((macn, ct) => branchRepository.BranchExistsAsync(macn))
            .WithMessage("Branch code does not match any registered branch");
    }
}
