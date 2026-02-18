using FluentValidation;

namespace BankDds.Core.Validators;

public class TransactionValidator : AbstractValidator<Models.Transaction>
{
    public TransactionValidator()
    {
        // Transaction ID validation
        RuleFor(x => x.MAGD)
            .NotEmpty().WithMessage("Transaction ID (MAGD) is required");

        // Account number validation - must be exactly 9 characters
        RuleFor(x => x.SOTK)
            .NotEmpty().WithMessage("Account number (SOTK) is required")
            .Length(9).WithMessage("Account number must be exactly 9 characters");

        // Transaction type validation
        // GAP-03: only GT / RT / CT are valid per DE3 §I.5–§I.6; CK was a legacy alias now removed.
        RuleFor(x => x.LOAIGD)
            .NotEmpty().WithMessage("Transaction type (LOAIGD) is required")
            .Must(x => x == "GT" || x == "RT" || x == "CT")
            .WithMessage("Transaction type must be 'GT' (Gửi tiền), 'RT' (Rút tiền), or 'CT' (Chuyển tiền)");

        // Amount validation: Must be >= 100,000 VND (implies > 0)
        RuleFor(x => x.SOTIEN)
            .GreaterThanOrEqualTo(100000).WithMessage("Minimum transaction amount is 100,000 VND");

        // Transaction date validation: Cannot be in the future
        RuleFor(x => x.NGAYGD)
            .LessThanOrEqualTo(DateTime.Now).WithMessage("Transaction date cannot be in the future");

        // Employee ID validation - must be exactly 10 characters
        RuleFor(x => x.MANV)
            .NotEmpty().WithMessage("Employee ID (MANV) is required")
            .Length(10).WithMessage("Employee ID must be exactly 10 characters");

        // Receiving account validation for transfers - must be exactly 9 characters
        // GAP-03: When guard uses only CT; CK alias removed.
        RuleFor(x => x.SOTK_NHAN)
            .NotEmpty().WithMessage("Receiving account (SOTK_NHAN) is required for transfers")
            .Length(9).WithMessage("Receiving account must be exactly 9 characters")
            .When(x => x.LOAIGD == "CT");
    }
}
