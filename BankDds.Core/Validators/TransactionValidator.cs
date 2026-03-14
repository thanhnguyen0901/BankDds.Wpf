using FluentValidation;

namespace BankDds.Core.Validators;

public class TransactionValidator : AbstractValidator<Models.Transaction>
{
    public TransactionValidator()
    {
        // Transaction ID validation
        RuleFor(x => x.MAGD)
            .NotEmpty().WithMessage("Mã giao dịch (MAGD) là bắt buộc.");

        // Account number validation - must be exactly 9 characters
        RuleFor(x => x.SOTK)
            .NotEmpty().WithMessage("Số tài khoản (SOTK) là bắt buộc.")
            .Length(9).WithMessage("Số tài khoản phải đúng 9 ký tự.");

        // Transaction type validation
        RuleFor(x => x.LOAIGD)
            .NotEmpty().WithMessage("Loại giao dịch (LOAIGD) là bắt buộc.")
            .Must(x => x == "GT" || x == "RT" || x == "CT")
            .WithMessage("Loại giao dịch phải là 'GT' (Gửi tiền), 'RT' (Rút tiền) hoặc 'CT' (Chuyển tiền).");

        // Amount validation: Must be >= 100,000 VND (implies > 0)
        RuleFor(x => x.SOTIEN)
            .GreaterThanOrEqualTo(100000).WithMessage("Số tiền giao dịch tối thiểu là 100.000 VND.");

        // Transaction date validation: Cannot be in the future
        RuleFor(x => x.NGAYGD)
            .LessThanOrEqualTo(DateTime.Now).WithMessage("Ngày giao dịch không được lớn hơn ngày hiện tại.");

        // Employee ID validation - must be exactly 10 characters
        RuleFor(x => x.MANV)
            .NotEmpty().WithMessage("Mã nhân viên (MANV) là bắt buộc.")
            .Length(10).WithMessage("Mã nhân viên phải đúng 10 ký tự.");

        // Receiving account validation for transfers - must be exactly 9 characters
        // GAP-03: When guard uses only CT; CK alias removed.
        RuleFor(x => x.SOTK_NHAN)
            .NotEmpty().WithMessage("Tài khoản nhận (SOTK_NHAN) là bắt buộc khi chuyển tiền.")
            .Length(9).WithMessage("Tài khoản nhận phải đúng 9 ký tự.")
            .When(x => x.LOAIGD == "CT");
    }
}

