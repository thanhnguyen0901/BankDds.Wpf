# Phân tích QLVT để cập nhật đề tài NGANHANG

## 1) Mục đích tài liệu
Tài liệu này dùng làm đầu vào cho AI khi kiểm tra và chỉnh sửa project NGANHANG theo đúng bản chất triển khai của đề tài QLVT.

Mục tiêu cuối cùng:
- NGANHANG không dùng Stored Procedure (SP) để thay cho thao tác triển khai hạ tầng phân tán.
- Phần hạ tầng phân tán được setup bằng giao diện SQL Server Management Studio (SSMS): Replication, Subscription, Linked Server, Agent, mapping login.
- SP tập trung cho nghiệp vụ runtime của ứng dụng: đăng nhập, tạo tài khoản, kiểm tra mã, báo cáo, kiểm tra ràng buộc liên site.

---

## 2) Kết luận đã kiểm chứng từ code + SQL script + README của QLVT

## 2.1 Bản chất cách làm hiện tại của QLVT
QLVT đang triển khai theo mô hình sau:
1. Dùng Replication trong SSMS để triển khai phân tán vật lý dữ liệu (Publisher/Subscriber).
2. Dùng Linked Server (LINK0, LINK1, LINK2) để SP nghiệp vụ truy vấn/kiểm tra dữ liệu chéo site.
3. Ứng dụng WinForms gọi SP để xử lý runtime, không thấy cơ chế dùng SP để tự dựng publication/subscription trong codebase.

## 2.2 Các điểm đúng đã có bằng chứng
1. README mô tả rõ quy trình đẩy article/SP qua Replication UI (Articles, Snapshot Agent, SQL Server Agent).
2. Có view lấy danh sách phân mảnh từ metadata replication (sysmergepublications, sysmergesubscriptions).
3. Nhiều SP nghiệp vụ có truy vấn LINK0/LINK1/LINK2, đúng kiểu truy vấn phân tán lúc chạy.
4. Form đăng nhập và tạo tài khoản trong WinForms đang gọi sp_DangNhap và sp_TaoTaiKhoan.

## 2.3 Các điểm cần hiểu đúng để tránh suy diễn quá mức
1. Trong repo không thấy script tạo publication/subscription kiểu sp_addpublication hoặc sp_addsubscription.
2. Tuy nhiên, repo không thể tự chứng minh toàn bộ cấu hình SSMS ngoài môi trường chạy thật; phần này vẫn là bước triển khai thủ công theo README.
3. Vì vậy, kết luận chính xác là: dự án được thiết kế để vận hành trên hạ tầng phân tán đã setup trước bằng SSMS, còn app chỉ tiêu thụ hạ tầng đó.

---

## 3) Cách QLVT xử lý đăng nhập và phân quyền

## 3.1 Đối tượng cốt lõi
1. sp_DangNhap
- Nhận login name.
- Trả về mã nhân viên, họ tên, nhóm quyền.
- UI dựa vào kết quả này để bật/tắt chức năng theo vai trò.

2. sp_TaoTaiKhoan
- Tạo login.
- Tạo user trong DB.
- Gán role.
- Xử lý tình huống trùng login/user.

3. view_DanhSachPhanManh
- Cấp danh sách chi nhánh/server cho combobox chọn chi nhánh.

4. view_DanhSachNhanVien
- Lấy nhân viên chưa có login để cấp tài khoản.

## 3.2 Nguyên tắc kiến trúc cần giữ
1. Hạ tầng phân tán và hạ tầng quyền hệ thống: setup ở SSMS.
2. SP runtime: xử lý nghiệp vụ và policy thao tác dữ liệu khi chương trình đang chạy.
3. Không gom toàn bộ hạ tầng phân tán vào SP runtime.

---

## 4) Định hướng đối chiếu cho NGANHANG
Thông tin đầu bài bạn cung cấp: NGANHANG hiện đang thiên về hướng "mọi thứ đều qua SP" (kể cả setup phân tán, link server, permission account).

Nếu kiểm tra codebase NGANHANG xác nhận đúng như trên, thì đây là các sai lệch so với hướng QLVT:
1. Sai lệch 1: dùng SP như công cụ triển khai hạ tầng phân tán thay vì dùng SSMS UI.
2. Sai lệch 2: không tách rạch ròi lớp hạ tầng (deployment) và lớp runtime (ứng dụng/SP nghiệp vụ).
3. Sai lệch 3: dễ làm flow vận hành khó kiểm soát, khó chấm đúng "bản chất đề" khi bảo vệ.

---

## 5) Mục tiêu migration cho NGANHANG

## 5.1 Tách 2 lớp rõ ràng
1. Lớp hạ tầng phân tán (ngoài app, thao tác SSMS)
- Distributor/Publisher/Subscriber.
- Publication/Subscription.
- SQL Server Agent/Snapshot Agent.
- Linked Server + mapping login.
- Các quyền nền tảng cần thiết.

2. Lớp ứng dụng + SP runtime
- Đăng nhập qua sp_DangNhap.
- Tạo tài khoản qua sp_TaoTaiKhoan.
- SP nghiệp vụ truy vấn liên site thông qua LINK đã có sẵn.
- Bật/tắt chức năng UI theo role.

## 5.2 Những gì cần loại bỏ hoặc đổi hướng trong NGANHANG
1. Loại bỏ vai trò "SP triển khai publication/subscription" (nếu có).
2. Loại bỏ vai trò "SP dựng linked server hàng loạt" như cơ chế chính.
3. Loại bỏ tư duy "toàn bộ permission account setup bằng SP runtime".
4. Giữ lại SP nghiệp vụ cần thiết cho luồng chạy thực tế.

## 5.3 Những gì cần bổ sung
1. Tài liệu triển khai SSMS cho NGANHANG
- Các bước setup replication.
- Các bước setup linked server.
- Các bước mapping login/role.
- Checklist xác minh đồng bộ dữ liệu.

2. Chuẩn hóa SP/runtime theo khung QLVT
- sp_DangNhap.
- sp_TaoTaiKhoan.
- view_DanhSachPhanManh (nếu UI cần chọn chi nhánh).
- view_DanhSachNhanVien (nếu có nghiệp vụ cấp account theo nhân viên).

3. Chỉnh code ứng dụng
- Login đọc role từ DB.
- UI phân quyền theo role.
- Chọn chi nhánh từ nguồn dữ liệu phân mảnh phù hợp.

---

## 6) Tiêu chí "Done" khi sửa NGANHANG
NGANHANG đạt hướng đúng khi đồng thời thỏa các điều kiện:
1. Có tài liệu setup SSMS đầy đủ, có thể dựng lại trên máy mới.
2. App không tự tạo hạ tầng phân tán (publication/subscription/linked server) trong runtime.
3. Luồng login + role-based UI hoạt động đúng và kiểm thử được.
4. SP chủ yếu phục vụ nghiệp vụ và kiểm tra liên site ở runtime.
5. Có checklist test end-to-end: login theo từng role, chuyển chi nhánh, truy vấn liên site, báo cáo.

---

## 7) Prompt gợi ý để đưa cho AI ở project NGANHANG
Bạn có thể đưa nguyên văn prompt sau:

"Hãy đọc kỹ file PHAN_TICH_QLVT_DE_CAP_NHAT_NGANHANG.md rồi kiểm tra toàn bộ codebase NGANHANG theo đúng bản chất QLVT.

Yêu cầu bắt buộc:
1) Phân loại tất cả thành 2 nhóm: (A) hạ tầng phân tán phải setup bằng SSMS UI, (B) logic runtime nên giữ trong app/SP.
2) Chỉ ra chỗ nào NGANHANG đang làm đúng, chỗ nào đang sai so với hướng QLVT.
3) Nếu phát hiện SP đang làm nhiệm vụ hạ tầng (publication/subscription/linked server/permission nền), hãy đánh dấu cụ thể file + đoạn code + lý do sai.
4) Đề xuất phương án thay thế: bỏ logic đó khỏi runtime, chuyển thành tài liệu thao tác SSMS từng bước.
5) Chuẩn hóa lại luồng đăng nhập, phân quyền và tạo tài khoản theo mô hình sp_DangNhap + sp_TaoTaiKhoan + role-based UI.
6) Sau khi sửa, cung cấp:
- danh sách file đã đổi,
- lý do đổi từng file,
- checklist test end-to-end,
- rủi ro còn lại và cách kiểm chứng."

---

## 8) Ghi chú cho người review
1. Trọng tâm review không phải "SP chạy được hay không", mà là "đúng bản chất kiến trúc đề tài phân tán hay không".
2. Ưu tiên soi các dấu hiệu sau:
- Còn logic dựng hạ tầng phân tán trong runtime không.
- Luồng role có tách bạch và kiểm thử được không.
- Tài liệu setup SSMS có đủ để người khác dựng lại từ đầu không.
3. Kết luận cuối cùng phải dựa trên bằng chứng trong code + tài liệu, tránh suy đoán theo thói quen triển khai cá nhân.
