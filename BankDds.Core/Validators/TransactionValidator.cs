using FluentValidation;

namespace BankDds.Core.Validators
{
    public class TransactionValidator : AbstractValidator<Models.Transaction>
    {
        public TransactionValidator()
        {
            RuleFor(x => x.MAGD)
                .NotEmpty().WithMessage("Mã giao dịch (MAGD) là bắt buộc.");
            RuleFor(x => x.SOTK)
                .NotEmpty().WithMessage("Số tài khoản (SOTK) là bắt buộc.")
                .Length(9).WithMessage("Số tài khoản phải đúng 9 ký tự.");
            RuleFor(x => x.LOAIGD)
                .NotEmpty().WithMessage("Loại giao dịch (LOAIGD) là bắt buộc.")
                .Must(x => x == "GT" || x == "RT" || x == "CT")
                .WithMessage("Loại giao dịch phải là 'GT' (Gửi tiền), 'RT' (Rút tiền) hoặc 'CT' (Chuyển tiền).");
            RuleFor(x => x.SOTIEN)
                .GreaterThanOrEqualTo(100000).WithMessage("Số tiền giao dịch tối thiểu là 100.000 VND.");
            RuleFor(x => x.NGAYGD)
                .LessThanOrEqualTo(DateTime.Now).WithMessage("Ngày giao dịch không được lớn hơn ngày hiện tại.");
            RuleFor(x => x.MANV)
                .NotEmpty().WithMessage("Mã nhân viên (MANV) là bắt buộc.")
                .Length(10).WithMessage("Mã nhân viên phải đúng 10 ký tự.");
            RuleFor(x => x.SOTK_NHAN)
                .NotEmpty().WithMessage("Tài khoản nhận (SOTK_NHAN) là bắt buộc khi chuyển tiền.")
                .Length(9).WithMessage("Tài khoản nhận phải đúng 9 ký tự.")
                .When(x => x.LOAIGD == "CT");
        }
    }
}