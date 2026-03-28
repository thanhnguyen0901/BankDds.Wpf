# Checklist Review UI/CRUD Theo Role

Ngày review: 2026-03-15

Phạm vi:
- Role `NganHang`, `ChiNhanh`, `KhachHang`
- Menu/tab hiển thị theo role
- CRUD theo role ở các màn `Quản trị`, `Khách hàng`, `Tài khoản`, `Nhân viên`, `Giao dịch`, `Báo cáo`
- Đối chiếu code hiện tại với:
  - `docs/requirements/DE3-NGANHANG.md`
  - `docs/requirements/DE3-NGANHANG_AUTHZ_WORKFLOW.md`

## 1. Kết luận nhanh

- Trạng thái triển khai 2026-03-15:
  - `DONE`: chặn sửa trực tiếp `SODU`
  - `DONE`: sửa luồng mở tài khoản liên chi nhánh
  - `DONE`: đổi sao kê `KhachHang` sang account picker
  - `DONE`: bỏ branch selector global của `NganHang`
  - `DONE`: sửa UX màn `Quản trị` cho `ChiNhanh`
  - `DONE`: đổi `AccountsView` thành `Tra cứu tài khoản`
  - `DONE`: bỏ hard-code branch list ở UI/validator liên quan
  - `DONE`: khóa `MACN` ở edit thường của `Nhân viên`
  - `DONE`: chuẩn hóa hiển thị tên chi nhánh/trạng thái
  - `DONE`: fix hiển thị `SĐT`
  - `DONE`: rà soát và nới width các bảng chính đang bị chật/header che
  - `BLOCKED VERIFICATION`: `dotnet build` hiện bị chặn bởi lỗi SDK resolver `MSB4276` trên máy local, chưa xác nhận được compile end-to-end bằng toolchain

- `NganHang`: branch selector ở header đang gây hiểu nhầm. Theo requirement hiện tại, selector này chỉ nên dùng cho phạm vi báo cáo, không phải filter toàn bộ tab `Quản trị`.
- `ChiNhanh`: việc thấy các tab `Khách hàng`, `Tài khoản`, `Nhân viên`, `Giao dịch`, `Báo cáo`, `Quản trị` là đúng với rule hiện tại.
- `ChiNhanh`: tab `Quản trị` đang đúng flow create-only.
- `ChiNhanh`: tab `Nhân viên` đang cho CRUD trong đúng chi nhánh đăng nhập. Đây đang khớp requirement hiện tại, không phải bug theo tài liệu đã chốt.
- `KhachHang`: UX ở báo cáo sao kê chưa tốt, vì chỉ prefill tài khoản đầu tiên vào textbox thay vì cho chọn từ danh sách tài khoản.
- Có 4 issue bổ sung đáng sửa, trong đó có 2 issue nghiêm trọng hơn các điểm UX:
  - workflow mở tài khoản liên chi nhánh đang bị chặn bởi UI hiện tại
  - app đang cho sửa trực tiếp `SODU`

## 1.1 Quyết định review đã chốt

### Đã chốt fix

- Chặn sửa trực tiếp `SODU` ở màn `Tài khoản` và subform tài khoản
- Sửa workflow mở tài khoản liên chi nhánh
- Đổi UX báo cáo của `KhachHang` từ textbox sang dropdown/account picker
- Bỏ branch selector global của `NganHang` khỏi header, chỉ giữ filter chi nhánh trong màn `Báo cáo`
- Sửa UX màn `Quản trị` cho `ChiNhanh`
- Bỏ hard-code danh sách chi nhánh, chuyển sang lấy động từ `IBranchService`
- Dọn UX màn `Nhân viên`: khóa `MACN` khi edit thường, chỉ đổi chi nhánh qua action riêng
- Gom về 1 luồng chính cho nghiệp vụ tài khoản:
  - giữ subform trong `CustomersView` để CRUD
  - đổi `AccountsView` thành màn tra cứu tài khoản
- Tên tab cuối cùng của `AccountsView`: `Tra cứu tài khoản`
- Mapping/converter tên chi nhánh sang tiếng Việt thân thiện trên toàn UI
- Mapping/converter trạng thái sang tiếng Việt thân thiện trên toàn UI
- Rà soát và fix toàn bộ table có header/cột bị che
- Rà soát và fix bug cột `SĐT` không load dữ liệu trên toàn UI app

### Đã rõ, chưa đổi requirement

- Tab `Nhân viên` của `ChiNhanh` hiện đang đúng theo requirement hiện có
- Tab `Quản trị` của `ChiNhanh` đúng về quyền backend; nếu cần đổi tên để đúng ngữ nghĩa UI thì sẽ làm trong scope fix UX

## 2. Review 5 vấn đề đã nêu

### 2.1 `NganHang` chọn chi nhánh ở header nhưng data `Quản trị` vẫn load tất cả

Trạng thái: `Không phải bug nghiệp vụ theo requirement hiện tại`, nhưng là `UX/logic hiển thị gây hiểu nhầm`

Đối chiếu:
- `docs/requirements/DE3-NGANHANG.md:184`: `NganHang` được chọn bất kỳ chi nhánh nào để xem báo cáo.
- `docs/requirements/DE3-NGANHANG_AUTHZ_WORKFLOW.md:24`: `NganHang` không là nhóm tác nghiệp CRUD hàng ngày.
- `docs/requirements/DE3-NGANHANG_AUTHZ_WORKFLOW.md:77`: login `NganHang` chọn được chi nhánh để xem báo cáo.
- `BankDds.Wpf/ViewModels/HomeViewModel.cs:26`: chỉ `NganHang` mới đổi branch ở header.
- `BankDds.Wpf/ViewModels/HomeViewModel.cs:63-65`: `NganHang` chỉ có `Báo cáo`, `Quản trị`, `Chi nhánh`, `Tra cứu KH`.
- `BankDds.Infrastructure/Data/UserService.cs:82-92`: `NganHang` lấy toàn bộ danh sách user, không filter theo branch.

Kết luận:
- Nếu bám đúng requirement hiện tại, branch selector ở header không nên được hiểu là filter cho tab `Quản trị`.
- Vấn đề thật nằm ở chỗ selector đang đặt ở header global nên tạo cảm giác nó chi phối toàn bộ màn hình.

Checklist:
- [x] Xác nhận requirement chỉ yêu cầu `NganHang` chọn chi nhánh để xem báo cáo
- [x] Xác nhận tab `Quản trị` của `NganHang` đang load global theo đúng logic hiện tại
- [DONE] Quyết định UX:
  - hoặc chuyển branch selector vào riêng màn `Báo cáo`
  - hoặc giữ ở header nhưng đổi label/behavior để thể hiện rõ chỉ áp dụng cho report scope
- [DONE] Đồng bộ giá trị branch selector header sang default filter của các report tab

### 2.2 `ChiNhanh` vào tab `Nhân viên` thấy có thể sửa/xóa/phục hồi nhân viên

Trạng thái: `Đúng với requirement hiện tại`

Đối chiếu:
- `docs/requirements/DE3-NGANHANG.md:125-129`: đề tài yêu cầu cập nhật nhân viên gồm thêm, xóa, ghi, chuyển chi nhánh.
- `docs/requirements/DE3-NGANHANG.md:188-190`: `ChiNhanh` toàn quyền trên chi nhánh đã đăng nhập.
- `docs/requirements/DE3-NGANHANG_AUTHZ_WORKFLOW.md:26-27`: `ChiNhanh` toàn quyền tác nghiệp trên chi nhánh đang đăng nhập.
- `BankDds.Wpf/ViewModels/EmployeesViewModel.cs:114-118`: CRUD `Nhân viên` enable cho `ChiNhanh`.
- `BankDds.Wpf/ViewModels/EmployeesViewModel.cs:140`: dữ liệu nhân viên của `ChiNhanh` chỉ load theo `_userSession.SelectedBranch`.

Kết luận:
- Theo tài liệu đã chốt trong repo, `ChiNhanh` được CRUD nhân viên thuộc chính chi nhánh đang đăng nhập.
- Nếu business mới đổi ý rằng `ChiNhanh` chỉ được xem chứ không được sửa nhân viên khác, đó là thay đổi requirement, không phải bug code theo mốc chốt 2026-03-13/2026-03-14.

Checklist:
- [x] Xác nhận code hiện tại cho `ChiNhanh` CRUD nhân viên trong chi nhánh mình
- [x] Xác nhận hành vi này khớp requirement hiện tại
- [ ] Nếu muốn đổi rule: cập nhật lại tài liệu requirement trước rồi mới khóa UI/service/SQL

### 2.3 Tab hiển thị cho `ChiNhanh` và logic `Quản trị`/`Nhân viên`

Trạng thái:
- Tab `Quản trị`: `Đúng`
- Tab `Nhân viên`: `Đúng theo requirement hiện tại`, nhưng có 1 số chỗ UI cần siết lại

Đối chiếu:
- `BankDds.Wpf/ViewModels/HomeViewModel.cs:58-63`: `ChiNhanh` thấy `Khách hàng`, `Tài khoản`, `Nhân viên`, `Giao dịch`, `Báo cáo`, `Quản trị`.
- `BankDds.Wpf/ViewModels/AdminViewModel.cs:161-163`: `ChiNhanh` không được sửa/xóa login, chỉ create.
- `BankDds.Wpf/ViewModels/AdminViewModel.cs:221-222`: `ChiNhanh` chỉ tạo được `ChiNhanh` hoặc `KhachHang`.
- `BankDds.Infrastructure/Data/UserService.cs:93-98`: danh sách user của `ChiNhanh` bị lọc theo branch của phiên đăng nhập và role mà họ được tạo.

Kết luận:
- `Quản trị` cho `ChiNhanh` theo mode create-only là đúng.
- `Nhân viên` cho `ChiNhanh` CRUD trong chi nhánh hiện tại là đúng theo tài liệu hiện có.

Checklist:
- [x] Xác nhận `Quản trị` của `ChiNhanh` là create-only
- [x] Xác nhận `Nhân viên` của `ChiNhanh` là branch-scoped CRUD
- [ ] Làm rõ lại với stakeholder nếu muốn đổi rule `Nhân viên` thành read-only

### 2.4 `KhachHang` chỉ có tab `Báo cáo`, nhưng số tài khoản là textbox

Trạng thái: `Bug UX đã xác nhận`

Đối chiếu:
- `BankDds.Wpf/Views/ReportsView.xaml:58-61`: `StatementAccountNumber` đang là `TextBox`.
- `BankDds.Wpf/ViewModels/ReportsViewModel.cs:253-260`: khi login `KhachHang`, app chỉ lấy danh sách tài khoản rồi gán account đầu tiên vào textbox.
- `docs/requirements/DE3-NGANHANG_AUTHZ_WORKFLOW.md:49`: `KhachHang` phải xem được tất cả tài khoản thuộc chính mình, kể cả ở chi nhánh khác.

Kết luận:
- Logic quyền đúng, nhưng UX hiện tại chưa đáp ứng tốt case khách có nhiều tài khoản.
- Nên đổi textbox thành dropdown/account picker lấy từ danh sách tài khoản của chính khách hàng.

Checklist:
- [x] Xác nhận vấn đề UX
- [x] Đổi `StatementAccountNumber` sang `ComboBox`
- [x] Load toàn bộ tài khoản của khách hàng vào danh sách chọn
- [x] Vẫn giữ guard backend để khách hàng chỉ xem được tài khoản của chính mình

### 2.5 Recheck toàn bộ UI CRUD theo role

Trạng thái: `Đã review`

Snapshot hiện tại:

`NganHang`
- Thấy tab: `Báo cáo`, `Quản trị`, `Chi nhánh`, `Tra cứu KH`
- Không thấy tab tác nghiệp `Khách hàng`, `Tài khoản`, `Nhân viên`, `Giao dịch`
- `Quản trị`: tạo login `NganHang`, reset/xóa login
- `Chi nhánh`: quản lý branch master
- `Tra cứu KH`: tra cứu liên chi nhánh
- `Báo cáo`: xem báo cáo, nhưng branch selector header chưa sync tốt với report filter

`ChiNhanh`
- Thấy tab: `Khách hàng`, `Tài khoản`, `Nhân viên`, `Giao dịch`, `Báo cáo`, `Quản trị`
- `Quản trị`: create-only cho `ChiNhanh`/`KhachHang` trong chi nhánh hiện tại
- `Khách hàng`: CRUD theo chi nhánh đăng nhập
- `Tài khoản`: đang CRUD theo chi nhánh đăng nhập, nhưng còn bug workflow và bug sửa `SODU`
- `Nhân viên`: CRUD + chuyển chi nhánh cho nhân viên thuộc chi nhánh đăng nhập
- `Giao dịch`: gửi/rút/chuyển trong scope branch hiện tại

`KhachHang`
- Thấy tab: `Báo cáo`
- Chỉ xem sao kê tài khoản của chính mình

## 3. Các issue bổ sung phát hiện thêm

### 3.1 Bug nghiêm trọng: workflow mở tài khoản liên chi nhánh đang bị chặn bởi UI hiện tại

Mức độ: `High`

Đối chiếu:
- `docs/requirements/DE3-NGANHANG_AUTHZ_WORKFLOW.md:48`: một khách hàng có thể mở nhiều tài khoản ở nhiều chi nhánh.
- `docs/requirements/DE3-NGANHANG_AUTHZ_WORKFLOW.md:50`: `ChiNhanh` thao tác tài khoản theo `TAIKHOAN.MACN` của chi nhánh đang đăng nhập.
- `BankDds.Wpf/ViewModels/AccountsViewModel.cs:160`: `ChiNhanh` chỉ load khách hàng theo `GetCustomersByBranchAsync(_userSession.SelectedBranch)`.
- `BankDds.Wpf/ViewModels/AccountsViewModel.cs:186`: khi mở tài khoản mới, `MACN = SelectedCustomer.MaCN`.
- `BankDds.Wpf/ViewModels/CustomersViewModel.cs:357`: subform mở tài khoản cũng gán `MACN = SelectedCustomer.MaCN`.
- `BankDds.Infrastructure/Data/Repositories/AccountRepository.cs:91-105`: repository lấy tài khoản theo khách hàng ở publisher, nghĩa là backend có support nhìn xuyên chi nhánh.

Tác động:
- Nhân viên chi nhánh TANDINH không thể mở tài khoản TANDINH cho khách hàng đăng ký gốc ở BENTHANH.
- Điều này đi ngược rule “1 khách hàng nhiều tài khoản ở nhiều chi nhánh”.

Checklist:
- [x] Xác nhận mismatch giữa requirement và UI hiện tại
- [x] Thiết kế lại màn `Tài khoản` cho `ChiNhanh`:
  - chọn khách hàng bằng tra cứu CMND toàn hệ thống
  - nhưng branch của tài khoản mới phải cố định theo branch đăng nhập
- [x] Không buộc `MACN` tài khoản mới = `KHACHHANG.MACN`

### 3.2 Bug nghiêm trọng: app đang cho sửa trực tiếp `SODU`

Mức độ: `Critical`

Đối chiếu:
- `BankDds.Wpf/Views/AccountsView.xaml:158-162`: form sửa tài khoản cho nhập `EditingAccount.SODU`.
- `BankDds.Wpf/Views/CustomersView.xaml:426`: subform tài khoản trong màn khách hàng cũng cho nhập `EditingAccount.SODU`.
- `BankDds.Wpf/ViewModels/AccountsViewModel.cs:229`: save gọi `UpdateAccountAsync`.
- `BankDds.Wpf/ViewModels/CustomersViewModel.cs:401`: save subform cũng gọi `UpdateAccountAsync`.
- `BankDds.Infrastructure/Data/Repositories/AccountRepository.cs:171-176`: `SP_UpdateAccount`.
- `sql/03_publisher_sp_views.sql:370-378`: `SP_UpdateAccount` update trực tiếp `SODU = @SODU`.

Tác động:
- Số dư có thể bị thay đổi bằng thao tác edit form, bỏ qua toàn bộ flow `Gửi tiền`, `Rút tiền`, `Chuyển tiền`.
- Đây là lệch business rất lớn và có thể phá tính nhất quán số liệu.

Checklist:
- [x] Xác nhận bug
- [x] Loại bỏ khả năng edit `SODU` trên UI
- [x] Giới hạn `SP_UpdateAccount` chỉ cho cập nhật metadata thực sự hợp lệ, hoặc bỏ hẳn nếu không cần
- [x] Bắt buộc thay đổi số dư chỉ qua module `Giao dịch`

### 3.3 Bug UX/logic: branch selector của `NganHang` ở header không đồng bộ với report filters

Mức độ: `Medium`

Đối chiếu:
- `BankDds.Wpf/Views/HomeView.xaml:145-150`: header có `SelectedBranchCode`.
- `BankDds.Wpf/ViewModels/HomeViewModel.cs:86-97`: đổi branch chỉ cập nhật session và reload active tab.
- `BankDds.Wpf/ViewModels/ReportsViewModel.cs:230-248`: report filters của `NganHang` luôn khởi tạo `ALL` trước, không lấy default từ `_userSession.SelectedBranch`.

Tác động:
- Người dùng chọn `BENTHANH` ở header nhưng vào báo cáo vẫn thấy mặc định `ALL`.
- Dễ tạo cảm giác app bỏ qua branch selector.

Checklist:
- [x] Xác nhận mismatch
- [x] Chốt phương án A:
  - bỏ selector global ở header
  - chỉ giữ filter chi nhánh trong màn `Báo cáo`
  - semantics rõ ràng: chọn chi nhánh để lấy báo cáo phù hợp, tức filter theo chi nhánh trong report scope

### 3.4 Bug maintainability/UX: danh sách chi nhánh đang hard-code ở một số màn

Mức độ: `Medium`

Đối chiếu:
- `BankDds.Wpf/ViewModels/EmployeesViewModel.cs:113`: hard-code `BENTHANH`, `TANDINH`.
- `BankDds.Wpf/ViewModels/AdminViewModel.cs:238-239`: fallback hard-code `BENTHANH`, `TANDINH`.

Tác động:
- Nếu thêm chi nhánh mới ở module `Chi nhánh`, một số form sẽ không phản ánh đúng.
- Màn `Nhân viên` và `Quản trị` có thể bị lệch dữ liệu cấu hình thực tế.

Checklist:
- [x] Xác nhận hard-code
- [x] Đưa danh sách chi nhánh về cùng một nguồn dữ liệu động
- [x] Chỉ fallback hard-code khi demo/dev mode và phải có cảnh báo rõ

### 3.5 Bug UX: màn `Nhân viên` cho chọn trực tiếp `MACN` dù việc chuyển chi nhánh đã có flow riêng

Mức độ: `Low`

Đối chiếu:
- `BankDds.Wpf/Views/EmployeesView.xaml:142-143`: `EditingEmployee.MACN` bind trực tiếp với combo tất cả chi nhánh.
- `BankDds.Wpf/Views/EmployeesView.xaml:162-166`: đồng thời lại có flow `ExecuteTransferBranch` riêng.

Tác động:
- UI thể hiện 2 cách đổi chi nhánh cho cùng một nhân viên.
- Người dùng có thể hiểu nhầm là chỉ cần sửa `MACN` rồi `Ghi`.

Checklist:
- [x] Xác nhận UI bị trùng ý nghĩa
- [x] Khi edit nhân viên thường, khóa field `MACN`
- [x] Chỉ cho đổi chi nhánh qua action `Chuyển nhân viên`

### 3.6 Bug UX: tab/màn `Quản trị` của `ChiNhanh` đang đúng quyền nhưng biểu đạt sai ngữ nghĩa

Mức độ: `Medium`

Đối chiếu:
- `BankDds.Wpf/ViewModels/AdminViewModel.cs:161-163`: với `ChiNhanh`, nút `Sửa`, `Xóa`, `Phục hồi` thực chất không dùng được.
- `BankDds.Wpf/ViewModels/AdminViewModel.cs:221-222`: `ChiNhanh` chỉ có create-flow cho `ChiNhanh` và `KhachHang`.
- `BankDds.Wpf/Views/AdminView.xaml:26-38`: UI vẫn hiện các nút `Sửa`, `Xóa login`.
- `BankDds.Wpf/Views/HomeView.xaml:234`: tab vẫn dùng tên `Quản trị` cho cả `NganHang` lẫn `ChiNhanh`.

Tác động:
- Người dùng `ChiNhanh` dễ hiểu nhầm rằng mình đang vào màn quản trị đầy đủ nhưng bị khóa chức năng.
- Tên tab `Quản trị` làm lệch kỳ vọng, trong khi use case thực tế của role này chỉ là tạo tài khoản đăng nhập trong phạm vi chi nhánh.

Checklist:
- [x] Xác nhận quyền backend hiện tại là đúng, issue nằm ở UX/wording
- [x] Với `ChiNhanh`, đổi tên tab/màn sang ngữ nghĩa đúng hơn, ví dụ:
  - `Tài khoản đăng nhập`
  - `Tạo người dùng`
- [x] Với `ChiNhanh`, ẩn hẳn nút `Sửa`, `Xóa` thay vì chỉ disable
- [x] Với `ChiNhanh`, đổi title/subtitle theo role, ví dụ:
  - `Tạo tài khoản đăng nhập cho chi nhánh`
- [x] Chỉ giữ giao diện `Quản trị` đầy đủ cho `NganHang`
- [x] Subtitle cụ thể sẽ do implementation quyết định, miễn đúng ngữ nghĩa create-only và đúng requirement

### 3.7 Bug UI: một số bảng đang có cột/header bị che hoặc phân bổ độ rộng chưa hợp lý

Mức độ: `Medium`

Mô tả:
- Hiện có tình trạng một số `DataGrid` dùng độ rộng cột cố định khiến header bị che, nội dung bị cụt, hoặc cột quá chật so với dữ liệu thực tế.
- Cần rà soát toàn bộ UI app và chuyển các cột phù hợp sang `*`, `Auto`, hoặc phân bổ width hợp lý hơn.

Checklist:
- [x] Chốt fix trên toàn UI app
- [DONE] Rà soát tất cả `DataGrid`/table trong app
- [DONE] Các cột có header bị che phải được chỉnh lại width
- [DONE] Các cột dữ liệu dài/ngắn cần phân bổ width hợp lý để hiện đầy đủ thông tin

### 3.8 Cần chuẩn hóa hiển thị tên chi nhánh trên toàn UI app

Mức độ: `Medium`

Mô tả:
- Hiện app đang hiển thị mã chi nhánh kỹ thuật như `BENTHANH`, `TANDINH`.
- Về UI nên hiển thị tên thân thiện như `Bến Thành`, `Tân Định`.

Checklist:
- [x] Chốt fix apply toàn UI app
- [DONE] Thêm converter hoặc mapping dùng chung cho branch code -> branch display name
- [DONE] Áp dụng cho toàn bộ UI đang hiển thị chi nhánh
- [DONE] Vẫn giữ code/mã gốc ở binding nội bộ nếu cần cho logic nghiệp vụ

### 3.9 Cần chuẩn hóa hiển thị trạng thái trên toàn UI app

Mức độ: `Medium`

Mô tả:
- Hiện app còn hiển thị giá trị kỹ thuật như `Active`, `Closed`.
- UI nên hiển thị tiếng Việt phù hợp ngữ cảnh.

Checklist:
- [x] Chốt fix apply toàn UI app
- [DONE] Thêm converter hoặc mapping dùng chung cho status -> tiếng Việt
- [DONE] Áp dụng cho toàn bộ UI đang hiển thị trạng thái

### 3.10 Bug dữ liệu: cột `SĐT` không load được dữ liệu ở một số màn

Mức độ: `High`

Mô tả:
- Có bug mới: cột `SĐT` hiện không load được dữ liệu.
- Cần kiểm tra toàn bộ UI app để xác định chỗ bind sai model/property hoặc map sai từ repository/viewmodel.

Checklist:
- [x] Chốt fix apply toàn UI app
- [DONE] Rà soát toàn bộ UI đang có cột/field `SĐT`
- [DONE] Xác định mismatch giữa model, repository mapping, viewmodel, XAML binding
- [DONE] Fix đồng bộ để dữ liệu `SĐT` hiển thị đúng trên toàn app

## 4. Plan fix/update đề xuất

### Phase 1: chặn các sai lệch nghiệp vụ nghiêm trọng

1. Chặn sửa trực tiếp `SODU`
- xóa field edit số dư ở cả `AccountsView` và subform trong `CustomersView`
- thu hẹp `SP_UpdateAccount` và service liên quan
- test lại gửi/rút/chuyển để đảm bảo chỉ giao dịch mới làm đổi số dư

2. Sửa workflow mở tài khoản liên chi nhánh
- tách `Customer branch` khỏi `Account branch`
- cho `ChiNhanh` tra cứu khách hàng toàn hệ thống theo CMND khi mở tài khoản
- ép `Account.MACN = session branch`
- giữ guard backend theo branch của tài khoản, không theo chi nhánh đăng ký gốc của khách

### Phase 2: làm rõ behavior theo role trong UI

3. Làm rõ selector chi nhánh của `NganHang`
- nếu bám requirement hiện tại, selector chỉ phục vụ báo cáo
- bỏ selector khỏi header global
- chỉ giữ filter chi nhánh trong màn `Báo cáo`
- không áp branch filter này vào `Quản trị`
Trạng thái quyết định: `Chốt fix theo phương án A`

4. Hoàn thiện UX `KhachHang`
- đổi ô nhập số tài khoản sang dropdown/account picker
- hiển thị tất cả tài khoản của khách hàng
Trạng thái quyết định: `Chốt fix`

5. Giảm hiểu nhầm ở màn `Nhân viên`
- khóa field `MACN` trong edit form
- chỉ cho chuyển chi nhánh qua action riêng
Trạng thái quyết định: `Chốt fix`

6. Sửa UX màn `Quản trị` cho `ChiNhanh`
- giữ quyền create-only như hiện tại
- đổi tên tab/title theo role
- ẩn các action `Sửa`, `Xóa` với `ChiNhanh`
- chỉ `NganHang` mới thấy đầy đủ semantics `Quản trị`
Trạng thái quyết định: `Chốt fix`

### Phase 3: dọn nợ kỹ thuật

7. Bỏ hard-code branch list
- dùng `IBranchService` ở mọi màn cần danh sách chi nhánh
Trạng thái quyết định: `Chốt fix`

8. Review lại 2 màn quản lý tài khoản đang bị trùng logic
- `AccountsView`
- subform tài khoản trong `CustomersView`
- gom rule dùng chung để tránh fix lệch 2 nơi
Trạng thái quyết định: `Chốt fix`
- Giữ subform trong `CustomersView` làm luồng CRUD tài khoản chính
- Đổi `AccountsView` thành màn `Tra cứu tài khoản`
- Không để `AccountsView` tiếp tục là entry point CRUD trùng lặp

9. Rà soát và fix UI table width/header trên toàn app
- chỉnh các cột bị che bằng `*`, `Auto`, hoặc width hợp lý
Trạng thái quyết định: `Chốt fix`

10. Chuẩn hóa hiển thị tên chi nhánh trên toàn app
- dùng converter/mapping branch code -> tên tiếng Việt
Trạng thái quyết định: `Chốt fix`

11. Chuẩn hóa hiển thị trạng thái trên toàn app
- dùng converter/mapping status -> tiếng Việt
Trạng thái quyết định: `Chốt fix`

12. Fix bug `SĐT` không load dữ liệu trên toàn app
- rà soát repository -> model -> viewmodel -> XAML binding
Trạng thái quyết định: `Chốt fix`

## 5. Đề xuất thứ tự triển khai

Ưu tiên 1:
- [x] Fix bug sửa trực tiếp `SODU`
- [x] Fix workflow mở tài khoản liên chi nhánh

Ưu tiên 2:
- [x] Fix UX `KhachHang` chọn tài khoản bằng dropdown
- [x] Fix branch selector `NganHang` theo phương án A
- [x] Fix UX màn `Quản trị` cho `ChiNhanh`

Ưu tiên 3:
- [x] Bỏ hard-code branch list
- [x] Dọn UI `Nhân viên` để không nhập nhằng giữa edit và transfer
- [x] Gom logic `AccountsView` và subform tài khoản theo hướng đã chốt
- [x] Rà soát và fix table width/header trên toàn app
- [x] Chuẩn hóa tên chi nhánh trên toàn app
- [x] Chuẩn hóa trạng thái trên toàn app
- [x] Fix bug `SĐT` không load dữ liệu trên toàn app

## 6. Chốt review

Kết luận cuối:
- Không phải toàn bộ 5 vấn đề ban đầu đều là bug.
- Có 2 điểm anh nêu thực chất đang đúng theo requirement hiện tại:
  - `ChiNhanh` thấy tab `Quản trị` theo mode create-only
  - `ChiNhanh` CRUD `Nhân viên` trong chi nhánh mình
- Có 2 bug thật cần sửa sớm hơn cả 5 điểm ban đầu:
  - mở tài khoản liên chi nhánh đang sai flow
  - sửa trực tiếp `SODU` đang phá business rule

## 7. Hau kiem runtime

- 1. [x] DONE: fix crash khi mo dropdown chon tai khoan o man `Bao cao` cua `KhachHang`
  - nguyen nhan: `ComboBox` editable + item display binding vao property read-only `Account.BranchDisplayName`
  - cach sua: doi sang dropdown chon thuan, bind `SelectedValue` theo `SOTK`, dung `StatementDisplayText` de hien thi
- 2. [x] DONE: fix loi `Tra cuu KH` theo ten bi `SELECT permission was denied on dbo.KHACHHANG` voi role `NganHang`
  - nguyen nhan: `CustomerLookupRepository` dang query truc tiep bang `dbo.KHACHHANG`, trong khi script security da `DENY SELECT` cho role `NGANHANG`
  - cach sua: doi repository sang goi stored procedure, them `SP_SearchCustomersByName`, va grant `EXECUTE` cho role `NGANHANG`
- 3. [x] DONE: fix loi `Tra cuu KH` theo `CMND` bi loi permission voi role `NganHang`
  - nguyen nhan: `CustomerLookupRepository` dang query truc tiep bang `dbo.KHACHHANG` ca o flow tim theo `CMND`, trong khi role `NGANHANG` khong duoc `SELECT` bang goc
  - cach sua: doi flow tim theo `CMND` sang goi `SP_GetCustomerByCMND` thay vi query truc tiep bang
- 4. [x] DONE: fix loi `Tra cuu KH` theo ho ten bao `Could not find stored procedure dbo.SP_SearchCustomersByName`
  - nguyen nhan: publisher chua co stored procedure `dbo.SP_SearchCustomersByName` trong DB dang chay
  - cach sua: run lai `sql/03_publisher_sp_views.sql`, sau do run lai `sql/04_publisher_security.sql` de dam bao proc ton tai va role `NGANHANG` co quyen `EXECUTE`
- 5. [x] DONE: fix dropdown chi nhanh o 3 subtab `Bao cao` cua `NganHang` khong doi filter data theo branch duoc chon
  - nguyen nhan: 3 `ComboBox` chi nhanh chi bind `ItemsSource`, chua bind `SelectedItem` ve `SelectedBranchForAccounts`, `SelectedBranchForCustomers`, `SelectedBranchForTransactionSummary`, nen ViewModel van giu gia tri cu
  - cach sua: bind `SelectedItem` hai chieu cho ca 3 dropdown de branch duoc chon di dung vao report service
- 6. [x] DONE: sync `SP_SearchCustomersByName` va grant tu publisher xuong subscriber tra cuu `SQLSERVER4`
  - nguyen nhan: `LookupDatabase` la subscriber `PUB_TRACUU`; publisher da doi SP nhung subscriber tra cuu chua nhan proc/grant moi nen van bao `Could not find stored procedure dbo.SP_SearchCustomersByName`
  - cach sua: dam bao proc nam trong Articles cua `PUB_TRACUU`, chay lai `03_publisher_sp_views.sql` va `04_publisher_security.sql` tren publisher, sau do chay Snapshot Agent/Distribution Agent hoac reinitialize subscription de phat tan schema/SP sang subscriber
- 7. [x] DONE: revert fix tam fallback app-level o `CustomerLookupRepository`
  - nguyen nhan: fallback tu `LookupDatabase` sang publisher la workaround, khong dung huong replication mong muon
  - cach sua: revert repository ve dung luong lookup subscriber, chi xu ly bang rollout schema/SP qua replication
- 8. [x] DONE: fix loi `KhachHang` chon tai khoan hop le nhung `Tao bao cao` bi `Access denied: KHACHHANG can only view own account statement`
  - nguyen nhan: danh sach tai khoan cua `KhachHang` load theo `CustomerCMND` trong session app, nhung `SP_GetAccountStatement` lai tu tuyen danh tinh `KHACHHANG` o tang SQL. Hai ben khong dung chung 1 nguon identity nen xay ra lech: UI cho chon dung tai khoan, nhung SQL van tu choi
  - cach sua: giu nguyen flow load danh sach tai khoan dang chay dung, va truyen `CustomerCMND` tu session app xuong `SP_GetAccountStatement` mot cach tuong minh de SQL check dung chinh chu tai khoan dang dang nhap
- 9. [x] DONE: fix UX/input sao ke tai khoan cho role `ChiNhanh`/`NganHang`
  - nguyen nhan: `StatementAccounts` hien chi duoc load cho role `KhachHang`, nen khi `ChiNhanh` vao sub-tab `Sao ke tai khoan` thi `ComboBox` mo ra khong co item nao
  - cach sua: tach UX theo role
    - `KhachHang`: giu dropdown chon tai khoan cua chinh minh
    - `ChiNhanh`/`NganHang`: chuyen sang `TextBox` nhap `SOTK`
- 10. [x] DONE: fix UI nut `Mo tai khoan` bi cat o panel tai khoan trong man `Khach hang`
  - nguyen nhan: cot panel ben phai co width co dinh va nhom button action dung `StackPanel` ngang, tong chieu rong button vuot qua khong gian hien thi nen nut bi cat
  - cach sua: doi nhom button action sang `WrapPanel` va noi rong cot ben phai de button tu xuong dong khi thieu rong
- 11. [x] DONE: can chinh lai 3 nut action tai khoan de ngang hang va deu nhau
  - nguyen nhan: `WrapPanel` tranh duoc viec bi cat nhung khong dam bao 3 nut thang hang va dong deu
  - cach sua: doi nhom button sang `Grid` 3 cot bang nhau de 3 nut cung hang va cung be ngang
- 12. [ ] REVIEW: bug load/save truong `Phai` o form sua va can ra soat toan bo man dang dung dropdown `Phai`
  - pham vi user report: role `ChiNhanh` vao tab `Khach hang`, chon 1 khach hang roi bam `Sua` thi truong `Phai` khong preselect; mo dropdown chon lai va `Save` thi bao loi
  - nguyen nhan root cause:
    - [CustomersView.xaml] form sua khach hang dang bind `ComboBox.SelectedValue` vao `EditingCustomer.Phai` nhung item lai khai bao bang `ComboBoxItem` tinh
    - [EmployeesView.xaml] form sua nhan vien dang dung cung pattern bind sai voi `EditingEmployee.PHAI`
    - model/repository/validator deu dang lam viec voi string gia tri `Nam`/`Nữ`, nhung UI lai dua vao `ComboBoxItem`; vi vay luc load edit khong map duoc item dang chon, va luc nguoi dung chon lai thi gia tri tra ve khong on dinh theo string domain mong doi
  - doi chieu:
    - `BankDds.Wpf/Views/CustomersView.xaml`: field `Phai` cua customer edit form
    - `BankDds.Wpf/Views/EmployeesView.xaml`: field `Phai` cua employee edit form
    - `BankDds.Core/Validators/CustomerValidator.cs`: validator chi chap nhan `Nam` hoac `Nữ`
    - `BankDds.Core/Validators/EmployeeValidator.cs`: validator chi chap nhan `Nam` hoac `Nữ`
    - `BankDds.Infrastructure/Data/Repositories/CustomerRepository.cs`: repository map `PHAI` thanh string trim
    - `BankDds.Infrastructure/Data/Repositories/EmployeeRepository.cs`: repository map `PHAI` thanh string trim
  - solution fix de xuat:
    - doi toan bo dropdown `Phai` tu `ComboBoxItem` tinh sang binding string thuần, vi du `ItemsSource` = danh sach `Nam`, `Nữ`
    - bind `SelectedItem` hai chieu truc tiep ve property string (`EditingCustomer.Phai`, `EditingEmployee.PHAI`) thay vi `SelectedValue`
    - ra soat tat ca man/form dang edit `Phai` de dong bo 1 kieu binding, tranh lap lai bug cung class
    - sau khi sua, retest 2 case:
      - sua `Khach hang`: vao edit phai preselect dung gia tri cu va save thanh cong khi khong doi hoac doi `Phai`
      - sua `Nhan vien`: mo edit phai preselect dung va save thanh cong
  - trang thai: cho `approve` truoc khi update code
- 13. [ ] REVIEW: button `Ghi khach hang` khong enable khi them moi du da nhap du lieu
  - pham vi user report: role `ChiNhanh` vao tab `Khach hang`, bam `Them moi`, nhap day du form nhung button `Ghi khach hang` van disable nen khong save duoc
  - nguyen nhan root cause:
    - [CustomersViewModel.cs] `CanSave` dang phu thuoc vao `EditingCustomer.CMND`
    - [CustomersView.xaml] form lai bind truc tiep vao property long nhau `EditingCustomer.*`
    - [Customer.cs] la POCO thuong, khong phat `PropertyChanged`
    - vi vay khi nguoi dung go vao `EditingCustomer.CMND`, `CustomersViewModel` khong nhan duoc thay doi de reevaluate `CanSave`; button `Save` bind theo convention cua Caliburn.Micro nen giu nguyen trang thai disable
  - doi chieu:
    - `BankDds.Wpf/ViewModels/CustomersViewModel.cs`: `CanSave`, setter `EditingCustomer`, setter `IsEditing`
    - `BankDds.Wpf/Views/CustomersView.xaml`: cac field bind vao `EditingCustomer.CMND`, `EditingCustomer.Ho`, `EditingCustomer.Ten` va nut `x:Name="Save"`
    - `BankDds.Core/Models/Customer.cs`: model khong co co che notify thay doi
  - nhan dinh them ve scope:
    - day la bug pattern, khong chi rieng `Khach hang`
    - cac form khac co nguy co tuong tu vi `CanSave` cung phu thuoc vao `Editing...` nested object gom:
      - `EmployeesViewModel.CanSave` voi `EditingEmployee`
      - `BranchesViewModel.CanSave` voi `EditingBranch`
      - `CustomersViewModel.CanSaveAccount` voi `EditingAccount`
      - `AdminViewModel.CanSave` co mot phan phu thuoc vao `EditingUser.Username`
  - solution fix de xuat:
    - chot 1 co che dong bo cho cac edit form:
      - hoac cho cac edit model (`Customer`, `Employee`, `Branch`, `Account`, `User`) implement `INotifyPropertyChanged` / `PropertyChangedBase` va ViewModel subscribe de raise lai `CanSave`, `CanSaveAccount`, cac display property lien quan
      - hoac tao form state/wrapper trong ViewModel thay vi bind truc tiep vao POCO model
    - trong scope pragmatical fix hien tai, uu tien:
      - lam cho `EditingCustomer` phat/sync thay doi de `CanSave` duoc reevaluate ngay khi nhap
      - dong bo cung pattern cho `EditingEmployee`, `EditingBranch`, `EditingAccount`, `EditingUser` de tranh lap lai bug cung loai
  - retest sau fix:
    - `Khach hang`: `Them moi` -> nhap `CMND` thi nut `Ghi khach hang` enable
    - `Khach hang`: subform tai khoan -> nhap `SOTK` thi nut luu tai khoan enable dung
    - `Nhan vien`: `Them moi/Sua` -> nhap `Ho/Ten` thi nut `Ghi` enable dung
    - `Chi nhanh`: `Them moi` -> nhap `MACN/TENCN` thi nut `Ghi` enable dung
    - `Quan tri`: tao user -> nhap `Username` va password hop le thi nut `Ghi` enable dung
  - trang thai: cho `approve` truoc khi update code
- 14. [x] DONE: vua add xong khach hang, mo sua ngay thi `Save` bao `Khong the luu khach hang`
  - pham vi user report: role `ChiNhanh` vao tab `Khach hang`, `Them moi` thanh cong; sau do chon lai chinh khach hang vua tao, vao `Sua` va `Save` thi app bao `Khong the luu khach hang`
  - nguyen nhan root cause:
    - [CustomerService.cs] flow `UpdateCustomerAsync` dang goi `_customerRepository.GetCustomerByCMNDAsync(customer.CMND)` de tim ban ghi hien co truoc khi update
    - [CustomerRepository.cs] `GetCustomerByCMNDAsync` lai doc tu publisher connection
    - [CustomerRepository.cs] `AddCustomerAsync` thi ghi vao branch database cua `customer.MaCN`
    - vi vay ngay sau khi add, neu replication tu branch len publisher chua kip dong bo, `GetCustomerByCMNDAsync` se tra `null`; service ket luan `existing == null` va tra `false`, UI hien `Khong the luu khach hang`
  - doi chieu:
    - `BankDds.Infrastructure/Data/CustomerService.cs`: `UpdateCustomerAsync`, `DeleteCustomerAsync`, `RestoreCustomerAsync`
    - `BankDds.Infrastructure/Data/Repositories/CustomerRepository.cs`: `GetCustomerByCMNDAsync` dang dung publisher; `AddCustomerAsync` dang ghi branch DB
    - `BankDds.Wpf/ViewModels/CustomersViewModel.cs`: UI hien message generic khi service tra `false`
  - tac dong/risk:
    - bug nay xuat hien ro nhat o case `add -> edit ngay`
    - co the lap lai voi `Delete`/`Restore` customer neu publisher chua kip nhan replication
    - ban chat day la bug consistency theo thoi diem giua write-path va read-path
  - solution fix de xuat:
    - tach ro lookup publisher va lookup branch-local
    - voi mutate flow customer (`update/delete/restore`) cua role `ChiNhanh`, khong duoc check existence bang publisher; phai doc tu branch shard hien tai/branch cua customer
    - neu can authorize theo branch goc, lay ban ghi tu branch-local source truoc roi moi `RequireCanModifyBranch`
    - cai thien thong diep loi de phan biet ro case `khong tim thay customer trong branch scope` thay vi generic `Khong the luu khach hang`
  - retest sau fix:
    - `Them moi` customer -> save thanh cong
    - chon lai ngay customer vua tao -> `Sua` -> `Save` thanh cong ngay lap tuc, khong can cho replication
    - `Delete/Restore` customer moi tao van chay dung trong branch scope
  - cach sua da ap dung:
    - them branch-local lookup cho customer mutate flow
    - `Update/Delete/Restore` customer cua `ChiNhanh` khong con check existence bang publisher
    - giu publisher cho lookup lien chi nhanh/read-only
  - ket qua user test: `passed`
- 15. [x] DONE: man dang nhap ho tro nhan `Enter` de thuc hien login
  - pham vi user report: sau khi nhap xong ten dang nhap va mat khau, nguoi dung muon nhan `Enter` thay vi phai click button `Dang nhap`
  - nguyen nhan: nut `Login` chua duoc danh dau la default button cua form, nen phim `Enter` khong trigger action dang nhap
  - cach sua da ap dung:
    - dat `IsDefault=\"True\"` cho button `Login` trong `BankDds.Wpf/Views/LoginView.xaml`
    - giu nguyen flow `Login()` hien tai, chi bo sung keyboard behavior theo chuan WPF
  - ket qua: nhan `Enter` trong form login se goi cung action voi click button
- 16. [ ] REVIEW: UI panel tai khoan o man `Khach hang` dang cat text nut `Mo tai khoan` va `Dong tai khoan`
  - pham vi user report: role `ChiNhanh` dang nhap, vao tab `Khach hang`, chon 1 dong de hien panel thao tac tai khoan phia duoi; 2 nut `Mo tai khoan` va `Dong tai khoan` bi cat text
  - nguyen nhan root cause:
    - `BankDds.Wpf/Views/CustomersView.xaml`: panel ben phai dang bi khoa `ColumnDefinition Width=\"360\"`
    - cung trong panel nay, 3 nut action dang duoc ep vao `Grid` 3 cot bang nhau, moi nut chi con khoang rong hien thi rat hep sau khi tru 2 khoang cach cot
    - `BankDds.Wpf/Resources/Styles.xaml`: `BaseButtonStyle` dang co `MinWidth=100`, `Padding=15,8`; rieng `SuccessButtonStyle` dang day `Padding=25,10`, nen tong khong gian can cho text label dai vuot qua be rong moi cot
    - ket qua la layout van giu 3 nut cung hang, nhung text bi cat do khong du be ngang de render day du
  - nhan dinh them:
    - day la regression layout sau lan doi tu `WrapPanel` sang `Grid` 3 cot bang nhau de giu 3 nut thang hang
    - bug hien tai xuat hien cu the o panel thao tac tai khoan cua man `Khach hang`; chua thay cung pattern nay o cac man khac co label dai tuong tu
  - solution fix de xuat:
    - dieu chinh layout panel tai khoan theo huong responsive thay vi khoa chat be rong 360
    - uu tien 1 trong 2 huong:
      - noi rong cot panel ben phai va giam padding/min-width cua nhom button nay de label `Mo tai khoan` / `Dong tai khoan` hien du
      - hoac giu panel rong hien tai nhung doi nhom action sang layout co kha nang wrap/xuong dong khi khong du rong
    - neu muon giu 3 nut deu nhau va khong cat text, can tinh lai tong be rong thuc te cua panel + margin + padding style, khong the chi dua vao `*` columns
  - retest sau fix:
    - role `ChiNhanh` vao `Khach hang`, chon 1 customer de mo panel tai khoan
    - xac nhan 3 nut `Mo tai khoan`, `Dong tai khoan`, `Mo lai` hien day du text, khong bi cat
    - thu resize man hinh/app o be rong thong dung de bao dam khong tai phat
  - trang thai: cho `approve` truoc khi update code
