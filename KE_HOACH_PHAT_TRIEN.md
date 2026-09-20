# EduFee — Kế hoạch Phát triển & Roadmap Tính năng

Tài liệu này xác định lộ trình phát triển, trạng thái hoàn thiện của các phân hệ chức năng và các định hướng nâng cấp trong tương lai cho hệ thống quản lý học phí EduFee.

---

## 1. Trạng thái các phân hệ hiện tại

| Mã module | Phân hệ / Tính năng | Trạng thái | Mô tả chi tiết |
| :---: | :--- | :---: | :--- |
| **M01** | Hệ thống kiểm thử hồi quy tự động | Hoàn thành | Xây dựng bộ test runner độc lập, kiểm tra toàn bộ luồng nghiệp vụ tài chính, lưu trữ, PDF và giao diện. |
| **M02** | Cơ sở dữ liệu SQLite & Migration | Hoàn thành | Quản lý schema versioning, ràng buộc khóa ngoại, chuyển đổi dữ liệu an toàn và tự động sao lưu dự phòng. |
| **M03** | Quản lý sinh viên & Học kỳ | Hoàn thành | Đầy đủ chức năng CRUD, kiểm tra tính hợp lệ dữ liệu, phân lớp, lọc tìm kiếm và chặn xóa khi có ràng buộc tài chính. |
| **M04** | Quản lý biểu phí & Học phí | Hoàn thành | Tính toán theo đơn giá tín chỉ, áp dụng miễn giảm, quản lý trạng thái thanh toán (kể cả trạng thái nộp muộn sau hạn). |
| **M05** | Thanh toán & Phát hành biên lai | Hoàn thành | Thực thi trong giao dịch SQLite nguyên tử, cấp mã biên lai duy nhất, lưu trữ snapshot số dư và sinh viên tại thời điểm thu. |
| **M06** | Thống kê & Báo cáo công nợ | Hoàn thành | Tổng hợp số liệu theo học kỳ, lớp học; bộ lọc đa điều kiện; kết xuất dữ liệu ra file CSV và PDF. |
| **M07** | Kết xuất biên lai PDF | Hoàn thành | Dựng trang PDF biên lai trực tiếp qua engine đồ họa GDI+, hỗ trợ đầy đủ Unicode tiếng Việt và nhúng logo đơn vị. |
| **M08** | Báo cáo công nợ PDF đa trang | Hoàn thành | Thuật toán phân trang tự động, xử lý ngắt trang bảng dữ liệu lớn và tách dòng tổng kết sang trang mới khi cần. |
| **M09** | Quản trị dữ liệu & Môi trường Demo | Hoàn thành | Tính năng sao lưu, phục hồi cơ sở dữ liệu có kiểm tra cấu trúc; chế độ khởi chạy `--demo` độc lập dữ liệu thử nghiệm. |
| **M10** | Thanh toán VietQR | Hoàn thành | Tạo mã QR động chuẩn hóa theo phiên giao dịch, hiển thị mã thanh toán, đồng hồ đếm ngược và luồng xác nhận thu phí. |

---

## 2. Quy tắc nghiệp vụ và nguyên tắc vận hành

1. **Nguồn dữ liệu vận hành**:
   - Cơ sở dữ liệu SQLite tại `%LocalAppData%\EduFee\edufee.db` là nguồn dữ liệu duy nhất của hệ thống khi chạy.
   - Thao tác chuyển đổi từ nguồn dữ liệu cũ yêu cầu cấu trúc đầy đủ các bảng và đối soát khớp số liệu trước khi ghi nhận.

2. **Nguyên tắc kế toán và giao dịch tài chính**:
   - Mọi khoản thu học phí phát sinh phải tạo lập biên lai tương ứng trong cùng một transaction.
   - Biên lai sau khi phát hành mang tính chứng từ cố định, không thay đổi số liệu khi các thông tin danh mục khác thay đổi.
   - Hệ thống ngăn chặn việc xóa các bản ghi sinh viên, học kỳ hoặc học phí nếu đã có phát sinh giao dịch thanh toán hoặc biên lai.

3. **An toàn và phục hồi cơ sở dữ liệu**:
   - Trước khi thực hiện phục hồi cơ sở dữ liệu từ tệp bên ngoài, hệ thống tự động kiểm tra cấu trúc schema và tạo một bản sao lưu an toàn của cơ sở dữ liệu hiện hành.

---

## 3. Quy trình nghiệm thu chất lượng

Trước khi phát hành các bản cập nhật mới, quy trình kiểm tra chất lượng tuân theo các bước chuẩn:

1. **Kiểm tra biên dịch**:
   ```powershell
   dotnet build 26K1_DotNet.slnx
   ```
   Yêu cầu: 0 lỗi biên dịch, 0 cảnh báo.

2. **Chạy kiểm thử hồi quy**:
   ```powershell
   dotnet run --project 26K1_DotNet.RegressionTests
   ```
   Yêu cầu: 100% các bài kiểm tra đều đạt trạng thái PASS.

3. **Kiểm tra chức năng hệ thống**:
   ```powershell
   dotnet run --project 26K1_DotNet -- --test
   ```
   Yêu cầu: Xác nhận toàn vẹn các dịch vụ lõi, cấu trúc SQLite và mô phỏng gửi email.

4. **Kiểm tra chế độ Demo độc lập**:
   ```powershell
   dotnet run --project 26K1_DotNet -- --demo
   ```
   Yêu cầu: Giao diện hiển thị đầy đủ dữ liệu mẫu và các biểu đồ KPI tài chính.

5. **Đóng gói phân phối**:
   ```powershell
   dotnet publish 26K1_DotNet -c Release -o output/publish --self-contained false
   ```

---

## 4. Định hướng phát triển các giai đoạn tiếp theo

1. **Hồ sơ tài chính sinh viên tích hợp (Unified Financial Ledger)**:
   - Cung cấp màn hình tra cứu tổng thể theo từng sinh viên: Lịch sử học phí qua các học kỳ, danh sách tất cả biên lai đã cấp và biến động số dư theo dòng thời gian.

2. **Nghiệp vụ điều chỉnh và hoàn học phí (Adjustments & Refunds)**:
   - Bổ sung quy trình điều chỉnh số tiền học phí (thay đổi số tín chỉ sau thời gian đăng ký môn học) kèm lý do phê duyệt; lưu giữ chứng từ gốc thay vì sửa đè dữ liệu.

3. **Phân loại tuổi nợ (Aging Debt Analysis)**:
   - Mở rộng báo cáo công nợ với các nhóm tuổi nợ: Trong hạn, quá hạn 1–30 ngày, 31–60 ngày và trên 60 ngày nhằm hỗ trợ công tác đôn đốc thu nộp hiệu quả hơn.

4. **Tùy biến mẫu chứng từ & Cấu hình thương hiệu**:
   - Cho phép người quản trị tùy chỉnh thông tin trường học, logo, chữ ký mẫu và nội dung thông báo trên biên lai trực tiếp từ màn hình cài đặt giao diện.
