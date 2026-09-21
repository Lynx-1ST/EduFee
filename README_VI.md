# EduFee — Hệ thống quản lý sinh viên và học phí

[English](README.md) | [Tiếng Việt](README_VI.md)

![CI](https://github.com/Lynx-1ST/EduFee/actions/workflows/ci.yml/badge.svg)

EduFee là ứng dụng desktop Windows Forms xây dựng trên .NET 10, phục vụ quản lý hồ sơ sinh viên, học kỳ, học phí, công nợ, thanh toán và biên lai tài chính. Dữ liệu vận hành được lưu trữ cục bộ bằng SQLite tại `%LocalAppData%\EduFee\edufee.db`.

---

## Tính năng

### 1. Quản lý sinh viên

- Thêm, sửa và xóa hồ sơ sinh viên với mã sinh viên, họ tên, ngày sinh, lớp, email và số điện thoại.
- Tìm kiếm theo mã sinh viên hoặc họ tên và lọc theo lớp.
- Nhập và xuất dữ liệu sinh viên bằng CSV UTF-8.
- Tách `StudentCode` khỏi khóa chính nội bộ của SQLite.

### 2. Quản lý học kỳ và học phí

- Quản lý học kỳ với ngày bắt đầu, ngày kết thúc, hạn nộp và trạng thái học kỳ hiện tại.
- Lập học phí theo số tín chỉ với đơn giá hiện hành của hệ thống.
- Hỗ trợ các chính sách miễn giảm và học bổng.
- Tự động xác định trạng thái: *Chưa nộp*, *Nộp một phần*, *Đã nộp đủ*, *Quá hạn* và *Nộp muộn*.
- Hỗ trợ lập học phí hàng loạt cho các sinh viên có cùng tên lớp.

### 3. Thanh toán và biên lai

- Ghi nhận thanh toán một phần hoặc toàn bộ.
- Cập nhật số dư học phí và tạo biên lai trong cùng một SQLite transaction nguyên tử.
- Chặn thu vượt số còn lại và ngăn sửa trực tiếp số tiền đã được chứng minh bằng biên lai.
- Lưu snapshot lịch sử bất biến trên biên lai để chứng từ không thay đổi khi hồ sơ sinh viên hoặc học phí được cập nhật sau này.
- Hỗ trợ mã chuyển khoản VietQR và MoMo Sandbox tùy chọn qua interface gateway chung.

### 4. Báo cáo và thống kê

- Thống kê tổng học phí, đã thu, còn nợ và tỷ lệ hoàn thành theo học kỳ.
- Xem công nợ theo sinh viên và thống kê tổng hợp theo lớp.
- Xuất dữ liệu sinh viên/công nợ ra CSV.
- Tạo PDF biên lai và báo cáo công nợ nhiều trang bằng pipeline GDI+ tích hợp.
- Hỗ trợ Print Preview và in biên lai qua API in của Windows.

### 5. An toàn dữ liệu và quản trị

- Lưu dữ liệu vận hành bằng SQLite với khóa ngoại và các ràng buộc tài chính.
- Quản lý schema bằng `PRAGMA user_version` với migration tuần tự đến schema v5, lưu vòng đời giao dịch gateway.
- Bảo đảm cập nhật thanh toán và tạo biên lai tuân thủ transaction ACID.
- Kiểm tra integrity và schema compatibility khi phục hồi dữ liệu.
- Tự động tạo safety backup trước khi thay thế database.
- Bảo vệ mật khẩu SMTP bằng Windows DPAPI.
- Giới hạn ứng dụng desktop ở một tiến trình đang chạy.

---

## Công nghệ

- **Ngôn ngữ / Nền tảng:** C# 14, .NET 10, Windows Forms
- **Cơ sở dữ liệu:** SQLite với `Microsoft.Data.Sqlite 10.0`
- **UI / Đồ họa:** WinForms, GDI+, Per-Monitor V2 High DPI
- **Tạo mã QR:** `QRCoder 1.8.0`
- **CI:** GitHub Actions trên `windows-latest`

---

## Yêu cầu môi trường

- Windows 10 hoặc Windows 11 (64-bit)
- .NET 10 SDK cho môi trường phát triển
- .NET Desktop Runtime 10 cho bản publish framework-dependent

---

## Build và chạy

Tại thư mục gốc của repository:

```powershell
dotnet build 26K1_DotNet.slnx
dotnet run --project 26K1_DotNet
```

---

## Demo Mode

Khởi chạy EduFee với database demo độc lập:

```powershell
dotnet run --project 26K1_DotNet -- --demo
```

Demo mode sử dụng dữ liệu tạm, tách biệt và không tác động database vận hành chính. Fixture hiện tại gồm 3 sinh viên, 1 học kỳ, 3 khoản học phí và 1 biên lai.

---

## MoMo Sandbox

Tích hợp này **chỉ dùng thanh toán thử nghiệm**, gọi API `captureWallet` và API truy vấn giao dịch tại `https://test-payment.momo.vn`. Không hỗ trợ chuyển sang Production.

- Nhập Partner Code, Access Key và Secret Key của tài khoản Sandbox trong cửa sổ cấu hình MoMo. Secret luôn được che, lưu bằng Windows DPAPI theo tài khoản Windows hiện tại; không đưa credential vào source code hoặc CI.
- Mã đơn được lưu trước khi gọi mạng. Tạo hoặc quét QR chưa làm tăng số học phí đã thu. Chỉ phản hồi truy vấn thành công, khớp giao dịch mới cập nhật học phí, tạo biên lai và đánh dấu hoàn tất trong cùng một transaction SQLite.
- Kết quả thất bại, bị hủy và hết hạn được lưu cùng mã kết quả và thời điểm kết thúc để đối soát. Trạng thái đang xử lý hoặc chưa xác định vẫn có thể kiểm tra lại; mọi trạng thái chưa thành công đều không thay đổi học phí hoặc tạo biên lai.
- Cửa sổ thanh toán kiểm tra trạng thái mỗi bốn giây. Đóng cửa sổ sẽ dừng polling, không hủy giao dịch phía MoMo. Lỗi mạng không tự đánh dấu thanh toán thất bại.
- `RedirectUrl` và `IpnUrl` mặc định là `https://localhost/` cho luồng desktop chỉ dùng query. Ứng dụng không cung cấp callback listener; trang quay về localhost sẽ không tải được. Có thể dùng URL do bạn quản lý nếu tài khoản Sandbox yêu cầu. Redirect/IPN không được dùng làm bằng chứng thanh toán.
- `--demo` luôn dùng mock. Kiểm thử tự động dùng HTTP giả lập, không cần credential hoặc kết nối MoMo.

Phần triển khai được kiểm thử offline. Kiểm chứng thanh toán Sandbox xuyên suốt vẫn cần credential hợp lệ và ví thử nghiệm MoMo của bạn. Production, tiền thật, webhook public, refund và thanh toán định kỳ nằm ngoài phạm vi.

Tài liệu provider: [API tạo thanh toán](https://developers.momo.vn/v3/vi/docs/payment/api/wallet/onetime/), [mã kết quả](https://developers.momo.vn/v3/docs/payment/api/result-handling/resultcode/), [kế hoạch tích hợp](26K1_DotNet/Docs/MOMO_SANDBOX_INTEGRATION_PLAN.md).

## Kiểm thử

Chạy self-test của ứng dụng:

```powershell
dotnet run --project 26K1_DotNet -- --test
```

Chạy toàn bộ regression/acceptance suite:

```powershell
dotnet run --project 26K1_DotNet.RegressionTests
```

Bộ regression kiểm tra các nhóm chính:

- SQLite persistence và constraints
- schema migration, rollback và retry
- migration và uniqueness của StudentCode
- tính nguyên tử của giao dịch tài chính
- receipt snapshots
- CSV import/export
- backup/restore validation
- PDF/report generation
- Luồng chuyển khoản VietQR
- Chữ ký MoMo, xác thực phản hồi, chống ghi nhận trùng và rollback/retry migration v5
- startup và một số hành vi UI

GitHub Actions cũng chạy Release build, regression suite và application self-test cho push và pull request hướng vào `main`.

---

## Release Publish

Tạo bản Release framework-dependent:

```powershell
dotnet publish 26K1_DotNet -c Release -o output/publish --self-contained false
```

Máy đích cần cài phiên bản .NET Desktop Runtime tương thích.

---

## Dữ liệu và cấu hình

### SQLite Database

Đường dẫn mặc định:

```text
%LocalAppData%\EduFee\edufee.db
```

### Chuyển đổi JSON cũ

Dữ liệu JSON legacy có thể được import bằng màn hình cấu hình database.

Chế độ dòng lệnh:

```powershell
dotnet run --project 26K1_DotNet -- --migrate
```

đọc các file JSON từ thư mục làm việc hiện tại.

Các file cần có:

```text
students.json
semesters.json
tuitionfees.json
receipts.json
```

### SMTP

Cấu hình SMTP được thiết lập trong màn hình cài đặt của ứng dụng. Mật khẩu lưu trữ được bảo vệ bằng Windows DPAPI.

---

## Kiến trúc

```text
WinForms UI
    ↓
Service Layer
    ↓
SqliteRepository
    ↓
SQLite
```

Luồng thanh toán:

```text
FormPayment
    ↓
TuitionService
    ↓
SqliteRepository
    ↓
BEGIN TRANSACTION
    ├── UPDATE TuitionFees
    └── INSERT PaymentReceipts
    ↓
COMMIT
```

Nếu xảy ra lỗi trong transaction, toàn bộ thay đổi được rollback.

---

## Tài liệu dự án

- [Roadmap phát triển](KE_HOACH_PHAT_TRIEN.md)
- [Ghi chú cải tiến và đặc tả kỹ thuật](IMPROVEMENTS.md)
- [Đặc tả thiết kế UI/UX](UI_Plan.md)
- [English README](README.md)

---

## Giới hạn phạm vi

Phạm vi lõi hiện tại duy trì EduFee như một ứng dụng desktop Windows cục bộ. Các hạng mục sau không thuộc phạm vi lõi:

- module quản lý lớp riêng
- đăng nhập/phân quyền nhiều vai trò
- Web API hoặc giao diện web
- ứng dụng mobile
- cloud synchronization
- tích hợp cổng thanh toán/ngân hàng Production hoặc chấp nhận tiền thật
- microservices
- tính năng AI
- rewrite sang WPF/WinUI
