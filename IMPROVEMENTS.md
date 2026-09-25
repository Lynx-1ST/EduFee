# Ghi chú Cải tiến & Đặc tả Kỹ thuật EduFee

Tài liệu này tổng hợp các giải pháp cải tiến kỹ thuật, kiến trúc xử lý dữ liệu và các đặc tả thiết kế hiện hành của hệ thống EduFee.

---

## 1. Các cải tiến kỹ thuật đã áp dụng

| Phân hệ | Giải pháp kỹ thuật đã triển khai |
| :--- | :--- |
| **Lưu trữ dữ liệu** | Chuyển đổi sang SQLite (`Microsoft.Data.Sqlite`) tại `%LocalAppData%\EduFee\edufee.db`. Số tiền được lưu trữ dưới dạng số nguyên (`long`), loại bỏ triệt để lỗi làm tròn dấu phẩy động. |
| **Ràng buộc toàn vẹn** | Kích hoạt bắt buộc khóa ngoại (`PRAGMA foreign_keys = ON`), thiết lập ràng buộc duy nhất cho mã biên lai (`ReceiptCode`) và mã sinh viên (`StudentCode`), chặn xóa các thực thể có dữ liệu tài chính liên quan. |
| **Giao dịch tài chính (ACID)** | Thao tác cập nhật trạng thái học phí và tạo bản ghi biên lai được thực thi trong một `SqliteTransaction` nguyên tử. Nếu xảy ra lỗi tại bất kỳ bước nào, toàn bộ giao dịch được hoàn tác (rollback) và bộ nhớ đệm được giữ nguyên trạng thái an toàn. |
| **Snapshot lịch sử chứng từ** | Biên lai lưu trữ bản sao cố định (snapshot) thông tin sinh viên và số dư tại thời điểm giao dịch, đảm bảo số liệu trên chứng từ không bị thay đổi khi thông tin sinh viên hoặc học phí được chỉnh sửa trong tương lai. |
| **Kiểm soát phiên bản CSDL** | Quản lý schema bằng `PRAGMA user_version`, migration tuần tự từ v1 đến v5. v4 bổ sung `StudentCode`; v5 bổ sung giao dịch gateway. Các bước migration có rollback và kiểm thử retry khi xảy ra lỗi. |
| **Cổng thanh toán QR** | VietQR và MoMo Sandbox triển khai `IPaymentGateway`. VietQR tạo mã chuyển khoản và dùng xác nhận thủ công; MoMo ký HMAC-SHA256, truy vấn endpoint thử nghiệm và xác thực phản hồi trước khi ghi nhận. Intent được lưu trước khi gọi mạng; học phí, biên lai và trạng thái hoàn tất được cập nhật trong cùng transaction. Khi restart, các giao dịch MoMo `Pending/Unknown` được truy vấn lại tự động và có lệnh kiểm tra thủ công; giao dịch hoàn tất không bị truy vấn hoặc ghi nhận lần hai. Secret MoMo lưu bằng DPAPI CurrentUser; test dùng HTTP giả lập, không cần credential. |
| **Bảo mật hệ thống** | Tham số hóa 100% các câu lệnh SQL để ngăn chặn SQL Injection; mã hóa mật khẩu SMTP bằng Windows Data Protection API (DPAPI); trung hòa các ký tự điều khiển trong tệp CSV để chống tấn công CSV Formula Injection. |
| **Kết xuất báo cáo PDF** | Triển khai engine render PDF trực tiếp bằng canvas đồ họa GDI+, hỗ trợ đầy đủ tiếng Việt có dấu, thuật toán ngắt trang tự động cho báo cáo nhiều dòng, độc lập hoàn toàn với driver máy in. |
| **Sao lưu và phục hồi** | Quá trình khôi phục cơ sở dữ liệu kiểm tra cấu trúc schema và tính toàn vẹn trước khi thay thế; tự động tạo bản sao lưu dự phòng an toàn trước khi thực hiện thao tác. |
| **Hệ thống kiểm thử** | Xây dựng bộ kiểm thử hồi quy và nghiệm thu module hóa (`RegressionTests`) cho nghiệp vụ tài chính, SQLite, migration, CSV, PDF, báo cáo, VietQR, startup và một số hành vi UI. GitHub Actions chạy build Release, regression suite và self-test trên Windows cho push/pull request vào `main`. |

---

## 2. Đặc tả phạm vi và giới hạn thiết kế

1. **Mô hình triển khai**:
   - Ứng dụng được thiết kế theo kiến trúc Desktop Client đơn máy, vận hành trên một phiên duy nhất tại một thời điểm (được bảo vệ bằng `Mutex` toàn cục).
   - Cơ sở dữ liệu SQLite được đặt trong thư mục cục bộ của người dùng trên máy tính thực thi.

2. **Cơ chế kết xuất tài liệu PDF**:
   - Tệp PDF biên lai và báo cáo công nợ được dựng từ canvas đồ họa vector/raster nhằm đảm bảo hiển thị đồng nhất phông chữ tiếng Việt trên tất cả các phiên bản hệ điều hành Windows mà không cần cài đặt phông chữ ngoài hoặc thư viện bên thứ ba.
   - Do dựng từ canvas đồ họa, văn bản trong tệp PDF xuất ra không hỗ trợ chức năng bôi đen chọn chữ (text selection).

3. **Thanh toán điện tử và Thông báo**:
   - Phân hệ thanh toán VietQR cung cấp giao diện tạo mã QR động kèm mã định danh giao dịch và đồng hồ đếm ngược, phục vụ quy trình xác nhận thanh toán trước khi lập biên lai thu tiền.
   - Tính năng gửi thư điện tử hỗ trợ kết nối tới các máy chủ SMTP tiêu chuẩn; khi ở chế độ Demo (`--demo`), hệ thống tự động chuyển sang chế độ mô phỏng ghi nhận nhật ký để đảm bảo an toàn thử nghiệm.

4. **Đối soát và chuyển đổi dữ liệu kế toán**:
   - Khi thực hiện chuyển đổi dữ liệu từ các phiên bản lưu trữ cũ, hệ thống yêu cầu đối soát đồng bộ giữa tổng số tiền đã thu trên học phí và tổng giá trị của các biên lai thu tiền phát sinh.
   - Những khoản thu chưa có chứng từ biên lai đối ứng hợp lệ sẽ không được tự động tạo mới chứng từ để đảm bảo nguyên tắc kế toán; người quản trị cần kiểm tra và cập nhật chứng từ trước khi hoàn tất chuyển đổi.
