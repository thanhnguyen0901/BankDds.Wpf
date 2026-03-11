# Checklist Refactor NGANHANG (Pass/Fail)

## 1) Mục đích
Checklist này dùng để AI kiểm tra NGANHANG sau khi refactor theo hướng QLVT:
- Tách rõ hạ tầng phân tán (setup SSMS UI) và logic runtime trong app/SP.
- Chỉ giữ SP cho nghiệp vụ runtime, không dùng SP làm cơ chế chính để dựng hạ tầng phân tán.
- Đảm bảo luồng đăng nhập, phân quyền, truy vấn liên site và báo cáo chạy đúng.

---

## 2) Quy ước chấm
- PASS: Đạt đầy đủ kỳ vọng ở cột Tiêu chí PASS.
- FAIL: Sai một phần hoặc toàn bộ kỳ vọng.
- NA: Không áp dụng do khác phạm vi nghiệp vụ (phải ghi rõ lý do).

Cột Kết quả: PASS | FAIL | NA
Cột Bằng chứng: ghi file, ảnh chụp màn hình, log, câu lệnh SQL đã chạy.

---

## 3) Điều kiện tiên quyết
| ID | Hạng mục | Bước thực hiện | Tiêu chí PASS | Kết quả | Bằng chứng | Ghi chú lỗi nếu FAIL |
|---|---|---|---|---|---|---|
| PRE-01 | Môi trường SQL | Xác nhận các SQL Server instance đã chạy | Tất cả instance cần dùng đều online |  |  |  |
| PRE-02 | SQL Server Agent | Kiểm tra SQL Server Agent trên server cần replication | Agent ở trạng thái Running |  |  |  |
| PRE-03 | DB baseline | Restore/attach DB NGANHANG theo tài liệu setup | DB tạo thành công, truy vấn SELECT 1 chạy được |  |  |  |
| PRE-04 | Quyền thao tác | Tài khoản triển khai có quyền setup replication + linked server | Không bị lỗi permission khi mở wizard/thiết lập |  |  |  |
| PRE-05 | Build app | Build project NGANHANG sau refactor | Build thành công, không lỗi compile |  |  |  |

---

## 4) Kiểm tra kiến trúc sau refactor (soi codebase)
| ID | Hạng mục | Bước thực hiện | Tiêu chí PASS | Kết quả | Bằng chứng | Ghi chú lỗi nếu FAIL |
|---|---|---|---|---|---|---|
| ARC-01 | Tách lớp hạ tầng/runtime | Rà soát code SQL/SP trong repo | Không còn luồng runtime tự tạo publication/subscription |  |  |  |
| ARC-02 | Linked server trong runtime | Rà soát SP/app | Runtime chỉ tiêu thụ linked server đã setup sẵn, không tự dựng hạ tầng hàng loạt |  |  |  |
| ARC-03 | Permission nền | Rà soát script và app startup | Không có logic startup tự cấp toàn bộ quyền hạ tầng bằng SP |  |  |  |
| ARC-04 | Tài liệu triển khai SSMS | Kiểm tra tài liệu đi kèm | Có tài liệu setup replication/linked server/mapping login đủ bước |  |  |  |
| ARC-05 | SP runtime | Liệt kê SP còn lại | SP còn lại chủ yếu là nghiệp vụ, kiểm tra mã, báo cáo, login, tạo tài khoản |  |  |  |

---

## 5) Kiểm tra setup phân tán bằng SSMS UI
| ID | Hạng mục | Bước thực hiện | Tiêu chí PASS | Kết quả | Bằng chứng | Ghi chú lỗi nếu FAIL |
|---|---|---|---|---|---|---|
| DIST-01 | Distributor | Cấu hình Distributor theo tài liệu | Tạo thành công, không lỗi wizard |  |  |  |
| DIST-02 | Publication | Tạo publication chứa đúng article cần dùng | Article đúng danh sách, có snapshot job |  |  |  |
| DIST-03 | Subscription | Tạo subscription tới site đích | Subscription active, không lỗi agent |  |  |  |
| DIST-04 | Đồng bộ ban đầu | Chạy snapshot/sync lần đầu | Dữ liệu xuất hiện ở site đích đúng số lượng |  |  |  |
| DIST-05 | Đồng bộ thay đổi | Update/Insert/Delete mẫu ở nguồn theo quy tắc hệ thống | Site đích nhận thay đổi đúng kỳ vọng |  |  |  |
| DIST-06 | Metadata phân mảnh | Chạy view danh sách phân mảnh (nếu có) | Trả đúng số site và tên server mong đợi |  |  |  |

---

## 6) Kiểm tra linked server và truy vấn liên site
| ID | Hạng mục | Bước thực hiện | Dữ liệu test | Tiêu chí PASS | Kết quả | Bằng chứng | Ghi chú lỗi nếu FAIL |
|---|---|---|---|---|---|---|---|
| LINK-01 | Kết nối LINK nội bộ | Test SELECT 1 qua từng LINK định nghĩa | LINK0/LINK1/LINK2 hoặc tên tương đương | Không lỗi login timeout/permission |  |  |  |
| LINK-02 | SP kiểm tra mã liên site | Gọi SP kiểm tra mã (NV, tài khoản, chứng từ...) | 1 mã có tồn tại ở site khác | SP trả mã trạng thái đúng (tồn tại) |  |  |  |
| LINK-03 | SP kiểm tra mã không tồn tại | Gọi lại SP với mã giả | 1 mã không tồn tại | SP trả mã trạng thái đúng (không tồn tại) |  |  |  |
| LINK-04 | Tình huống site còn lại tắt | Tạm ngắt site phụ rồi gọi SP có truy vấn liên site | 1 SP đại diện | Hệ thống báo lỗi đúng, không làm hỏng dữ liệu cục bộ |  |  |  |

---

## 7) Kiểm tra đăng nhập và phân quyền giao diện
| ID | Vai trò | Bước thực hiện | Tiêu chí PASS | Kết quả | Bằng chứng | Ghi chú lỗi nếu FAIL |
|---|---|---|---|---|---|---|
| AUTH-01 | Công ty | Đăng nhập tài khoản vai trò Công ty | Đăng nhập thành công, đọc đúng role từ DB |  |  |  |
| AUTH-02 | Công ty | Kiểm tra quyền trên UI | Chỉ có quyền xem/báo cáo/chức năng được phép theo thiết kế |  |  |  |
| AUTH-03 | Chi nhánh | Đăng nhập tài khoản vai trò Chi nhánh | Đăng nhập thành công, role đúng |  |  |  |
| AUTH-04 | Chi nhánh | Kiểm tra thao tác dữ liệu | Có thể thêm/sửa/xóa trong phạm vi cho phép |  |  |  |
| AUTH-05 | KhachHang | Đăng nhập tài khoản KhachHang | Đăng nhập thành công, role đúng |  |  |  |
| AUTH-06 | KhachHang | Kiểm tra hạn chế tạo tài khoản | Không thể vào/chạy chức năng tạo tài khoản nếu không được phép |  |  |  |
| AUTH-07 | Sai mật khẩu | Đăng nhập sai password | Bị từ chối, thông báo lỗi rõ ràng |  |  |  |
| AUTH-08 | Sai chi nhánh | Chọn chi nhánh không hợp lệ rồi đăng nhập | Bị chặn theo đúng rule nghiệp vụ |  |  |  |

---

## 8) Kiểm tra luồng tạo tài khoản runtime
| ID | Hạng mục | Bước thực hiện | Dữ liệu test | Tiêu chí PASS | Kết quả | Bằng chứng | Ghi chú lỗi nếu FAIL |
|---|---|---|---|---|---|---|---|
| ACC-01 | Tạo account hợp lệ | Gọi chức năng tạo tài khoản từ UI | User mới + role hợp lệ | Tạo thành công login/user/role |  |  |  |
| ACC-02 | Trùng login | Tạo tài khoản với login đã tồn tại | Login trùng | Bị từ chối đúng thông báo |  |  |  |
| ACC-03 | Trùng user | Tạo tài khoản với user đã tồn tại | User trùng | Bị từ chối đúng thông báo |  |  |  |
| ACC-04 | Gán role đúng | Kiểm tra role membership sau khi tạo | Tài khoản vừa tạo | Role đúng theo chọn trên UI |  |  |  |
| ACC-05 | Đăng nhập lại bằng account mới | Logout rồi login bằng account mới | Account vừa tạo | Vào được hệ thống, quyền đúng role |  |  |  |

---

## 9) Kiểm tra nghiệp vụ liên chi nhánh/site
| ID | Hạng mục | Bước thực hiện | Tiêu chí PASS | Kết quả | Bằng chứng | Ghi chú lỗi nếu FAIL |
|---|---|---|---|---|---|---|
| BIZ-01 | Kiểm tra mã toàn hệ thống | Tạo mới dữ liệu có mã unique toàn hệ thống | Chặn trùng mã nếu đã tồn tại ở site khác |  |  |  |
| BIZ-02 | Nghiệp vụ chuyển chi nhánh/site | Chạy flow chuyển dữ liệu nhân sự/chứng từ theo thiết kế | Dữ liệu chuyển đúng, trạng thái nguồn-đích đúng |  |  |  |
| BIZ-03 | Ràng buộc xóa dữ liệu | Thử xóa bản ghi đang được tham chiếu ở site khác | Bị chặn đúng theo rule nghiệp vụ |  |  |  |
| BIZ-04 | Cập nhật tồn kho/số dư liên quan | Chạy nghiệp vụ nhập/xuất hoặc tương đương của NGANHANG | Tổng hợp số liệu sau cập nhật đúng |  |  |  |

---

## 10) Kiểm tra báo cáo và truy vấn tổng hợp
| ID | Hạng mục | Bước thực hiện | Tiêu chí PASS | Kết quả | Bằng chứng | Ghi chú lỗi nếu FAIL |
|---|---|---|---|---|---|---|
| REP-01 | Báo cáo theo khoảng thời gian | Chạy báo cáo từ ngày A đến B | Dữ liệu đúng thời gian, đúng số lượng |  |  |  |
| REP-02 | Báo cáo theo vai trò | Chạy cùng báo cáo với role khác nhau | Phạm vi dữ liệu đúng theo role |  |  |  |
| REP-03 | Báo cáo có dữ liệu liên site | Chạy báo cáo cần gom dữ liệu toàn hệ | Không thiếu/không trùng bất thường |  |  |  |

---

## 11) Kiểm tra lỗi và rollback/an toàn dữ liệu
| ID | Hạng mục | Bước thực hiện | Tiêu chí PASS | Kết quả | Bằng chứng | Ghi chú lỗi nếu FAIL |
|---|---|---|---|---|---|---|
| SAFE-01 | Giao dịch lỗi giữa chừng | Cố tình gây lỗi ở bước giữa nghiệp vụ nhiều lệnh | Dữ liệu không bị nửa vời (rollback đúng) |  |  |  |
| SAFE-02 | Mất kết nối tạm thời | Ngắt kết nối DB khi đang thao tác | App báo lỗi rõ, không làm bẩn dữ liệu |  |  |  |
| SAFE-03 | Retry sau lỗi | Chạy lại nghiệp vụ sau khi khôi phục kết nối | Hệ thống chạy lại ổn định, không nhân đôi dữ liệu |  |  |  |

---

## 12) Tổng hợp kết quả phiên kiểm thử
| Nhóm | Số test | PASS | FAIL | NA | Tỷ lệ PASS |
|---|---:|---:|---:|---:|---:|
| PRE | 5 |  |  |  |  |
| ARC | 5 |  |  |  |  |
| DIST | 6 |  |  |  |  |
| LINK | 4 |  |  |  |  |
| AUTH | 8 |  |  |  |  |
| ACC | 5 |  |  |  |  |
| BIZ | 4 |  |  |  |  |
| REP | 3 |  |  |  |  |
| SAFE | 3 |  |  |  |  |
| Tổng | 43 |  |  |  |  |

---

## 13) Kết luận phiên refactor
| Trạng thái | Tiêu chí |
|---|---|
| Đạt | Không còn lỗi FAIL mức kiến trúc (ARC-01 đến ARC-05 đều PASS) và không còn lỗi FAIL mức dữ liệu/giao dịch (SAFE-01 đến SAFE-03 đều PASS). |
| Chưa đạt | Còn ít nhất 1 FAIL ở nhóm kiến trúc hoặc nhóm an toàn dữ liệu. |

Ghi chú kết luận cuối:
- Phiên test ngày: ..........
- Người/AI thực hiện: ..........
- Commit/branch kiểm thử: ..........
- Các lỗi cần sửa tiếp: ..........
