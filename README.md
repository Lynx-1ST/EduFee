# EduFee — Hệ thống quản lý sinh viên và học phí

EduFee là ứng dụng desktop Windows Forms xây dựng trên nền tảng .NET 10, phục vụ công tác quản lý hồ sơ sinh viên, biểu phí học kỳ, theo dõi công nợ, thu tiền và phát hành biên lai tài chính. Dữ liệu vận hành được lưu trữ cục bộ bằng cơ sở dữ liệu SQLite tại `%LocalAppData%\EduFee\edufee.db`.

---

## Tính năng chính

### 1. Quản lý sinh viên
- Quản lý danh sách sinh viên: Thêm mới, chỉnh sửa, xóa hồ sơ sinh viên với đầy đủ thông tin (Mã SV, Họ tên, Ngày sinh, Lớp, Email, Số điện thoại).
- Tìm kiếm nhanh theo mã sinh viên, họ tên hoặc lọc theo lớp học.
- Nhập và xuất dữ liệu danh sách sinh viên qua tệp CSV chuẩn Unicode UTF-8.

### 2. Quản lý học kỳ và học phí
- Quản lý danh mục học kỳ, cấu hình ngày bắt đầu, kết thúc, hạn nộp học phí và kích hoạt học kỳ hiện tại.
- Lập học phí theo số lượng tín chỉ với đơn giá hiện hành của hệ thống, hỗ trợ các chính sách miễn giảm học phí.
- Tự động xác định và cập nhật trạng thái thanh toán: *Chưa nộp*, *Nộp 1 phần*, *Đã nộp đủ*, *Quá hạn*, *Nộp muộn* (hoàn thành sau hạn nộp).
- Hỗ trợ lập học phí hàng loạt cho toàn bộ sinh viên thuộc cùng một lớp.

### 3. Thu phí và phát hành biên lai
- Ghi nhận thanh toán học phí theo từng lần nộp (nộp một phần hoặc thanh toán toàn bộ).
- Cơ chế giao dịch nguyên tử (ACID Transaction): Cập nhật trạng thái công nợ và tạo bản ghi biên lai thu tiền đồng thời trong một transaction SQLite duy nhất.
- Lưu trữ snapshot lịch sử: Mỗi biên lai lưu cố định thông tin sinh viên và số dư tại thời điểm thu tiền, đảm bảo tính toàn vẹn chứng từ khi hồ sơ sinh viên hoặc biểu phí thay đổi sau này.
- Mô phỏng thanh toán VietQR: Sinh mã QR động chuẩn hóa theo phiên giao dịch, hiển thị mã thanh toán và hỗ trợ quét mã kiểm tra trước khi xác nhận lập biên lai.

### 4. Báo cáo và thống kê
- Bảng điều khiển thống kê tổng quan theo từng học kỳ: Tổng học phí, đã thu, còn nợ, tỷ lệ hoàn thành.
- Báo cáo công nợ chi tiết theo sinh viên hoặc tổng hợp theo lớp học.
- Xuất báo cáo công nợ và biên lai thu tiền ra định dạng CSV và PDF trực tiếp bằng engine GDI+ thuần.

### 5. Quản trị và an toàn dữ liệu
- Cơ chế kiểm soát phiên bản cơ sở dữ liệu (`PRAGMA user_version`), tự động nâng cấp cấu trúc bảng khi có thay đổi.
- Sao lưu và phục hồi cơ sở dữ liệu nguyên tử: Kiểm tra tính toàn vẹn và cấu trúc bảng trước khi hoán đổi dữ liệu.
- Bảo mật thông tin cấu hình gửi mail SMTP bằng cơ chế mã hóa Windows DPAPI.

---

## Kiến trúc công nghệ

- **Ngôn ngữ & Nền tảng**: C# 14 / .NET 10.0 Windows Forms
- **Cơ sở dữ liệu**: SQLite (`Microsoft.Data.Sqlite 10.0`)
- **Đồ họa & Hiển thị**: GDI+ Vector Rendering, hỗ trợ High-DPI (`PerMonitorV2`)
- **Tạo mã QR**: `QRCoder 1.8.0`

---

## Cài đặt và Khởi chạy

### Yêu cầu môi trường
- Hệ điều hành: Windows 10 / Windows 11 (64-bit)
- Bộ phát triển: .NET 10.0 SDK

### 1. Biên dịch và chạy ứng dụng
Tại thư mục gốc của repository:

```powershell
dotnet build 26K1_DotNet.slnx
dotnet run --project 26K1_DotNet
```

### 2. Chạy với dữ liệu mẫu (Demo Mode)
Để khởi chạy ứng dụng trong môi trường thử nghiệm độc lập, sử dụng tham số `--demo`:

```powershell
dotnet run --project 26K1_DotNet -- --demo
```

Hệ thống sẽ tự động khởi tạo cơ sở dữ liệu thử nghiệm độc lập tại thư mục tạm (không tác động đến cơ sở dữ liệu vận hành chính) với dữ liệu mẫu gồm 3 sinh viên, 1 học kỳ, 3 khoản học phí và 1 biên lai thanh toán.

### 3. Chạy kiểm thử hệ thống
Kiểm tra tính tương thích và toàn vẹn cơ sở dữ liệu:

```powershell
dotnet run --project 26K1_DotNet -- --test
```

Chạy toàn bộ bộ kiểm thử hồi quy và nghiệm thu tính năng:

```powershell
dotnet run --project 26K1_DotNet.RegressionTests
```

### 4. Đóng gói phân phối (Release Publish)
Đóng gói phiên bản thực thi không nhúng kèm runtime (yêu cầu máy đích đã cài .NET Desktop Runtime):

```powershell
dotnet publish 26K1_DotNet -c Release -o output/publish --self-contained false
```

---

## Quản lý dữ liệu và cấu hình

- **Cơ sở dữ liệu SQLite**: Mặc định đặt tại `%LocalAppData%\EduFee\edufee.db`. Ứng dụng thực thi duy nhất một tiến trình tại một thời điểm để đảm bảo an toàn truy cập file SQLite.
- **Nhập dữ liệu cũ**: Hỗ trợ chuyển đổi từ các file JSON cấu trúc cũ sang SQLite. Giao diện cho phép chọn thư mục nguồn; chế độ dòng lệnh `--migrate` đọc bộ bốn tệp JSON từ thư mục làm việc hiện tại.
- **Cấu hình SMTP**: Thiết lập máy chủ gửi mail thông báo trong màn hình cài đặt. Mật khẩu kết nối được bảo vệ bởi Windows DPAPI.

---

## Tài liệu liên quan

- [Kế hoạch phát triển và Roadmap kỹ thuật](KE_HOACH_PHAT_TRIEN.md)
- [Ghi chú cải tiến kỹ thuật](IMPROVEMENTS.md)
- [Đặc tả thiết kế giao diện UI/UX](UI_Plan.md)
