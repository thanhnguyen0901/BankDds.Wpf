# Hướng Dẫn SSMS 21: Full Flow NGANHANG

Áp dụng: SQL Server + SSMS 21  

## Tài liệu liên quan

- [Yêu cầu đề tài NGANHANG](../requirements/DE3-NGANHANG.md)
- [README dự án](../../README.md)

## 1) Quy ước instance và database

1. Publisher instance: `DESKTOP-JBB41QU` -> DB `NGANHANG`.
2. Subscriber CN1: `DESKTOP-JBB41QU\SQLSERVER2` -> DB `NGANHANG`.
3. Subscriber CN2: `DESKTOP-JBB41QU\SQLSERVER3` -> DB `NGANHANG`.
4. Subscriber Tra cứu: `DESKTOP-JBB41QU\SQLSERVER4` -> DB `NGANHANG`.

## 2) Thứ tự triển khai full flow

1. Kết nối đủ 4 instance trong SSMS 21.
2. Tạo 4 database đúng theo mapping ở mục 1.
3. Cấu hình Distributor trên Publisher.
4. Chạy bộ script SQL trên Publisher theo thứ tự chuẩn.
5. Tạo 3 Publication trên Publisher.
6. Tạo 3 Push Subscription từ Publisher đến 3 Subscriber.
7. Đẩy SP xuống Subscriber bằng Articles + Snapshot Agent.
8. Theo dõi đồng bộ trong Replication Monitor đến khi `Succeeded`.
9. Cấu hình Linked Server trên Publisher/CN1/CN2.
10. Tạo login/user/role mapping theo 3 nhóm quyền đề tài.
11. Kiểm tra nghiệm thu dữ liệu phân mảnh và quyền truy cập.

## 3) Chi tiết từng bước

### Bước 1: Kết nối 4 instance

1. Mở SSMS 21.
2. Kết nối lần lượt:
`DESKTOP-JBB41QU`, `DESKTOP-JBB41QU\SQLSERVER2`, `DESKTOP-JBB41QU\SQLSERVER3`, `DESKTOP-JBB41QU\SQLSERVER4`.
3. Xác nhận Object Explorer hiển thị đủ 4 node server.

### Bước 2: Tạo database

1. Trên `DESKTOP-JBB41QU`: tạo `NGANHANG`.
2. Trên `DESKTOP-JBB41QU\SQLSERVER2`: tạo `NGANHANG`.
3. Trên `DESKTOP-JBB41QU\SQLSERVER3`: tạo `NGANHANG`.
4. Trên `DESKTOP-JBB41QU\SQLSERVER4`: tạo `NGANHANG`.

### Bước 3: Cấu hình Distributor

Instance thao tác: `DESKTOP-JBB41QU`.

1. Chuột phải `Replication` -> `Configure Distribution...`.
2. Chọn `This server will act as its own Distributor`.
3. Snapshot folder đã dùng khi chạy thực tế:
`D:\SQLServer2022\MSSQLSERVER\MSSQL16.MSSQLSERVER\MSSQL\ReplData`.
4. Distribution database:
`distribution`.
5. Distribution data file folder:
`D:\SQLServer2022\MSSQLSERVER\MSSQL16.MSSQLSERVER\MSSQL\Data`.
6. Distribution log file folder:
`D:\SQLServer2022\MSSQLSERVER\MSSQL16.MSSQLSERVER\MSSQL\Data`.
7. Màn `Publishers`: tick `DESKTOP-JBB41QU`.
8. Màn `Wizard Actions`: chỉ tick `Configure distribution` (không tick generate script).
9. `Finish` và kiểm tra `Messages`.

Lưu ý thực tế:

1. Snapshot folder là local path (không phải UNC) sẽ hiện cảnh báo không hỗ trợ pull subscription ở Subscriber.
2. Flow tài liệu này dùng Push Subscription nên có thể tiếp tục.

Checklist verify DONE cho Bước 3:

1. Ở màn kết quả, tất cả step là `Success`.
2. Trong SSMS có database `distribution`.
3. Node `Replication` trên `DESKTOP-JBB41QU` đã ở trạng thái configured (không còn trạng thái chưa cấu hình).

### Bước 4: Chạy script SQL trên Publisher

Instance thao tác: `DESKTOP-JBB41QU`.  
Database thao tác: `NGANHANG`.

Quy trình chạy từng script (làm lần lượt, không chạy song song):

1. Trong SSMS, click đúng connection `DESKTOP-JBB41QU`.
2. Bấm `New Query`.
3. Ở combobox database (góc trên thanh công cụ query), chọn `NGANHANG`.
4. Vào `File -> Open -> File...`, mở file script cần chạy trong thư mục dự án `sql\`.
5. Kiểm tra lại lần cuối:
query đang gắn vào server `DESKTOP-JBB41QU`,
database dropdown là `NGANHANG`.
6. Bấm `Execute`.
7. Kiểm tra tab `Messages`:
không có `Error`,
có các dòng `PRINT` hoàn tất script.
8. Chỉ chuyển sang script tiếp theo khi script hiện tại chạy thành công.

Thứ tự script bắt buộc:

1. `sql/02_publisher_schema.sql`
Mục đích: tạo schema/bảng/constraint/các object nền tảng.
2. `sql/03_publisher_sp_views.sql`
Mục đích: tạo toàn bộ SP và view nghiệp vụ.
3. `sql/04_publisher_security.sql`
Mục đích: tạo role và grant execute cho SP.
4. `sql/04b_publisher_seed_data.sql` (tùy chọn)
Mục đích: nạp dữ liệu mẫu nghiệp vụ + seed account login test cho đủ role.

Checklist DONE cho Bước 4:

1. Chạy xong đủ 3 script bắt buộc đầu tiên (và script seed nếu cần demo).
2. Tab `Messages` của từng script không có lỗi SQL.
3. Sau script `03`: có message hoàn tất tạo SP/view.
4. Sau script `04`: có message hoàn tất role/grant và SP bảo mật.
5. Sau script `04b` (nếu chạy): có summary số lượng dữ liệu seed, danh sách mapping `NGUOIDUNG`, và kiểm tra nhóm `CHINHANH/KHACHHANG` thiếu `MACN`.

Mật khẩu mặc định account test sau `04b`: `Password!123`.

Lưu ý:

1. Bước này bắt buộc hoàn tất trước khi tạo Publication.
2. Nếu lỡ chọn sai database khi chạy script: dừng lại, chọn lại `NGANHANG`, chạy lại script đó.

### Bước 5: Tạo Publication

Instance thao tác: `DESKTOP-JBB41QU`.

Quy trình wizard chung (áp dụng cho cả 3 publication):

1. `Replication` -> `Local Publications` -> chuột phải `New Publication...`.
2. `Publication Database`: chọn `NGANHANG`.
3. `Publication Type`: chọn `Merge publication`.
4. `Subscriber Types`: giữ `SQL Server 2008 or later`.
5. `Articles`: tick bảng theo publication đang tạo (xem bảng cấu hình bên dưới).
6. `Filter Table Rows`:
với `PUB_BENTHANH`/`PUB_TANDINH` thì bấm `Add -> Add Filter...` để filter theo `MACN`;
với `PUB_TRACUU` thì không cần add filter.
7. Trong popup `Add Filter` (chỉ cho BT/TD):
nhập điều kiện `WHERE [MACN] = N'...'`;
chọn `A row from this table will go to multiple subscriptions`.
8. `Snapshot Agent`: giữ tick `Create a snapshot immediately`.
9. `Agent Security` -> `Security Settings...`:
chọn `Run under the SQL Server Agent service account`;
`Connect to the Publisher` = `By impersonating the process account`.
10. `Wizard Actions`: giữ `Create the publication`.
11. `Complete the Wizard`: nhập đúng tên publication, bấm `Finish`.
12. Ở màn kết quả, tất cả action phải `Success`.

Cấu hình từng publication:

1. `PUB_BENTHANH`
Articles:
`CHINHANH`, `KHACHHANG`, `NHANVIEN`, `TAIKHOAN`, `GD_GOIRUT`, `GD_CHUYENTIEN`.
Không tick:
`NGUOIDUNG`, `Stored Procedures`, `Views`.
Filter cho toàn bộ 6 bảng trên:
`WHERE [MACN] = N'BENTHANH'`.
Trong tất cả popup `Add Filter`:
chọn `multiple subscriptions`.

2. `PUB_TANDINH`
Articles:
giống `PUB_BENTHANH`.
Filter cho toàn bộ 6 bảng trên:
`WHERE [MACN] = N'TANDINH'`.
Trong tất cả popup `Add Filter`:
chọn `multiple subscriptions`.

3. `PUB_TRACUU`
Articles:
`CHINHANH`, `KHACHHANG`.
Không filter:
`CHINHANH` và `KHACHHANG` đều để toàn bộ dữ liệu.
Nếu lỡ mở `Add Filter` ở publication này:
bấm `Cancel` để không lưu filter.

Lỗi thường gặp và xử lý:

1. Lỗi:
`Article 'X' cannot be published ... partition_options = 3`.
Nguyên nhân:
đã chọn `A row ... will go to only one subscription`.
2. Cách xử lý:
xóa publication bị lỗi và tạo lại, chọn `multiple subscriptions` cho các bảng có filter.
3. Cảnh báo FILESTREAM (`Error 22522`):
chỉ là warning, không phải nguyên nhân chính của lỗi add article trong case này.

Checklist DONE cho Bước 5:

1. Trong `Local Publications` có đủ:
`PUB_BENTHANH`, `PUB_TANDINH`, `PUB_TRACUU`.
2. `Properties` -> `Articles` của mỗi publication đúng danh sách bảng như thiết kế.
3. `Properties` -> `Filter Rows...`:
BT/TD có filter `MACN` đúng chi nhánh.
4. `PUB_TRACUU` không có filter cho `CHINHANH`, `KHACHHANG`.

### Bước 6: Tạo Push Subscription

Instance thao tác: `DESKTOP-JBB41QU`.

Flow đã thao tác thực tế cho `PUB_BENTHANH` (làm mẫu):

1. `Replication` -> `Local Publications` -> chuột phải `PUB_BENTHANH` -> `New Subscriptions...`.
2. Màn `Publication`: giữ đúng `PUB_BENTHANH` -> `Next`.
3. Màn `Merge Agent Location`: chọn
`Run all agents at the Distributor ... (push subscriptions)` -> `Next`.
4. Màn `Subscribers`:
bấm `Add SQL Server Subscriber...`,
connect tới `DESKTOP-JBB41QU\SQLSERVER2`,
nếu lỗi cert không trust thì tick `Trust server certificate` rồi connect lại.
5. Sau khi add xong:
tick dòng `DESKTOP-JBB41QU\SQLSERVER2`,
`Subscription Database` chọn `NGANHANG`,
dòng `DESKTOP-JBB41QU` (publisher) để không tick -> `Next`.
6. Màn `Merge Agent Security`:
bấm `...` và chọn:
`Run under the SQL Server Agent service account`,
`Connect to the Publisher and Distributor` = `By impersonating the process account`,
`Connect to the Subscriber` = `By impersonating the process account` -> `OK` -> `Next`.
7. Màn `Synchronization Schedule`:
giữ `Run on demand only` -> `Next`.
8. Màn `Initialize Subscriptions`:
giữ `Initialize` = tick, `Initialize When` = `Immediately` -> `Next`.
9. Màn `Subscription Type`:
giữ mặc định `Server`, priority `75.00` -> `Next`.
10. Màn `Wizard Actions` (nếu có):
giữ `Create the subscription(s)` -> `Next`.
11. Màn `Complete`:
bấm `Finish`.
12. Màn kết quả:
`Creating subscription` = `Success`,
`Starting Snapshot Agent` = `Success`,
`Starting the synchronization agent(s)` có thể `Warning` nếu snapshot chưa sẵn.

Mapping bắt buộc:

1. `PUB_BENTHANH` -> Subscriber `DESKTOP-JBB41QU\SQLSERVER2`, DB `NGANHANG`.
2. `PUB_TANDINH` -> Subscriber `DESKTOP-JBB41QU\SQLSERVER3`, DB `NGANHANG`.
3. `PUB_TRACUU` -> Subscriber `DESKTOP-JBB41QU\SQLSERVER4`, DB `NGANHANG`.

Áp dụng lại y hệt flow trên cho 2 publication còn lại:

1. `PUB_TANDINH` map sang `DESKTOP-JBB41QU\SQLSERVER3` + DB `NGANHANG`.
2. `PUB_TRACUU` map sang `DESKTOP-JBB41QU\SQLSERVER4` + DB `NGANHANG`.

Lưu ý quan trọng:

1. Trên cả 3 subscriber phải có sẵn database `NGANHANG` trước khi tạo subscription.
2. Nếu wizard báo lỗi connect/security, kiểm tra lại `Distribution Agent Security` và quyền truy cập snapshot folder.
3. Nếu đã tạo dở subscription sai mapping, xóa subscription đó rồi tạo lại đúng publication-instance đích.
4. Nếu wizard báo warning:
`Subscriptions ... cannot be initialized immediately because the snapshot is not available`
thì đây là cảnh báo thường gặp khi snapshot chưa sinh xong, không phải lỗi tạo subscription.
5. Cách xử lý warning snapshot chưa sẵn sàng:
`Local Publications` -> chọn publication tương ứng -> `View Snapshot Agent Status` -> `Start`,
đợi snapshot `Succeeded`,
sau đó mở `Replication Monitor` để chạy lại/đợi Merge Agent đồng bộ.

Checklist DONE cho Bước 6:

1. Trong mỗi publication, mục `Subscriptions` thấy đúng subscriber instance theo mapping.
2. `Replication Monitor` thấy đủ 3 subscription.
3. Trạng thái job subscription không còn lỗi đỏ sau khi chạy snapshot/merge đầu tiên.

### Bước 7: Đẩy SP xuống Subscriber

Instance thao tác: `DESKTOP-JBB41QU`.

Mục tiêu:
đưa các stored procedure đã tạo ở Publisher xuống Subscriber thông qua Articles + Snapshot.

Thực hiện lần lượt cho từng publication:
`PUB_BENTHANH`, `PUB_TANDINH`, `PUB_TRACUU`.

Quy trình chi tiết cho 1 publication:

1. Vào `Replication` -> `Local Publications`.
2. Chuột phải publication cần cấu hình -> `Properties`.
3. Chọn trang `Articles`.
4. Bỏ tick `Show only checked articles in the list`.
5. Mở node `Stored Procedures`.
6. Tick toàn bộ SP trong node `Stored Procedures` (đã thực hiện thực tế cho cả 3 publication).
7. Bấm `OK` để lưu thay đổi Articles.
8. Chuột phải lại node publication đó (không chuột phải node subscription con) -> `View Snapshot Agent Status`.
9. Bấm `Start` để sinh snapshot mới chứa phần schema SP vừa thêm.
10. Đợi trạng thái Snapshot Agent là `Succeeded`.

Lưu ý:

1. Mỗi lần thay đổi Articles, cần chạy lại Snapshot Agent cho publication tương ứng.
2. Nếu Snapshot Agent lỗi `Access to the path ... is denied`, cấp quyền NTFS cho thư mục `ReplData`:
`NT SERVICE\\SQLSERVERAGENT` (bắt buộc) và `NT SERVICE\\MSSQLSERVER` (khuyến nghị),
quyền `Modify` hoặc `Full control`, áp dụng cho folder + subfolders + files.
3. Nếu subscription báo chưa initialize được vì snapshot chưa sẵn, đợi Snapshot Agent hoàn tất rồi đồng bộ lại trong Replication Monitor.

Checklist DONE cho Bước 7:

1. Cả 3 publication đã tick SP trong `Properties` -> `Articles`.
2. Snapshot Agent của cả 3 publication chạy `Succeeded` sau khi thêm SP.
3. Trên Subscriber tương ứng có thể nhìn thấy SP mới trong:
`Databases` -> `NGANHANG` -> `Programmability` -> `Stored Procedures`.
4. Không còn lỗi quyền snapshot path khi chạy lại Snapshot Agent.

### Bước 8: Theo dõi đồng bộ

Instance thao tác: `DESKTOP-JBB41QU`.

Mục tiêu:
xác nhận cả 3 publication đã tạo snapshot thành công và mỗi subscription đã có ít nhất 1 lần merge thành công.

Phân biệt trạng thái để tránh hiểu nhầm:

1. `Uninitialized subscription`: chưa chạy merge thành công lần đầu, cần bấm `Start`.
2. `Not synchronizing`: không phải lỗi nếu lịch đang là `Run on demand only`; đây là trạng thái nghỉ sau khi job chạy xong.
3. `Completed` ở tab `Agents` (job `Snapshot Agent`): snapshot đã sinh thành công.
4. `Applied the snapshot and merged ... conflict(s)` trong cửa sổ `View Synchronization Status`: lần sync đó thành công.

Quy trình kiểm tra chuẩn (lặp cho từng publication):

1. Mở `Replication` -> chuột phải `Launch Replication Monitor`.
2. Ở cây trái: `My Publishers` -> `DESKTOP-JBB41QU`.
3. Chọn publication cần kiểm tra:
`[NGANHANG]: PUB_BENTHANH`, rồi `PUB_TANDINH`, rồi `PUB_TRACUU`.
4. Tab `Agents`:
đảm bảo job `Snapshot Agent` có `Status = Completed` (hoặc `Succeeded`).
5. Tab `All Subscriptions`:
kiểm tra đúng subscriber mapping, không nhầm publication.
6. Nếu đang thấy `Uninitialized subscription`:
chuột phải subscription -> `View Synchronization Status` -> `Start`.
7. Chờ cửa sổ sync báo thành công:
`Applied the snapshot and merged ... 0 conflict(s)` hoặc `The merge process was successful`.
8. Đóng cửa sổ sync, quay lại `Replication Monitor` và bấm `Refresh`.
9. Nếu sau đó trạng thái thành `Not synchronizing` với `Delivery Rate = 0 rows/sec`:
coi như bình thường trong mode `Run on demand only`.

Mapping cần kiểm tra trong monitor:

1. `PUB_BENTHANH` <-> `DESKTOP-JBB41QU\SQLSERVER2`.
2. `PUB_TANDINH` <-> `DESKTOP-JBB41QU\SQLSERVER3`.
3. `PUB_TRACUU` <-> `DESKTOP-JBB41QU\SQLSERVER4`.

Lỗi thường gặp và xử lý nhanh:

1. `cannot be initialized immediately because the snapshot is not available`:
chạy lại `Snapshot Agent` của publication đó, đợi `Succeeded`, rồi chạy lại Merge Agent.
2. `Uninitialized subscription` trong tab `All Subscriptions`:
chuột phải subscription tương ứng -> `View Synchronization Status` -> `Start`,
đợi đồng bộ xong rồi `Refresh` Replication Monitor.
3. `Access to the path ... is denied`:
kiểm tra quyền thư mục snapshot `ReplData` cho `NT SERVICE\\SQLSERVERAGENT` (và `NT SERVICE\\MSSQLSERVER`).
4. Lỗi kết nối/certificate khi add subscriber hoặc chạy agent:
bật `Trust server certificate` cho kết nối instance liên quan.
5. Cửa sổ sync đứng lâu ở `Connecting to Subscriber ...`:
đổi `Subscriber connection` của subscription sang `Using SQL Server login` (ví dụ `sa` trong môi trường lab), rồi chạy lại sync.
6. `Not synchronizing`:
không phải lỗi nếu agent schedule là `Run on demand only`; chỉ cần xác nhận lần sync gần nhất đã thành công.

Checklist DONE cho Bước 8:

1. Snapshot Agent của `PUB_BENTHANH`, `PUB_TANDINH`, `PUB_TRACUU` đều `Succeeded`.
2. Mỗi subscription đã chạy `View Synchronization Status -> Start` ít nhất 1 lần và báo thành công.
3. Mapping publication-subscriber đúng:
BT->SQLSERVER2, TD->SQLSERVER3, TRACUU->SQLSERVER4.
4. Trạng thái `Not synchronizing` được chấp nhận nếu bạn dùng `Run on demand only`.

### Bước 9: Cấu hình Linked Server

Mục tiêu: các SP phân tán gọi được dữ liệu liên server.

### 9.1 Mapping phải tạo đúng

Trên `DESKTOP-JBB41QU` (Publisher):
1. `LINK0` -> `DESKTOP-JBB41QU\SQLSERVER4`.
2. `LINK1` -> `DESKTOP-JBB41QU\SQLSERVER2`.
3. `LINK2` -> `DESKTOP-JBB41QU\SQLSERVER3`.

Trên `DESKTOP-JBB41QU\SQLSERVER2`:
1. `LINK0` -> `DESKTOP-JBB41QU\SQLSERVER4`.
2. `LINK1` -> `DESKTOP-JBB41QU\SQLSERVER3`.

Trên `DESKTOP-JBB41QU\SQLSERVER3`:
1. `LINK0` -> `DESKTOP-JBB41QU\SQLSERVER4`.
2. `LINK1` -> `DESKTOP-JBB41QU\SQLSERVER2`.

### 9.2 Làm trước khi tạo

1. Đảm bảo bạn connect được thủ công vào cả 4 instance trong Object Explorer.
2. Đảm bảo các instance đích (`SQLSERVER2`, `SQLSERVER3`, `SQLSERVER4`) đang `Running`.
3. Nếu đã tồn tại `LINK0/LINK1/LINK2` cũ và mapping sai:
xóa link sai trước rồi tạo lại đúng tên.

### 9.3 Quy trình tạo 1 linked server trong SSMS (UI)

1. Click đúng instance cần cấu hình link.
2. Mở `Server Objects` -> `Linked Servers`.
3. Chuột phải `Linked Servers` -> `New Linked Server...`.
4. Tab `General`:
`Linked server` = tên theo mapping (`LINK0`, `LINK1`, `LINK2`);
`Server type` = `Other data source`;
`Provider` = `Microsoft OLE DB Driver for SQL Server (MSOLEDBSQL)`;
`Product name` = để trống;
`Data source`:
ưu tiên dùng tên instance đích theo mapping (ví dụ `DESKTOP-JBB41QU\SQLSERVER4`);
nếu test fail (`Error 53/1225`) thì đổi sang `tcp:<HOST>,<PORT>` (ví dụ `tcp:DESKTOP-JBB41QU,61000`).
`Provider string`:
khuyến nghị `Encrypt=No;TrustServerCertificate=Yes` (hoặc để trống nếu driver không chấp nhận một số thuộc tính).
`Catalog` = để trống.
5. Tab `Security` (khuyến nghị cho lab):
chọn `Be made using this security context`;
nhập SQL login có quyền trên instance đích (ví dụ `sa`).
6. Tab `Server Options`:
`Data Access` = `True`;
`RPC` = `True`;
`RPC Out` = `True`.
7. Bấm `OK`.
8. Nếu popup báo
`The linked server has been created but failed a connection test...`:
chọn `Yes` để giữ linked server,
sau đó kiểm tra lại bằng `sp_testlinkedserver`.

Ghi chú:
Nếu môi trường của bạn dùng Windows login liên-instance ổn định, có thể chọn pass-through.
Nếu từng gặp lỗi treo/không auth khi gọi remote SP, ưu tiên SQL login cố định như trên.
Với máy nhiều named instance, cách ổn định nhất là `Data source = tcp:HOST,PORT` sau khi đã xác nhận connect thành công bằng SSMS.
Thực tế lab: để `Product name` trống cho kết quả ổn định hơn so với nhập `SQL Server`.

### 9.4 Thứ tự thực hiện khuyến nghị

1. Cấu hình trên Publisher `DESKTOP-JBB41QU` đủ 3 link: `LINK0`, `LINK1`, `LINK2`.
2. Cấu hình trên `DESKTOP-JBB41QU\SQLSERVER2`: `LINK0`, `LINK1`.
3. Cấu hình trên `DESKTOP-JBB41QU\SQLSERVER3`: `LINK0`, `LINK1`.
4. Sau mỗi instance, chạy test ngay rồi mới chuyển instance kế tiếp.

### 9.5 Script kiểm tra nhanh sau khi tạo

Kiểm tra endpoint trước khi tạo link (khuyến nghị cho mọi máy):

1. Thử connect SSMS bằng instance name: `HOST\SQLSERVERX`.
2. Nếu lỗi mạng/timeout, thử connect bằng TCP:
`tcp:HOST,PORT`.
3. Chỉ dùng endpoint nào connect thành công để điền vào `Data source` của linked server.

Mẹo tìm PORT đang lắng nghe của named instance:

1. `SQL Server Configuration Manager` -> `Protocols for SQLSERVERX` -> `TCP/IP` -> tab `IP Addresses` -> `IPAll`:
đọc `TCP Dynamic Ports` hoặc `TCP Port`.
2. Hoặc xem `SQL Server Error Log` của instance, tìm dòng `Server is listening on ...`.

Script mẫu tạo linked server (chỉnh `@server` và `@datasrc` theo mapping):

```sql
USE master;
GO

IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'LINK0')
    EXEC sp_dropserver @server = N'LINK0', @droplogins = 'droplogins';
GO

EXEC sp_addlinkedserver
    @server     = N'LINK0',
    @srvproduct = N'',
    @provider   = N'MSOLEDBSQL',
    @datasrc    = N'DESKTOP-JBB41QU\SQLSERVER4',
    @provstr    = N'Encrypt=No;TrustServerCertificate=Yes';
GO

EXEC sp_addlinkedsrvlogin
    @rmtsrvname  = N'LINK0',
    @useself     = N'False',
    @locallogin  = NULL,
    @rmtuser     = N'sa',
    @rmtpassword = N'<SA_PASSWORD>';
GO

EXEC sp_serveroption N'LINK0', N'data access', N'true';
EXEC sp_serveroption N'LINK0', N'rpc',         N'true';
EXEC sp_serveroption N'LINK0', N'rpc out',     N'true';
GO
```

Chạy trên từng instance vừa cấu hình:

```sql
SELECT
    name,
    data_source,
    is_rpc_out_enabled,
    is_rpc_enabled
FROM sys.servers
WHERE is_linked = 1
ORDER BY name;
```

Kiểm tra kết nối từng link:

1. `EXEC master.dbo.sp_testlinkedserver N'LINK0';`
2. `EXEC master.dbo.sp_testlinkedserver N'LINK1';`
3. Riêng Publisher chạy thêm:
`EXEC master.dbo.sp_testlinkedserver N'LINK2';`

Test đọc dữ liệu thật (trên Publisher, ví dụ `LINK1`):

```sql
SELECT TOP 5 *
FROM [LINK1].[NGANHANG].dbo.CHINHANH;
```

### 9.6 Lỗi thường gặp và xử lý nhanh

1. `Login failed` khi test linked server:
vào lại tab `Security`, dùng SQL login có quyền trên instance đích.
2. `RPC Out` disabled khi gọi SP remote:
vào `Server Options` bật `RPC Out = True`.
3. `Could not connect` tới instance đích:
kiểm tra lại endpoint trong `Data source`:
thử chuyển từ `HOST\INSTANCE` sang `tcp:HOST,PORT`;
đảm bảo service instance đích đang chạy và `TCP/IP` đã enable.
4. Tạo đúng tên nhưng trỏ sai server:
xóa linked server sai và tạo lại đúng `data source`.
5. Wizard báo fail test nhưng `sp_testlinkedserver` pass:
coi như linked server hoạt động được, tiếp tục bước kế tiếp.

Checklist DONE cho Bước 9:

1. Publisher có đủ `LINK0`, `LINK1`, `LINK2` đúng mapping.
2. `SQLSERVER2` có `LINK0`, `LINK1` đúng mapping.
3. `SQLSERVER3` có `LINK0`, `LINK1` đúng mapping.
4. `sp_testlinkedserver` chạy thành công cho tất cả link bắt buộc.

### Bước 10: Security + Authz Workflow theo đề tài

Mục tiêu:
đồng bộ SQL login/role và mapping `NGUOIDUNG` để app chạy đúng workflow trong `DE3-NGANHANG_AUTHZ_WORKFLOW.md`.

### 10.1 Điều kiện đầu vào

1. Da chay xong cac script o Buoc 4 tren Publisher `DESKTOP-JBB41QU`, DB `NGANHANG`.
2. Bootstrap security one-time tren subscriber:
- Ket noi `SQLSERVER2`, `SQLSERVER3`, `SQLSERVER4`.
- Chay `sql/04_publisher_security.sql` tren moi instance dich (DB `NGANHANG`).
3. Neu ban da dung moi truong tu ban cu, chay lai dung thu tu:
- `sql/03_publisher_sp_views.sql`
- `sql/04_publisher_security.sql`
4. App login dung `username/password` SQL, sau do goi `sp_DangNhap` de lay session (`TENNHOM`, `MACN`, `CustomerCMND`, `EmployeeId`).

### 10.2 Rule phân quyền chốt theo đề

1. Có 3 role:
- `NGANHANG`
- `CHINHANH`
- `KHACHHANG`
2. Rule tạo account:
- account `NGANHANG` chỉ tạo được account `NGANHANG`.
- account `CHINHANH` tạo được account `CHINHANH` và `KHACHHANG`.
- account `KHACHHANG` không được tạo account.
3. Rule scope:
- `NGANHANG`: xem báo cáo theo chi nhánh được chọn.
- `CHINHANH`: tác nghiệp trong đúng chi nhánh của account.
- `KHACHHANG`: chỉ xem sao kê của chính mình.

### 10.3 NGUOIDUNG là mapping bắt buộc cho app

`NGUOIDUNG` dùng để map context nghiệp vụ cho session app, phải có trên Publisher:

1. `Username`: trùng SQL login.
2. `UserGroup`: `0=NganHang`, `1=ChiNhanh`, `2=KhachHang`.
3. `DefaultBranch`: mã chi nhánh (`BENTHANH`/`TANDINH`).
4. `EmployeeId`: bắt buộc với `ChiNhanh`.
5. `CustomerCMND`: bắt buộc với `KhachHang`.

Ghi chú:
2 DB phân mảnh (`SQLSERVER2`, `SQLSERVER3`) không cần bảng `NGUOIDUNG` để app login;
nguồn xác thực + mapping session nằm ở Publisher.
Security login/user/role tren subscriber duoc sync tu dong qua linked server khi goi SP quan tri account.

### 10.4 Luồng tạo account đúng chuẩn

Ưu tiên dùng app module `Quản trị` (đã bám rule ở trên):

1. Login bằng account quản trị hợp lệ.
2. Vào tab `Quản trị` -> `Thêm người dùng`.
3. Chọn đúng nhóm theo quyền người tạo:
- `NGANHANG` chỉ thấy chọn `NganHang`.
- `CHINHANH` thấy chọn `ChiNhanh` hoặc `KhachHang`.
4. Nhập field bắt buộc theo nhóm:
- `ChiNhanh`: nhập `EmployeeId`.
- `KhachHang`: nhập `CustomerCMND`.
5. `DefaultBranch`:
- với `CHINHANH` bị khóa theo chi nhánh đang đăng nhập.
- với `NGANHANG` cho phép chọn.
6. Bấm `Ghi`.

Khi lưu, app gọi theo chuỗi:
1. `sp_TaoTaiKhoan` (tao SQL login + DB user + role tren Publisher, dong thoi sync login/user/role sang subscriber qua `LINK0/1/2`).
2. `USP_AddUser` (ghi/upsert mapping `NGUOIDUNG`).

Khi doi mat khau hoac xoa account:
1. `sp_DoiMatKhau` tu sync mat khau xuong subscriber.
2. `sp_XoaTaiKhoan` tu xoa login/user tren subscriber.
### 10.5 Tạo bằng SQL (khi cần kiểm thử thủ công)

```sql
USE NGANHANG;
GO

-- 1) Tao login + role SQL
EXEC dbo.sp_TaoTaiKhoan
    @LOGIN   = N'KH_TEST01',
    @PASS    = N'Test@123',
    @TENNHOM = N'KHACHHANG';
GO

-- 2) Tao mapping app session
EXEC dbo.USP_AddUser
    @Username      = N'KH_TEST01',
    @PasswordHash  = N'N/A-SQL-AUTH',
    @UserGroup     = 2,
    @DefaultBranch = N'BENTHANH',
    @CustomerCMND  = N'0800100001',
    @EmployeeId    = NULL;
GO
```

### 10.6 Query kiểm tra nhanh sau khi tạo

```sql
USE NGANHANG;
GO

-- Role SQL
SELECT dp.name AS UserName, rp.name AS RoleName
FROM sys.database_role_members drm
JOIN sys.database_principals dp ON dp.principal_id = drm.member_principal_id
JOIN sys.database_principals rp ON rp.principal_id = drm.role_principal_id
WHERE dp.name IN (N'ADMIN_NH', N'NV_BT', N'KH_DEMO', N'KH_TEST01')
ORDER BY dp.name;

-- Mapping NGUOIDUNG
SELECT Username, UserGroup, DefaultBranch, CustomerCMND, EmployeeId, TrangThaiXoa
FROM dbo.NGUOIDUNG
WHERE Username IN (N'ADMIN_NH', N'NV_BT', N'KH_DEMO', N'KH_TEST01')
ORDER BY Username;
```

### 10.7 Kiểm tra login/session đúng workflow

Đăng nhập bằng từng account và chạy:

```sql
USE NGANHANG;
GO
EXEC dbo.sp_DangNhap;
SELECT IS_MEMBER('NGANHANG') AS IsNganHang,
       IS_MEMBER('CHINHANH') AS IsChiNhanh,
       IS_MEMBER('KHACHHANG') AS IsKhachHang;
```

Kỳ vọng:
1. `TENNHOM` trả đúng role của account.
2. `MACN` trả đúng theo `NGUOIDUNG.DefaultBranch` (hoặc mapping nghiệp vụ tương ứng).
3. Account `KHACHHANG` phải có `CustomerCMND`.
4. Account `CHINHANH` phải có `EmployeeId`.

### 10.8 Lỗi thường gặp và xử lý nhanh

1. `Login failed for user ...`:
login chưa tồn tại ở instance đang kết nối hoặc sai mật khẩu.
2. `EXECUTE permission denied`:
user chưa vào đúng role SQL (`NGANHANG`/`CHINHANH`/`KHACHHANG`).
3. Tạo login thành công nhưng app không login được theo role:
thiếu mapping `NGUOIDUNG` hoặc mapping sai (`UserGroup/DefaultBranch/CMND/EmployeeId`).
4. `CHINHANH` tạo account khác chi nhánh:
bị chặn theo rule; kiểm tra lại `DefaultBranch` và session branch người tạo.
5. Tao account tren Publisher xong nhung app van `Login failed` khi di qua tab nghiep vu:
kiem tra Buoc 9 (LINK0/1/2), kiem tra `sp_SyncSecurityToSubscribers` va grant role tren subscriber.

Checklist DONE cho Bước 10:

1. `sp_DangNhap`, `sp_TaoTaiKhoan`, `USP_AddUser` chạy được.
2. Rule tạo account đúng:
- `NGANHANG -> NGANHANG`
- `CHINHANH -> CHINHANH/KHACHHANG`
- `KHACHHANG -> không được tạo`
3. Mỗi account có đủ role SQL + mapping `NGUOIDUNG` tương ứng.
4. Login app dựng đúng session theo role/scope trong đề.
5. Account moi tao tren Publisher dung duoc ngay tren tab nghiep vu (khong can chay tay script sync tren subscriber).

### Bước 11: Kiểm tra nghiệm thu cuối

1. Trên Publisher có đủ 3 publication.
2. Có đủ 3 push subscription đúng mapping.
3. Agent trạng thái `Succeeded`.
4. Subscriber nhận đúng dữ liệu phân mảnh.
5. SP có mặt trên các DB subscriber theo publication tương ứng.
6. Login theo từng nhóm chạy đúng quyền.

## 4) Khi nào phải chạy lại script/SP

1. Sửa cấu trúc bảng hoặc ràng buộc: chạy lại `02_publisher_schema.sql` trên Publisher.
2. Sửa SP/view nghiệp vụ: chạy lại `03_publisher_sp_views.sql` trên Publisher.
3. Sua grant/role hoac SP security: chay lai `04_publisher_security.sql` tren Publisher, sau do bootstrap lai tren subscriber neu can.
4. Muốn nạp lại dữ liệu demo: chạy `04b_publisher_seed_data.sql`.
5. Sau khi thay đổi SP cần phát tán: vào Publication tick lại Articles SP và chạy Snapshot Agent.

## 5) Đối chiếu chức năng đề tài và SP chính

1. Cập nhật khách hàng:
`SP_AddCustomer`, `SP_UpdateCustomer`, `SP_DeleteCustomer`, `SP_RestoreCustomer`, `SP_GetCustomersByBranch`.
2. Mở và quản lý tài khoản:
`SP_AddAccount`, `SP_UpdateAccount`, `SP_CloseAccount`, `SP_ReopenAccount`, `SP_GetAccountsByCustomer`.
3. Cập nhật nhân viên và chuyển chi nhánh:
`SP_AddEmployee`, `SP_UpdateEmployee`, `SP_DeleteEmployee`, `SP_RestoreEmployee`, `SP_TransferEmployee`.
4. Giao dịch gửi/rút/chuyển:
`SP_Deposit`, `SP_Withdraw`, `SP_CrossBranchTransfer`.
5. Báo cáo sao kê:
`SP_GetAccountStatement` (trả số dư đầu kỳ + danh sách giao dịch có RunningBalance).
6. Liệt kê tài khoản mở trong kỳ:
`SP_GetAccountsOpenedInPeriod` (có tham số chi nhánh hoặc toàn hệ).
7. Liệt kê khách hàng theo chi nhánh/tất cả, sắp xếp họ tên:
`SP_GetCustomersByBranch` (ORDER BY `HO`, `TEN`).
8. Phân quyền đăng nhập:
`sp_DangNhap`, `sp_TaoTaiKhoan`, nhóm role `NGANHANG`/`CHINHANH`/`KHACHHANG`.
