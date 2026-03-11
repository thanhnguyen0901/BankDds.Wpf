# Giai đoạn F - Biên Bản Acceptance PASS/FAIL

Ngày tạo: 2026-03-11  
Phạm vi: kiểm thử chấp nhận cho các mục `F1..F7` trong kế hoạch chuyển đổi UI-first.

## 1) Trạng thái hiện tại

- `Repo scope`: đã hoàn tất rà soát code + chuẩn hóa tài liệu/test script.
- `Live SQL/App`: chưa chạy trong phiên này (cần môi trường SSMS + replication/link thực tế).
- Kết luận tạm thời: **READY FOR EXECUTION** (chờ chạy test live để chốt PASS/FAIL cuối).

## 2) Điều kiện bắt buộc trước khi test F

1. Hoàn tất B2/B3/B4 theo [CHECKLIST_THUC_THI_SSMS_UI_FIRST_GIAI_DOAN_B.md](/d:/Projects/SV/CSDL-PT/BankDds.Wpf/docs/sql/CHECKLIST_THUC_THI_SSMS_UI_FIRST_GIAI_DOAN_B.md).
2. Chạy xong runtime SQL theo [00_readme_runtime_execution_order.md](/d:/Projects/SV/CSDL-PT/BankDds.Wpf/sql/runtime/00_readme_runtime_execution_order.md).
3. Có tối thiểu 3 login test: `NGANHANG`, `CHINHANH`, `KHACHHANG`.
4. MSDTC + LINK1 hoạt động ở 2 chi nhánh.

## 3) Bảng test F1-F7

| ID | Mục tiêu | Cách test | Kết quả | Bằng chứng |
|---|---|---|---|---|
| F1 | Login theo 3 role | Dùng app login với 3 tài khoản; đối chiếu role + branch từ `sp_DangNhap`. Có thể dùng script [sp_dangnhap_test.sql](/d:/Projects/SV/CSDL-PT/BankDds.Wpf/docs/tests/sp_dangnhap_test.sql). | TODO | |
| F2 | Chuyển chi nhánh (NGANHANG chỉ xem) | Login `NGANHANG`, đổi branch trên Home; xác nhận chỉ xem được dữ liệu branch chọn và không thực hiện CRUD. | TODO | |
| F3 | CRUD đúng phạm vi role | `CHINHANH`: CRUD trong branch mình; `KHACHHANG`: không CRUD nghiệp vụ nội bộ; `NGANHANG`: không CRUD dữ liệu giao dịch/vận hành chi nhánh. | TODO | |
| F4 | Tạo/xóa/đổi mật khẩu qua SP auth | Thực hiện ở Admin: tạo login mới, reset mật khẩu, xóa login. Xác nhận chạy qua `sp_TaoTaiKhoan/sp_DoiMatKhau/sp_XoaTaiKhoan`. | TODO | |
| F5 | Chuyển tiền liên chi nhánh qua LINK + MSDTC | Chuyển từ tài khoản BT sang TD (và ngược lại), kiểm tra số dư + lịch sử giao dịch 2 bên, không mất nhất quán khi lỗi. | TODO | |
| F6 | Báo cáo theo thời gian + branch | Chạy `Sao kê`, `Tài khoản mở mới`, `Tổng hợp giao dịch` với các khoảng ngày/branch khác nhau. Đối chiếu số liệu DB. | TODO | |
| F7 | Tình huống lỗi network/link/site down | Tạm ngắt LINK hoặc dịch vụ liên quan rồi chạy giao dịch liên chi nhánh; xác nhận rollback đúng, không tạo dữ liệu nửa vời. | TODO | |

## 4) Bằng chứng code-level đã sẵn sàng (repo scope)

1. Login/role/branch từ `sp_DangNhap`:
- [AuthService.cs](/d:/Projects/SV/CSDL-PT/BankDds.Wpf/BankDds.Infrastructure/Security/AuthService.cs)
- [LoginViewModel.cs](/d:/Projects/SV/CSDL-PT/BankDds.Wpf/BankDds.Wpf/ViewModels/LoginViewModel.cs)

2. Rule branch/role trong UI + service authorization:
- [HomeViewModel.cs](/d:/Projects/SV/CSDL-PT/BankDds.Wpf/BankDds.Wpf/ViewModels/HomeViewModel.cs)
- [AuthorizationService.cs](/d:/Projects/SV/CSDL-PT/BankDds.Wpf/BankDds.Infrastructure/Security/AuthorizationService.cs)

3. Luồng account admin qua SP auth:
- [UserRepository.cs](/d:/Projects/SV/CSDL-PT/BankDds.Wpf/BankDds.Infrastructure/Data/Repositories/UserRepository.cs)
- [UserService.cs](/d:/Projects/SV/CSDL-PT/BankDds.Wpf/BankDds.Infrastructure/Data/UserService.cs)
- [AdminViewModel.cs](/d:/Projects/SV/CSDL-PT/BankDds.Wpf/BankDds.Wpf/ViewModels/AdminViewModel.cs)

4. Giao dịch liên chi nhánh + xử lý lỗi LINK/MSDTC:
- [TransactionRepository.cs](/d:/Projects/SV/CSDL-PT/BankDds.Wpf/BankDds.Infrastructure/Data/Repositories/TransactionRepository.cs)

5. Báo cáo theo khoảng thời gian/branch:
- [ReportRepository.cs](/d:/Projects/SV/CSDL-PT/BankDds.Wpf/BankDds.Infrastructure/Data/Repositories/ReportRepository.cs)

## 5) Log chạy trong phiên này

1. `dotnet build`: **FAILED** nhưng không trả về dòng compile error (0 warning, 0 error).  
2. Cần chạy lại build/test trên máy đã đủ .NET workload để có kết luận kỹ thuật cuối.

## 6) Kết luận giai đoạn F (phiên hiện tại)

- Chưa thể chốt PASS/FAIL cuối cho `F1..F7` vì thiếu chạy test live trên SQL instances.
- Đã chuẩn bị đầy đủ checklist, script tham chiếu, và bằng chứng code-level để chạy acceptance ngay.
