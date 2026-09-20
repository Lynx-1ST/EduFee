# EduFee — Quản lý sinh viên và học phí

Ứng dụng Windows Forms cho đồ án quản lý sinh viên, học kỳ, học phí, công nợ và biên lai trên một máy Windows. Dữ liệu vận hành được lưu bằng SQLite tại `%LocalAppData%\EduFee\edufee.db`.

## Chức năng đã có

- Thêm, sửa, tìm kiếm, lọc lớp, nhập và xuất CSV sinh viên.
- Lập học kỳ và học phí theo tín chỉ, miễn giảm, hạn nộp, trạng thái nợ/quá hạn.
- Ghi nhận thanh toán một phần hoặc đủ tiền; cập nhật học phí và phát hành biên lai trong một transaction SQLite.
- Thanh toán QR mô phỏng VietQR/MoMo: tạo mã QR quét được, mã giao dịch và callback xác nhận giả lập trước khi lập biên lai; không kết nối tài khoản thật.
- Thống kê công nợ theo học kỳ/lớp, xuất CSV và PDF; xuất PDF biên lai.
- Sao lưu và phục hồi SQLite có kiểm tra trước khi thay dữ liệu; một phiên ứng dụng tại một thời điểm.

Các tệp PDF được tạo từ ảnh bố cục để hỗ trợ tiếng Việt mà không thêm thư viện ngoài. Vì vậy nội dung PDF không thể tìm kiếm hoặc chọn/copy văn bản. Chức năng in trên máy in thật chưa được nghiệm thu; xuất PDF không cần driver máy in.

## Chạy ứng dụng

Yêu cầu: Windows 10/11 và .NET 10 SDK để phát triển/chạy từ mã nguồn.

Tại thư mục gốc repository:

```powershell
dotnet build 26K1_DotNet.slnx
dotnet run --project 26K1_DotNet
```

Chạy bản trình diễn tách biệt với dữ liệu thật:

```powershell
dotnet run --project 26K1_DotNet -- --demo
```

Demo tạo 3 sinh viên, 1 học kỳ, 3 phiếu học phí và 1 biên lai. Với đơn giá mặc định 620.000 đồng/tín chỉ, tổng học phí là **22.440.000 đồng**, đã thu **3.000.000 đồng**, còn nợ **19.440.000 đồng**. Dữ liệu demo nằm trong thư mục demo riêng và email chỉ được mô phỏng.

Lệnh kiểm thử nhanh cách ly với dữ liệu thật:

```powershell
dotnet run --project 26K1_DotNet -- --test
dotnet run --project 26K1_DotNet.RegressionTests
```

Kết quả regression hiện hành được ghi ở đầu ra của runner và `regression-latest.log` khi chạy; không dùng một số lượng kiểm tra cố định trong tài liệu vì bộ kiểm thử được mở rộng cùng mã nguồn.

Đóng gói Release phụ thuộc .NET Desktop Runtime trên máy đích:

```powershell
dotnet publish 26K1_DotNet -c Release -o output/publish --self-contained false
```

## Dữ liệu, nhập và phục hồi

- SQLite trong AppData là nguồn dữ liệu duy nhất khi ứng dụng chạy. Ứng dụng không tự nhập JSON cũ lúc khởi động.
- Dùng màn hình cấu hình dữ liệu hoặc `--migrate` để nhập rõ ràng một thư mục có đủ `students.json`, `semesters.json`, `tuitionfees.json`, `receipts.json`. Ứng dụng tạo bản sao lưu trước khi nhập và từ chối cả lượt nhập nếu liên kết hoặc sổ cái không khớp.
- Dữ liệu mẫu JSON cũ có phiếu học phí #13 và #15 đã ghi nhận thu nhưng không có biên lai tương ứng. Chúng được giữ nguyên và không được tự sửa hay tự sinh biên lai.
- Email settings được lưu cùng vùng dữ liệu người dùng; mật khẩu SMTP dùng Windows DPAPI. Demo chỉ ghi email mô phỏng.

## Tài liệu

- [Kế hoạch phát triển](KE_HOACH_PHAT_TRIEN.md) ghi trạng thái bản lõi và các chức năng tiếp theo.
- [Ghi chú cải tiến](IMPROVEMENTS.md) tóm tắt các thay đổi độ tin cậy đã áp dụng và giới hạn còn lại.
