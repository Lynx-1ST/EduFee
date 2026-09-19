# EduFee — Hệ Thống Quản Lý Sinh Viên & Học Phí

> Ứng dụng Desktop Windows Forms quản lý sinh viên, học kỳ, học phí, thu tiền và công nợ trên nền tảng **.NET 10 (C# 14)** cho Trường Đại học Mỏ - Địa chất.

---

## 📌 Tính Năng Nổi Bật

* **Quản lý Sinh viên**: Thêm, sửa, xóa hồ sơ sinh viên; tìm kiếm theo tên, mã sinh viên, lọc theo lớp; nhập (Import) và xuất (Export) CSV chống lỗi ký tự xuống dòng, formula injection và trùng lặp mã.
* **Quản lý Học phí & Học kỳ**: Thiết lập học kỳ, tính học phí tự động theo số tín chỉ (chuẩn 620.000 VNĐ/tín chỉ), hỗ trợ chính sách miễn giảm/học bổng, theo dõi hạn nộp và trạng thái quá hạn.
* **Giao Dịch Thu Tiền & Biên Lai Nguyên Tử**: Ghi nhận nộp tiền và phát hành biên lai trong **cùng một transaction SQLite**. Không bao giờ xảy ra tình trạng đã trừ nợ nhưng thiếu biên lai hoặc ngược lại.
* **Biên Lai Lịch Sử Bất Biến (Snapshot)**: Lưu trữ trọn vẹn trạng thái tại thời điểm thanh toán (tên SV, lớp, học kỳ, tổng học phí, số tiền đã đóng lũy kế, số dư còn lại) vào biên lai để bảo toàn tính pháp lý lịch sử.
* **Xuất Báo Cáo & Biên Lai PDF Trực Tiếp**:
  * Biên lai thu học phí: Định dạng chuẩn A4 Portrait.
  * Báo cáo công nợ học phí: Định dạng A4 Landscape đa trang, tự động phân trang (23–24 dòng/trang) với lặp lại tiêu đề và dòng tổng kết tài chính không tràn lề.
  * Không phụ thuộc vào cài đặt máy in hay driver của Windows.
* **Bảo Mật & Độ Tin Cậy**:
  * Mã hóa mật khẩu SMTP cấu hình gửi email bằng **Windows DPAPI** theo tài khoản người dùng Windows.
  * Dữ liệu tiền tệ lưu trữ kiểu số nguyên đồng `INTEGER` tránh sai số làm tròn số thực.
  * Bật kiểm soát khóa ngoại (`PRAGMA foreign_keys = ON`), chặn xóa hồ sơ có lịch sử tài chính.
  * Sao lưu (Backup) và Phục hồi (Restore) SQLite có kiểm chứng toàn vẹn cấu trúc và hợp đồng schema.
* **Trải Nghiệm UI/UX Hiện Đại**: Hệ thống theme tập trung (`UITheme`), hỗ trợ **High-DPI PerMonitorV2**, layout phản hồi (responsive), điều hướng phím tắt (Enter/Esc), validation thông minh qua `ErrorProvider`.

---

## 🚀 Cài Đặt & Khởi Chạy

### Yêu cầu môi trường
* Hệ điều hành: Windows 10/11
* .NET SDK: **.NET 10.0** trở lên

### Build và chạy ứng dụng

Từ thư mục chứa repository:

```powershell
# Khôi phục và build mã nguồn
dotnet build

# Chạy ứng dụng giao diện chính
dotnet run
```

*(Hoặc từ thư mục gốc chứa file solution: `dotnet build 26K1_DotNet.slnx`)*

### Các tham số dòng lệnh hữu ích

* **Kiểm tra tự động toàn diện (Self-Test)**:
  ```powershell
  dotnet run -- --test
  ```
  *Chạy bộ kiểm thử giả lập cách ly với thư mục tạm: kiểm tra schema database, đối soát di trú dữ liệu, phát hành email mô phỏng.*

* **Chuyển đổi dữ liệu JSON sang SQLite (Migration)**:
  ```powershell
  dotnet run -- --migrate
  ```
  *Đối soát sổ cái tài chính và nạp dữ liệu từ các file JSON vào file database chuẩn `edufee.db`.*

---

## 🧪 Kiểm Thử Hồi Quy (Regression Tests)

Dự án đi kèm bộ kiểm thử hồi quy độc lập tại `26K1_DotNet.RegressionTests`:

```powershell
dotnet run --project ../26K1_DotNet.RegressionTests
```

**Kết quả kiểm thử**: Đạt **52/52 bài kiểm tra PASS (100%)**, bao gồm:
1. Quy tắc tính học phí, miễn giảm, tính ngày quá hạn.
2. Cơ chế ghi đè nguyên tử và khôi phục khi tệp dữ liệu bị khóa/lỗi.
3. Chống đọc dữ liệu hỏng hoặc tệp rỗng.
4. Nhập xuất CSV an toàn, chống formula injection.
5. Mã hóa và giải mã DPAPI mật khẩu SMTP.
6. Tính bền vững của bộ lọc học phí và thống kê khi đổi học kỳ.
7. Đảm bảo giao diện hiển thị chuẩn ở các độ rộng 900px và 1080px.
8. Giao dịch đồng thời, khóa ngoại và ràng buộc tiền tệ trên SQLite.
9. Kiểm chứng hợp đồng sao lưu và phục hồi database.
10. Xuất file PDF biên lai và báo cáo công nợ đa trang không cần máy in.

---

## 📂 Cấu Trúc Dữ Liệu & Vận Hành

* **Cơ sở dữ liệu SQLite**: `%LocalAppData%\EduFee\edufee.db` là nguồn chân lý duy nhất trong quá trình ứng dụng chạy.
* **Khởi tạo lần đầu**: Nếu database trong AppData chưa có dữ liệu, ứng dụng sẽ tự động nhập dữ liệu ban đầu từ các file seed (`students.json`, `semesters.json`, `tuitionfees.json`, `receipts.json`) và đối soát tính toàn vẹn của sổ cái.
* **Cấu hình Email**: Lưu tại `email_settings.json` với mật khẩu được mã hóa an toàn qua DPAPI.

---

## 📑 Tài Liệu Tham Khảo

* [**IMPROVEMENTS.md**](IMPROVEMENTS.md): Nhật ký rà soát độ tin cậy và chi tiết các giải pháp kiến trúc đã triển khai.
* [**KE_HOACH_PHAT_TRIEN.md**](KE_HOACH_PHAT_TRIEN.md): Báo cáo đánh giá hệ thống và lộ trình phát triển backlog (T01 – T09).
