namespace BankDds.Core.Formatting
{
    /// <summary>
    /// Provides consistent Vietnamese display text for shared business codes.
    /// </summary>
    public static class DisplayText
    {
        public static string Branch(string? branchCode)
        {
            var normalized = NormalizeBranchCode(branchCode);

            return normalized switch
            {
                "" => string.Empty,
                "ALL" => "Tất cả chi nhánh",
                "BENTHANH" => "Bến Thành",
                "TANDINH" => "Tân Định",
                _ => normalized
            };
        }

        public static string SoftDeleteStatus(int deletedFlag) =>
            deletedFlag == 0 ? "Hoạt động" : "Đã xóa";

        public static string AccountStatus(string? statusCode)
        {
            var normalized = (statusCode ?? string.Empty).Trim();

            return normalized switch
            {
                "Active" => "Hoạt động",
                "Closed" => "Đã đóng",
                _ => normalized
            };
        }

        public static string TransactionStatus(string? statusCode)
        {
            var normalized = (statusCode ?? string.Empty).Trim();

            return normalized switch
            {
                "Completed" => "Thành công",
                "Failed" => "Thất bại",
                "Pending" => "Đang xử lý",
                _ => normalized
            };
        }

        public static string NormalizeBranchCode(string? branchCode) =>
            (branchCode ?? string.Empty).Trim().ToUpperInvariant();
    }
}
