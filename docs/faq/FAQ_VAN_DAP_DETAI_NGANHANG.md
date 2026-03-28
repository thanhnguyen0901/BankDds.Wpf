# FAQ Vấn Đáp Đề Tài NGÂN HÀNG

Tài liệu này tổng hợp các câu hỏi thường gặp khi làm, demo và chuẩn bị vấn đáp đề tài `NGÂN HÀNG`.
Nội dung được viết lại theo đúng domain của đề tài này và bám theo các tài liệu nhóm đang dùng:

- `docs/requirements/DE3-NGANHANG.md`
- `docs/requirements/DE3-NGANHANG_AUTHZ_WORKFLOW.md`
- `docs/sql/HUONG_DAN_SSMS21_FULLFLOW_NGANHANG.md`

# [**FAQ**](#faq)

FAQ là tập hợp các câu hỏi thường gặp trong quá trình làm đồ án và chuẩn bị thi vấn đáp.
Phần này gồm:

- các câu hỏi dễ gây nhầm khi triển khai đề tài
- các câu hỏi lý thuyết giảng viên có thể hỏi trong buổi vấn đáp

## [**1. Câu Hỏi Dễ Nhầm**](#1-cau-hoi-de-nham)

Đây là nhóm các câu hỏi mà sinh viên rất dễ bị nhầm khi làm đề tài `NGÂN HÀNG`.

***
> **Câu hỏi 1:** Một khách hàng chỉ đăng ký ở một chi nhánh. Vậy khách hàng đó có được mở tài khoản ở chi nhánh khác không?
>
> **Đáp:** Có.
>
> **Giải thích:**  
> `KHACHHANG.MACN` là chi nhánh đăng ký gốc của khách hàng.  
> `TAIKHOAN.MACN` là chi nhánh mở tài khoản.  
> Theo đề tài, một khách hàng chỉ đăng ký ở một chi nhánh nhưng có thể có nhiều tài khoản ở nhiều chi nhánh khác nhau.

***
> **Câu hỏi 2:** Vì sao màn khách hàng ở chi nhánh Bến Thành vẫn có thể mở tài khoản ở Tân Định?
>
> **Đáp:** Vì tài khoản được mở theo chi nhánh đang thao tác, không bị buộc phải trùng với chi nhánh đăng ký gốc của khách hàng.

***
> **Câu hỏi 3:** `SỐ DƯ` có được sửa trực tiếp trên form tài khoản không?
>
> **Đáp:** Không.
>
> **Giải thích:**  
> `SỐ DƯ` chỉ được thay đổi qua các nghiệp vụ:
> - Gửi tiền
> - Rút tiền
> - Chuyển tiền
>
> Nếu cho sửa trực tiếp `SỐ DƯ` thì sẽ phá business rule và làm sai sao kê.

***
> **Câu hỏi 4:** Server tra cứu `SQLSERVER4` dùng để làm gì?
>
> **Đáp:** Dùng để phục vụ tra cứu liên chi nhánh.
>
> **Giải thích:**  
> Đây là subscriber tra cứu nhận publication `PUB_TRACUU`.  
> Người dùng không đăng nhập trực tiếp vào “chi nhánh 3”.  
> Server này được dùng gián tiếp qua các chức năng tra cứu hoặc stored procedure.

***
> **Câu hỏi 5:** Server tra cứu có những dữ liệu nào?
>
> **Đáp:** Theo flow hiện tại của nhóm, subscriber tra cứu nhận các article phục vụ tra cứu liên chi nhánh như:
> - `CHINHANH`
> - `KHACHHANG`

***
> **Câu hỏi 6:** Khi không tìm thấy dữ liệu ở phân mảnh hiện tại thì có nên nhảy lần lượt qua phân mảnh khác để tìm không?
>
> **Đáp:** Không nên.
>
> **Giải thích:**  
> Với đề tài này, tra cứu liên chi nhánh nên đi qua publisher hoặc subscriber tra cứu theo kiến trúc đã chốt.  
> Nếu viết theo kiểu “site 1 -> site 2 -> site 3” thì dễ lỗi khi một site trung gian bị down.

***
> **Câu hỏi 7:** Viết stored procedure ở đâu là hợp lý?
>
> **Đáp:** Nên viết ở publisher trước.
>
> **Giải thích:**  
> Publisher là nơi quản lý schema/SP gốc.  
> Sau đó dùng publication, article, snapshot để phát tán xuống subscriber cần dùng.

***
> **Câu hỏi 8:** Nếu sửa stored procedure trên publisher thì subscriber có tự có ngay không?
>
> **Đáp:** Không phải lúc nào cũng có ngay.
>
> **Giải thích:**  
> Sau khi sửa SP cần:
> - đảm bảo SP nằm trong `Articles`
> - chạy lại `Snapshot Agent`
> - chạy `Distribution Agent` hoặc `reinitialize subscription` nếu cần

***
> **Câu hỏi 9:** Nếu sửa stored procedure trên subscriber thì có đồng bộ ngược về publisher không?
>
> **Đáp:** Không.
>
> **Giải thích:**  
> Với flow hiện tại, schema/SP được quản lý từ publisher và phát tán xuống subscriber.  
> Không có chiều đồng bộ ngược từ subscriber về publisher.

***
> **Câu hỏi 10:** Vì sao login `KháchHàng` bắt buộc phải có `CustomerCMND`?
>
> **Đáp:** Vì app phải map login SQL sang danh tính nghiệp vụ của khách hàng.
>
> **Giải thích:**  
> `CustomerCMND` là khóa dùng để:
> - lấy đúng danh sách tài khoản của khách
> - giới hạn sao kê đúng tài khoản của chính khách đó

***
> **Câu hỏi 11:** Vì sao login `ChiNhánh` bắt buộc phải có `EmployeeId`?
>
> **Đáp:** Vì giao dịch phải gắn với đúng nhân viên đang thao tác.
>
> **Giải thích:**  
> `EmployeeId` được dùng để:
> - audit người lập giao dịch
> - tự động gán `MANV` vào nghiệp vụ
> - không cho người dùng sửa tay `MANV`

***
> **Câu hỏi 12:** `NgânHàng` có được CRUD dữ liệu nghiệp vụ như `ChiNhánh` không?
>
> **Đáp:** Không.
>
> **Giải thích:**  
> `NgânHàng` trong đề tài này chủ yếu:
> - xem báo cáo theo chi nhánh
> - tra cứu liên chi nhánh
> - quản trị account/login theo scope đã chốt
>
> Không phải là nhóm tác nghiệp CRUD hằng ngày.

***
> **Câu hỏi 13:** `ChiNhánh` có được CRUD nhân viên trong chi nhánh mình không?
>
> **Đáp:** Có.
>
> **Giải thích:**  
> Rule đã chốt là `ChiNhánh` toàn quyền tác nghiệp trong chi nhánh đang đăng nhập, gồm:
> - thêm
> - xóa mềm
> - phục hồi
> - sửa
> - chuyển chi nhánh

***
> **Câu hỏi 14:** `ChiNhánh` có được sửa hoặc xóa login của người khác không?
>
> **Đáp:** Không theo flow hiện tại.
>
> **Giải thích:**  
> `ChiNhánh` chỉ dùng màn tạo người dùng theo mode create-only.  
> Không phải full admin như `NgânHàng`.

***
> **Câu hỏi 15:** Vì sao màn của `ChiNhánh` được đổi từ “Quản trị” sang “Tạo người dùng”?
>
> **Đáp:** Để diễn đạt đúng quyền thật của role này.
>
> **Giải thích:**  
> Backend vẫn đúng từ trước, nhưng tên “Quản trị” dễ gây hiểu nhầm rằng `ChiNhánh` có quyền sửa/xóa toàn bộ login.

***
> **Câu hỏi 16:** Vì sao ở báo cáo sao kê, `KháchHàng` dùng dropdown còn `ChiNhánh`/`NgânHàng` lại nhập `SOTK`?
>
> **Đáp:** Vì nghiệp vụ của hai nhóm khác nhau.
>
> **Giải thích:**  
> `KháchHàng` chỉ xem tài khoản của chính mình nên dropdown là hợp lý.  
> `ChiNhánh` và `NgânHàng` cần lập sao kê cho tài khoản bất kỳ trong scope cho phép nên textbox nhập `SOTK` rõ nghiệp vụ hơn.

***
> **Câu hỏi 17:** Vì sao form đăng nhập chỉ có Bến Thành và Tân Định mà không có server tra cứu?
>
> **Đáp:** Vì server tra cứu không phải là một chi nhánh nghiệp vụ để đăng nhập trực tiếp.

***
> **Câu hỏi 18:** Nếu `PUB_TRACUU` thiếu một stored procedure mới thì biểu hiện lỗi sẽ như thế nào?
>
> **Đáp:** Các chức năng tra cứu liên chi nhánh có thể báo:
> - `Could not find stored procedure ...`
> - hoặc lỗi quyền nếu grant chưa được đẩy xuống subscriber

***
> **Câu hỏi 19:** Tại sao phải thống nhất tên linked server giữa các site?
>
> **Đáp:** Vì stored procedure viết ở publisher rồi đẩy xuống subscriber cần dùng cùng một tên LINK.
>
> **Giải thích:**  
> Nếu mỗi nơi đặt tên khác nhau thì SP mang xuống site khác sẽ lỗi.

***
> **Câu hỏi 20:** Nếu gặp lỗi `Cannot open database ... requested by the login` thì xử lý sao?
>
> **Đáp:** Kiểm tra lại:
> - tên server
> - tên database
> - login SQL
> - connection string
> - login đã được map vào database user chưa

***
> **Câu hỏi 21:** Sau khi sửa `03_publisher_sp_views.sql`, có cần chạy lại `04_publisher_security.sql` không?
>
> **Đáp:** Nếu có thêm SP mới hoặc thay đổi grant thì nên chạy lại.

***
> **Câu hỏi 22:** Vì sao phải có bảng `NGUOIDUNG` trong khi SQL đã có login/user/role rồi?
>
> **Đáp:** Vì app cần thêm mapping nghiệp vụ.
>
> **Giải thích:**  
> SQL login/user/role chỉ giải quyết phần xác thực và quyền database.  
> App còn cần:
> - `UserGroup`
> - `DefaultBranch`
> - `EmployeeId`
> - `CustomerCMND`

## [**2. Interview Questions**](#2-interview-questions)

Đây là phần tổng hợp các câu hỏi lý thuyết mà thầy có thể hỏi trong buổi thi vấn đáp cuối kỳ.
Các câu trả lời đã được điều chỉnh để phù hợp với đề tài `NGÂN HÀNG`.

***
> **Câu hỏi 1:** Sau khi phân tán xong thì có một trường dữ liệu là `rowguid`. Vậy `rowguid` được sinh ra để làm gì?
>
> **Đáp:**  
> `rowguid` hỗ trợ quá trình đồng bộ dữ liệu trong merge replication.  
> Nó giúp SQL Server định danh duy nhất từng dòng khi dữ liệu được đồng bộ giữa publisher và subscriber.

***
> **Câu hỏi 2:** `Login Name` là gì? `Username` là gì?
>
> **Đáp:**  
> `Login Name` là tài khoản dùng để đăng nhập vào SQL Server instance.  
> Ví dụ: `sa`, `ADMIN_NH`, `NV_BT`, `KH_DEMO`.
>
> `Username` là user trong một database cụ thể.  
> Ví dụ trong database `NGANHANG`, vào:
> - `Security -> Users`
> sẽ thấy các user làm việc trong database đó.
>
> Login và user thường được map với nhau qua `sid`.

***
> **Câu hỏi 3:** `db_datareader`, `db_datawriter`, `db_securityadmin`, `db_accessadmin`, `db_owner` là gì?
>
> **Đáp:**  
> Đây là các role/quyền ở mức database.
>
> - `db_datareader`: chỉ đọc dữ liệu
> - `db_datawriter`: ghi dữ liệu
> - `db_securityadmin`: quản lý quyền, role ở mức database
> - `db_accessadmin`: quản lý quyền truy cập database
> - `db_owner`: quyền cao nhất trong database
>
> Với đề tài này, nhóm chủ yếu dùng role nghiệp vụ riêng:
> - `NGANHANG`
> - `CHINHANH`
> - `KHACHHANG`

***
> **Câu hỏi 4:** Có hai cách để viết stored procedure: viết ở server gốc rồi phát tán, hoặc viết thủ công từng nơi. Cách nào hiệu quả hơn?
>
> **Đáp:**  
> Viết ở server gốc rồi phát tán hiệu quả hơn.
>
> **Giải thích:**  
> Nếu sửa SP ở publisher thì có thể rollout xuống subscriber theo publication/article/snapshot.  
> Nếu viết tay từng nơi thì rất dễ lệch phiên bản.

***
> **Câu hỏi 5:** Nếu sửa stored procedure trên phân mảnh thì có đồng bộ về các phân mảnh khác và server gốc không?
>
> **Đáp:** Sai.
>
> **Giải thích:**  
> Với flow nhóm đang làm, schema/SP được quản lý một chiều từ publisher xuống subscriber.  
> Không có chiều ngược lại.

***
> **Câu hỏi 6:** Sửa dữ liệu tại server gốc thì phân mảnh có nhận được không? Và ngược lại?
>
> **Đáp:**  
> Với merge replication, dữ liệu nghiệp vụ có thể đồng bộ hai chiều theo publication đã cấu hình.
>
> **Lưu ý:**  
> Dữ liệu có thể đồng bộ hai chiều, nhưng schema/SP/grant không nên hiểu là tự đồng bộ hai chiều giống dữ liệu.

***
> **Câu hỏi 7:** Nêu ưu và nhược điểm khi ưu tiên tìm kiếm trên site phân mảnh trước khi về site chủ.
>
> **Đáp:**  
> **Ưu điểm:**
> - có thể giảm tải cho site chủ nếu dữ liệu nằm ngay ở site cục bộ
>
> **Nhược điểm:**
> - logic phức tạp hơn
> - dễ lỗi nếu site trung gian bị down
> - dễ làm sai phân quyền nếu đi vòng sang site khác không đúng kiến trúc
>
> Với đề tài `NGÂN HÀNG`, nhóm ưu tiên:
> - dùng publisher cho báo cáo toàn hệ thống
> - dùng subscriber tra cứu cho tra cứu liên chi nhánh

***
> **Câu hỏi 8:** Muốn thực thi một stored procedure, view, function trong code thì làm thế nào?
>
> **Đáp:**  
> Với stored procedure, tạo `SqlCommand`, set `CommandType = StoredProcedure`, truyền tham số rồi thực thi.  
> Với view hoặc function, có thể dùng câu lệnh SQL phù hợp rồi đọc dữ liệu qua `SqlDataReader` hoặc adapter/repository tương ứng.

***
> **Câu hỏi 9:** Giao tác là gì? Để viết giao tác phân tán cần bật dịch vụ gì?
>
> **Đáp:**  
> Giao tác là một dãy thao tác đọc/ghi dữ liệu cần được đảm bảo nhất quán.  
> Để viết giao tác phân tán cần dịch vụ:
> - `MSDTC` (`Microsoft Distributed Transaction Coordinator`)

***
> **Câu hỏi 10:** Ý nghĩa của `BEGIN TRAN`, `COMMIT`, `ROLLBACK`, `BEGIN DISTRIBUTED TRAN` là gì?
>
> **Đáp:**  
> - `BEGIN TRAN`: bắt đầu giao tác
> - `COMMIT`: xác nhận thành công
> - `ROLLBACK`: hủy giao tác, trả dữ liệu về trạng thái trước đó
> - `BEGIN DISTRIBUTED TRAN`: mở đầu giao tác phân tán

***
> **Câu hỏi 11:** Nêu những tính chất của giao tác.
>
> **Đáp:** Có 4 tính chất ACID:
>
> 1. **Atomicity**: hoặc tất cả cùng thành công, hoặc tất cả cùng thất bại  
> 2. **Consistency**: dữ liệu luôn thỏa các ràng buộc  
> 3. **Isolation**: giao tác đang chạy không làm lộ trạng thái chưa commit  
> 4. **Durability**: đã commit thì dữ liệu bền vững

***
> **Câu hỏi 12:** Dữ liệu rác là gì?
>
> **Đáp:**  
> Là dữ liệu chưa commit nhưng bị giao tác khác đọc nhầm.  
> Đây là một ví dụ của hiện tượng `dirty read`.

***
> **Câu hỏi 13:** Có mấy loại giao tác?
>
> **Đáp:**  
> Có 2 nhóm lớn:
> - giao tác tập trung
> - giao tác phân tán
>
> Giao tác tập trung có:
> - giao tác phẳng
> - giao tác lồng
>
> Giao tác phân tán trong thực hành thường là giao tác phẳng trên nhiều site.

***
> **Câu hỏi 14:** `XACT_ABORT` là gì? Nhận mấy giá trị?
>
> **Đáp:**  
> `XACT_ABORT` là tùy chọn ảnh hưởng cách SQL xử lý lỗi trong transaction.  
> Nó có 2 giá trị:
> - `ON`
> - `OFF`
>
> - `OFF`: có thể bỏ qua lệnh lỗi và tiếp tục  
> - `ON`: gặp lỗi thì hủy toàn bộ transaction

***
> **Câu hỏi 15:** Khi viết stored procedure, khi nào không cần `BEGIN TRAN` mà vẫn được coi là một giao tác?
>
> **Đáp:**  
> Nếu chỉ có một câu lệnh `INSERT`, `UPDATE` hoặc `DELETE` đơn lẻ thì SQL Server vẫn xử lý nó như một giao tác ngầm định.
>
> Nếu có nhiều bước phụ thuộc nhau thì nên dùng transaction tường minh.

***
> **Câu hỏi 16:** Dịch vụ `MSDTC` là gì?
>
> **Đáp:**  
> `MSDTC` là `Microsoft Distributed Transaction Coordinator`.  
> Nó đảm bảo các cập nhật trên nhiều server hoặc cùng thành công, hoặc cùng rollback.

***
> **Câu hỏi 17:** Vị từ thích hợp là gì?
>
> **Đáp:**  
> Là vị từ thỏa:
> - tính đầy đủ
> - tính cực tiểu

***
> **Câu hỏi 18:** Tiêu chí đầy đủ và tiêu chí cực tiểu là gì?
>
> **Đáp:**  
> - **Đầy đủ**: phân mảnh tạo ra phải đủ để biểu diễn toàn bộ dữ liệu cần thiết  
> - **Cực tiểu**: không tạo ra phân mảnh thừa, mỗi phân mảnh phải có ý nghĩa sử dụng

***
> **Câu hỏi 19:** Vị từ là gì? Một vị từ đơn giản là gì?
>
> **Đáp:**  
> Vị từ là điều kiện logic dùng để phân mảnh hoặc chọn dữ liệu.  
> Ví dụ vị từ đơn giản trong đề tài này:
> - `MACN = 'BENTHANH'`
> - `MACN = 'TANDINH'`

***
> **Câu hỏi 20:** Một vị từ “thích hợp” là gì?
>
> **Đáp:**  
> Là vị từ mà khi dùng nó để tạo ra phân mảnh thì phân mảnh đó thực sự được các truy vấn hoặc stored procedure sử dụng.

***
> **Câu hỏi 21:** Sự trong suốt phân tán là gì?
>
> **Đáp:**  
> Là khi người dùng không cần biết dữ liệu đang nằm ở đâu mà vẫn thao tác bình thường.
>
> Với app:
> - người dùng ở mức trong suốt cao
>
> Với người lập trình:
> - vẫn phải biết publication, subscriber, linked server, branch scope

***
> **Câu hỏi 22:** Có mấy mức độ trong suốt phân tán?
>
> **Đáp:** Có 4 mức độ:
>
> 1. Không cần chỉ ra phân mảnh  
> 2. Trong suốt vị trí  
> 3. Trong suốt ánh xạ cục bộ  
> 4. Không trong suốt
>
> Người dùng cuối ở mức cao hơn.  
> Người lập trình thường làm ở mức thấp hơn vì phải biết nơi dữ liệu thực sự nằm.

***
> **Câu hỏi 23:** Điều kiện để có thể phân tán cơ sở dữ liệu là gì? Có mấy bước chính?
>
> **Đáp:**  
> Điều kiện quan trọng:
> - có `SQL Server Agent`
> - có `Distributor`
> - có publication/subscription
>
> Các bước lớn:
> 1. Định nghĩa `Distributor`
> 2. Định nghĩa `Publication`
> 3. Định nghĩa `Subscription`

***
> **Câu hỏi 24:** Có mấy hình thức phân mảnh?
>
> **Đáp:** Có 3 hình thức:
> - phân mảnh ngang
> - phân mảnh dọc
> - phân mảnh hỗn hợp

***
> **Câu hỏi 25:** Nêu đặc điểm của phân mảnh ngang.
>
> **Đáp:**  
> Phân mảnh ngang là chia quan hệ theo các bộ.
>
> Trong đề tài `NGÂN HÀNG`, ví dụ đơn giản:
> - dữ liệu nghiệp vụ của `BENTHANH`
> - dữ liệu nghiệp vụ của `TANDINH`
>
> với vị từ:
> - `MACN = 'BENTHANH'`
> - `MACN = 'TANDINH'`

***
> **Câu hỏi 26:** Nêu đặc điểm của phân mảnh dọc.
>
> **Đáp:**  
> Phân mảnh dọc là chia quan hệ theo cột nhưng vẫn phải đảm bảo khóa chính để tái thiết dữ liệu.

***
> **Câu hỏi 27:** Nêu đặc điểm của phân mảnh hỗn hợp.
>
> **Đáp:**  
> Là sự kết hợp giữa phân mảnh ngang và phân mảnh dọc.

***
> **Câu hỏi 28:** Có mấy quy tắc phân mảnh?
>
> **Đáp:** Có 3 quy tắc chính:
> - tính đầy đủ
> - tính tái thiết
> - tính tách biệt

***
> **Câu hỏi 29:** `Run continuously` khác `Run on demand` ở điểm nào?
>
> **Đáp:**  
> - `Run continuously`: đồng bộ gần như liên tục, nhất quán cao hơn
> - `Run on demand`: tự quản cao hơn, thích hợp khi các site có thể tạm disconnect

> Trong môi trường lab của repo này, nếu muốn giả lập gần `Run continuously` trên SSMS qua `SQL Server Agent`, có thể cấu hình job `Merge Agent` với lịch:
> - `Recurring`
> - `Daily`
> - `Occurs every 1 minute`
> - `Starting at 12:00:00 AM`
> - `Ending at 11:59:59 PM`
> - `No end date`
>
> Đây là cấu hình nhanh, ổn định hơn so với thử `1 second`.

***
> **Câu hỏi 30:** `Snapshot folder` là gì?
>
> **Đáp:**  
> Là thư mục trung gian chứa dữ liệu/schema snapshot để phát tán từ publisher đến subscriber.  
> Thường nên là shared folder/network path khi cấu hình thực tế.

***
> **Câu hỏi 31:** Tại sao `sp_DangNhap` phải trả về `EmployeeId`?
>
> **Đáp:**  
> Để app tự gán đúng `MANV` vào các nghiệp vụ giao dịch và audit.

***
> **Câu hỏi 32:** Tại sao `sp_DangNhap` phải trả về `CustomerCMND`?
>
> **Đáp:**  
> Để app xác định đúng khách hàng đang đăng nhập và giới hạn sao kê/tài khoản theo chính khách đó.

***
> **Câu hỏi 33:** Bốn thuộc tính quan trọng của ComboBox là gì?
>
> **Đáp:**  
> - `ItemsSource` hoặc `DataSource`
> - `DisplayMemberPath` hoặc `DisplayMember`
> - `SelectedValuePath` hoặc `ValueMember`
> - `SelectedItem` hoặc `SelectedValue`

***
> **Câu hỏi 34:** Vì sao phải dùng remote login hoặc linked server?
>
> **Đáp:**  
> Để cho phép truy cập dữ liệu hoặc thực thi logic khi đang đứng từ server này sang server khác trong hệ phân tán.

***
> **Câu hỏi 35:** Có những cách nào để tối ưu truy vấn?
>
> **Đáp:** Một số cách cơ bản:
> - chọn và chiếu trước, kết sau
> - khử bớt phép kết nếu có thể
> - tránh lặp điều kiện
> - sắp xếp điều kiện hợp lý trong `AND`/`OR`
> - tạo index đúng các cột lọc/join

***
> **Câu hỏi 36:** Stored procedure trong suốt là gì?
>
> **Đáp:**  
> Là stored procedure mà khi thực thi ở các site cần dùng thì vẫn cho kết quả đúng mà người dùng không cảm nhận được mình đang thao tác trên hệ phân tán.
>
> Điều kiện để viết SP trong suốt tốt hơn:
> - tên database thống nhất
> - tên linked server thống nhất
> - rollout SP đúng từ publisher xuống subscriber

***
> **Câu hỏi 37:** Trong database, cái nào là nhân bản, cái nào là phân hoạch?
>
> **Đáp:**  
> Trong đề tài này:
> - dữ liệu phục vụ tra cứu toàn hệ thống ở subscriber tra cứu có tính nhân bản theo publication
> - dữ liệu nghiệp vụ theo chi nhánh mang tính phân hoạch/phân mảnh theo `MACN`

***
> **Câu hỏi 38:** Nếu đã phân tán xong mà muốn đổi cấu trúc bảng hoặc thêm SP mới thì làm sao?
>
> **Đáp:**  
> Làm ở publisher trước:
> - sửa schema hoặc SP
> - chạy script ở publisher
> - cập nhật publication/articles nếu cần
> - chạy snapshot/distribution để phát tán xuống subscriber

***
> **Câu hỏi 39:** Dữ liệu sau khi nhập trên form sẽ được đẩy về đâu?
>
> **Đáp:**  
> Theo flow của nhóm:
> - nghiệp vụ được xử lý trên publisher hoặc site phù hợp
> - sau đó replication đồng bộ xuống subscriber tương ứng

***
> **Câu hỏi 40:** Trong các table, cái nào mang tính đầy đủ, cái nào vi phạm tính tách biệt?
>
> **Đáp:**  
> Các table nhân bản phục vụ tra cứu sẽ vi phạm tính tách biệt theo nghĩa dữ liệu có mặt ở nhiều nơi.  
> Các table phân mảnh theo chi nhánh được thiết kế để đảm bảo tính đầy đủ khi ghép lại.

***
> **Câu hỏi 41:** Giao tác tập trung và giao tác phân tán giống và khác nhau thế nào?
>
> **Đáp:**  
> **Giống:** đều có tính chất ACID  
> **Khác:**  
> - giao tác tập trung chạy trong một môi trường CSDL tập trung
> - giao tác phân tán chạy trên nhiều site/server

***
> **Câu hỏi 42:** `Login Name` nằm trong bảng nào?
>
> **Đáp:**  
> Trong hệ catalog của SQL Server ở mức server, ví dụ các view hệ thống về principals/logins.

***
> **Câu hỏi 43:** Tại sao biết user liên kết với login nào?
>
> **Đáp:**  
> Vì giữa login và user có liên hệ qua `sid`.

***
> **Câu hỏi 44:** Tên nhóm quyền nằm ở đâu?
>
> **Đáp:**  
> Nằm trong các database principal/role của SQL Server.  
> Với đề tài này, nhóm dùng các role:
> - `NGANHANG`
> - `CHINHANH`
> - `KHACHHANG`

***
> **Câu hỏi 45:** Ưu và nhược điểm của nhân bản là gì?
>
> **Đáp:**  
> **Ưu điểm:** truy xuất nhanh, thuận tiện cho tra cứu  
> **Nhược điểm:** rollout cập nhật schema/SP/grant phức tạp hơn, dữ liệu sao chép nhiều nơi

***
> **Câu hỏi 46:** Ưu và nhược điểm của phân hoạch/phân mảnh là gì?
>
> **Đáp:**  
> **Ưu điểm:** dữ liệu tác nghiệp theo chi nhánh rõ ràng, giảm scope xử lý cục bộ  
> **Nhược điểm:** truy vấn toàn hệ thống và đồng bộ phức tạp hơn

***
> **Câu hỏi 47:** Tại sao table đó phải nhân bản?
>
> **Đáp:**  
> Với đề tài `NGÂN HÀNG`, dữ liệu phục vụ tra cứu toàn hệ thống được đưa xuống subscriber tra cứu để:
> - tra cứu liên chi nhánh
> - không bắt người dùng cuối phải đăng nhập thẳng vào publisher

## [**3. Ghi Nhớ Nhanh Trước Buổi Vấn Đáp**](#3-ghi-nho-nhanh-truoc-buoi-van-dap)

1. Nhớ rõ 3 role:
   - `NganHang`
   - `ChiNhanh`
   - `KhachHang`

2. Nhớ rõ business rule quan trọng nhất:
   - 1 khách hàng đăng ký ở 1 chi nhánh
   - 1 khách hàng có thể có nhiều tài khoản ở nhiều chi nhánh
   - `KhachHang` chỉ xem sao kê tài khoản của chính mình

3. Nhớ mapping 4 instance:
   - Publisher: `DESKTOP-JBB41QU`
   - CN1: `SQLSERVER2`
   - CN2: `SQLSERVER3`
   - Tra cứu: `SQLSERVER4`

4. Nhớ 3 publication:
   - `PUB_BENTHANH`
   - `PUB_TANDINH`
   - `PUB_TRACUU`

5. Nhớ câu trả lời rất hay bị hỏi:
   - Sửa SP xong thì phải làm gì?
   - Trả lời: sửa ở publisher, kiểm tra articles, chạy snapshot/distribution để đẩy xuống subscriber

6. Nhớ `sp_DangNhap` trả về:
   - `TENNHOM`
   - `MACN`
   - `EmployeeId`
   - `CustomerCMND`

7. Nhớ các script quan trọng:
   - `02_publisher_schema.sql`
   - `03_publisher_sp_views.sql`
   - `04_publisher_security.sql`
   - `04b_publisher_seed_data.sql`
