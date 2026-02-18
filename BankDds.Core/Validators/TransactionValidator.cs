using FluentValidation;

namespace BankDds.Core.Validators;

public class TransactionValidator : AbstractValidator<Models.Transaction>
{
    public TransactionValidator()
    {
        // Transaction ID validation
        RuleFor(x => x.MAGD)
            .NotEmpty().WithMessage("Transaction ID (MAGD) is required");

        // Account number validation
        RuleFor(x => x.SOTK)
            .NotEmpty().WithMessage("Account number (SOTK) is required");

        // Transaction type validation
        RuleFor(x => x.LOAIGD)
            .NotEmpty().WithMessage("Transaction type (LOAIGD) is required")
            .Must(x => x == "GT" || x == "RT" || x == "CK")
            .WithMessage("Transaction type must be 'GT' (deposit), 'RT' (withdrawal), or 'CK' (transfer)");

        // Amount validation: Must be > 0
        RuleFor(x => x.SOTIEN)
            .GreaterThan(0).WithMessage("Transaction amount must be greater than 0")
            .GreaterThanOrEqualTo(100000).WithMessage("Minimum transaction amount is 100,000 VND");

        // Transaction date validation: Cannot be in the future
        RuleFor(x => x.NGAYGD)
            .LessThanOrEqualTo(DateTime.Now).WithMessage("Transaction date cannot be in the future");

        // Employee ID validation
        RuleFor(x => x.MANV)
            .GreaterThan(0).WithMessage("Employee ID (MANV) must be valid");

        // Receiving account validation for transfers
        RuleFor(x => x.SOTK_NHAN)
            .NotEmpty().WithMessage("Receiving account (SOTK_NHAN) is required for transfers")
            .When(x => x.LOAIGD == "CK");
    }
}
