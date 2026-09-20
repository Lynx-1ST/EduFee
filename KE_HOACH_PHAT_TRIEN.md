# EduFee — Kế hoạch phát triển

Phạm vi: đồ án Windows Forms chạy trên một máy. Bản lõi đã hoàn thành quản lý sinh viên, học phí/công nợ, biên lai, SQLite, CSV/PDF và backup/restore. Cần chạy thử bản Release trên máy Windows đích trước khi nộp.

## Trạng thái bản lõi

| Hạng mục | Trạng thái |
| --- | --- |
| T01 — Runner regression | Hoàn thành; runner chạy dữ liệu tạm và xuất kết quả hiện hành. |
| T02 — Schema SQLite, migration JSON an toàn | Hoàn thành; version schema, kiểm tra liên kết/sổ cái, nhập transaction và backup trước nhập. |
| T03 — Sinh viên, học kỳ bằng SQL | Hoàn thành; CRUD, validation và chặn xóa có tham chiếu. |
| T04 — Học phí bằng SQL | Hoàn thành; tiền nguyên đồng, giảm trừ, số dư và các ràng buộc số tiền. |
| T05 — Thu tiền và biên lai | Hoàn thành; một transaction, mã biên lai duy nhất, snapshot lịch sử. |
| T06 — Thống kê công nợ | Hoàn thành; lọc, tổng hợp và xuất CSV/PDF từ dữ liệu báo cáo. |
| T07 — PDF biên lai | Hoàn thành; xuất trực tiếp, không cần máy in mặc định. |
| T08 — PDF công nợ | Hoàn thành; phân trang và tổng cộng cho báo cáo nhiều dòng. |
| T09 — Dữ liệu, backup/restore, demo | Hoàn thành phần mã; còn chạy thử trên máy Windows đích và máy in thực nếu cần. |

Phần trình diễn thanh toán đã có thêm VietQR/MoMo mô phỏng. Mã QR chứa payload demo, có mã giao dịch và thời hạn; chỉ nút xác nhận callback mô phỏng mới tiếp tục ghi nhận thu tiền. Đây không phải tích hợp cổng thanh toán thật.

Tài liệu đánh giá ban đầu có mô tả SQL chỉ là bản sao, chưa có PDF và runner dừng sau một phần kiểm tra. Đây là các phát hiện lịch sử đã được dùng làm backlog, không phải hiện trạng của bản lõi.

## Quy tắc vận hành hiện tại

- SQLite tại `%LocalAppData%\EduFee\edufee.db` là nguồn vận hành. JSON chỉ dùng để nhập dữ liệu cũ có chủ đích.
- Không tự nhập JSON khi database trống. Lượt nhập cần đủ bốn tệp JSON và bị từ chối toàn bộ nếu tổng biên lai không khớp số đã thu hoặc tham chiếu không hợp lệ.
- Không tự tạo biên lai cho dữ liệu cũ: `tuitionfees.json` có phiếu #13 (8.680.000 đồng) và #15 (9.920.000 đồng) đã thu nhưng thiếu biên lai. Người phụ trách cần cung cấp chứng từ thật hoặc xác nhận hiệu chỉnh sổ cái.
- Một lần thu tiền cập nhật học phí và biên lai cùng transaction. Xóa sinh viên, học kỳ hoặc học phí có lịch sử tài chính bị chặn.
- Backup và restore kiểm tra database trước khi thay thế, đồng thời lưu database hiện tại để có thể quay lại.

## Kiểm tra trước khi nộp

1. `dotnet build 26K1_DotNet.slnx` và `dotnet run --project 26K1_DotNet.RegressionTests` thành công; lấy số PASS từ log của lần chạy đó.
2. Chạy `dotnet run --project 26K1_DotNet -- --demo`; đối chiếu 3 sinh viên, 1 học kỳ, 3 học phí, 1 biên lai; tổng 22.440.000 đồng, đã thu 3.000.000 đồng, còn nợ 19.440.000 đồng.
3. Trình diễn: thêm sinh viên → lập học phí → thu một phần → xuất biên lai PDF → lọc công nợ → xuất PDF → backup → restore.
4. Publish bằng `dotnet publish 26K1_DotNet -c Release -o output/publish --self-contained false`, sau đó chạy thử thư mục publish trên Windows có .NET Desktop Runtime tương ứng.

## Mốc C — phát triển sau bản lõi

1. **Hồ sơ tài chính sinh viên:** xem các học kỳ, học phí, biên lai và số dư trong một màn hình.
2. **Hủy/điều chỉnh khoản thu:** bắt buộc lý do, giữ biên lai gốc và lịch sử điều chỉnh thay vì xóa.
3. **Theo dõi tuổi nợ:** nhóm chưa đến hạn, 1–30, 31–60 và trên 60 ngày.
4. **Cấu hình tổ chức:** tên trường, logo, thông tin ngân hàng và mẫu nhắc nợ; email thật chỉ bật khi đã cấu hình và thử nghiệm.

Không đưa đăng nhập nhiều vai trò, cổng sinh viên, thanh toán ngân hàng tự động hoặc đồng bộ nhiều máy vào phạm vi bắt buộc của đồ án này.
