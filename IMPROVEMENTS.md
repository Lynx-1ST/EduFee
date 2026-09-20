# Ghi chú cải tiến EduFee

Tài liệu này ghi nhận bản lõi cho đồ án desktop một máy, không phải tuyên bố sẵn sàng cho hệ thống nhiều người dùng.

## Đã áp dụng

| Khu vực | Cải tiến |
| --- | --- |
| Lưu trữ | SQLite trong `%LocalAppData%\EduFee` là nguồn dữ liệu vận hành; tiền lưu số nguyên đồng. |
| Dữ liệu | Schema có khóa ngoại, ràng buộc tiền, mã biên lai duy nhất và chặn xóa dữ liệu có lịch sử tài chính. |
| Thu tiền | Cập nhật số đã thu và tạo biên lai thực hiện trong cùng transaction; biên lai lưu snapshot của sinh viên, học kỳ và số dư. |
| JSON cũ | Nhập là thao tác chủ động, yêu cầu đủ bốn tệp và đối soát sổ cái trước khi ghi; dữ liệu không hợp lệ không được nhập một phần. |
| Báo cáo | CSV và PDF công nợ; PDF biên lai, Unicode tiếng Việt và phân trang báo cáo. |
| Khôi phục | Backup/restore SQLite kiểm tra schema và tính toàn vẹn trước khi thay database, đồng thời giữ một bản sao dữ liệu cũ. |
| Kiểm thử | Có runner regression độc lập cho dữ liệu SQL, giao dịch tài chính, migration, PDF, khởi động demo và các hành vi giao diện cơ bản. |

## Giới hạn cần nêu khi bảo vệ đồ án

- Chỉ phù hợp một máy; khóa mutex ngăn mở đồng thời, không có đồng bộ mạng hay phân quyền nhiều người dùng.
- PDF là ảnh bố cục nên không hỗ trợ tìm kiếm/chọn văn bản. In ra máy in vật lý chưa được kiểm thử.
- Email thật và đối soát chuyển khoản ngân hàng không nằm trong luồng demo; demo dùng email mô phỏng.
- Bộ test tự động không thay cho nghiệm thu thủ công trên máy Windows đích, DPI/máy in/SMTP thực tế.
- JSON cũ có hai khoản đã thu thiếu biên lai (#13 và #15); ứng dụng giữ nguyên tệp nguồn và từ chối nhập cho đến khi người phụ trách cung cấp chứng từ thật hoặc hiệu chỉnh sổ cái.

Xem lệnh chạy, demo, đóng gói và kết quả regression mới nhất trong [README.md](README.md).
