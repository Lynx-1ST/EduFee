# EduFee — Đánh giá và kế hoạch phát triển

> Trạng thái triển khai 19/09/2026: T01-T08 và phần sao lưu/phục hồi của T09 đã được triển khai. Regression có kiểm tra SQLite, transaction, backup/restore và PDF. Phần đóng gói bộ cài/máy Windows đích vẫn cần nghiệm thu trên môi trường phát hành thực tế.

Ngày kiểm tra: 19/09/2026. Phạm vi đã được người dùng xác nhận: **ứng dụng desktop một máy / đồ án**.

## 1. Kết luận

Ứng dụng WinForms .NET 10 đã có phần lớn màn hình nghiệp vụ sinh viên, học phí, thu tiền và công nợ. Tuy nhiên, yêu cầu SQL mới đạt mức đồng bộ bản sao SQLite; chưa dùng SQL làm nguồn dữ liệu vận hành. Thống kê hiện xuất CSV, chưa có xuất báo cáo PDF trực tiếp. Cần hoàn thiện tính nhất quán thu tiền–biên lai trước khi coi luồng tài chính là hoàn chỉnh.

Đánh giá sử dụng ba agent độc lập: quản lý sinh viên; học phí và công nợ; SQL, biên lai và PDF. Agent chính tổng hợp và chạy build, regression, self-test. Đây là kiểm tra mã nguồn và bộ test hiện có, chưa phải nghiệm thu thao tác thủ công toàn bộ giao diện hay in ra máy in thật.

## 2. Đối chiếu yêu cầu

| Yêu cầu | Hiện trạng | Việc còn thiếu |
|---|---|---|
| Quản lý sinh viên | Có thêm/sửa/xóa, tìm kiếm, lọc lớp, import/export CSV | Chặn xóa hồ sơ có dữ liệu tài chính; validation tại service; bảo toàn trạng thái khi lưu lỗi |
| Quản lý học phí | Có học kỳ, tính tiền theo tín chỉ, giảm trừ, lập học phí và ghi nhận nộp tiền | Ràng buộc nghiệp vụ tại service và database; bảo vệ khoản học phí đã thu |
| Quản lý công nợ | Có số phải nộp, đã nộp, còn nợ, trạng thái quá hạn | Đối soát với biên lai; bảo đảm mọi báo cáo dùng cùng bộ lọc và quy tắc tính |
| Thống kê nợ | Có tổng hợp, danh sách nợ, thống kê theo lớp/học kỳ và CSV | Báo cáo PDF nhiều trang, tổng cộng và thông tin bộ lọc |
| In biên lai | Có lịch sử, xem và luồng xem trước in | Nghiệm thu in thực tế; lưu PDF trực tiếp; giữ nguyên nội dung lịch sử |
| Kết nối SQL | Có SQLite, tạo bảng, đồng bộ JSON sang SQL, đọc ngược | CRUD và thu tiền trực tiếp trên SQLite; migration có kiểm chứng |
| Xuất báo cáo PDF | Chưa thấy luồng xuất PDF trực tiếp trong màn thống kê | Tạo chức năng riêng, không phụ thuộc cấu hình máy in Windows |

## 3. Các vấn đề ưu tiên

Mức P0: cần giải quyết trước nghiệm thu luồng tài chính. P1: cần để đáp ứng đủ yêu cầu đồ án. P2: phát triển tiếp sau bản lõi.

| Mức | Phát hiện và tác động | Bằng chứng mã nguồn |
|---|---|---|
| P0 | Thu tiền lưu học phí trước, lưu biên lai sau. Nếu lần lưu thứ hai lỗi, tiền đã ghi nhận nhưng không có biên lai tương ứng. | `26K1_DotNet/FormPayment.cs:246–251` |
| P0 | Xóa sinh viên/học kỳ/phiếu học phí chưa bảo vệ lịch sử thu. Xóa phiếu đã thu có thể làm biên lai còn lưu nhưng không truy cập được qua phiếu. | `26K1_DotNet/Services/StudentService.cs:132–140`, `26K1_DotNet/Services/TuitionService.cs:159–165`, `26K1_DotNet/Services/SemesterService.cs:108–113` |
| P0 | Sửa/xóa sinh viên thay đổi dữ liệu trong bộ nhớ trước khi lưu, không hoàn tác khi ghi lỗi; form sửa còn thay đổi trực tiếp đối tượng. | `26K1_DotNet/Services/StudentService.cs:116–139`, `26K1_DotNet/FormStudentDetail.cs:67–74` |
| P1 | SQL là bản sao thủ công, nên thay đổi hằng ngày chưa tự cập nhật vào database. | `26K1_DotNet/Form1.cs:32–37` |
| P0 | Đồng bộ chỉ INSERT OR REPLACE, không loại bỏ hàng đã xóa khỏi JSON. Sau đồng bộ rồi nạp SQL, bản ghi đã xóa có thể xuất hiện lại. Nạp SQL còn ghi đè bốn JSON liên tiếp, có nguy cơ dở dang khi lỗi. | `26K1_DotNet/Data/SqlDataMigrator.cs:37–38`, `26K1_DotNet/FormDatabaseConfig.cs:277–296` |
| P1 | Service sửa học phí nhận số tiền/trạng thái trực tiếp; tạo biên lai chưa xác thực đầy đủ quan hệ sinh viên–học kỳ–học phí. Thống kê lấy PaidAmount trong khi lịch sử lấy tổng biên lai, nên dữ liệu lệch có thể tạo hai kết quả khác nhau. | `26K1_DotNet/Services/TuitionService.cs:145–156`, `26K1_DotNet/Services/ReceiptService.cs:32–46` |
| P1 | Schema hiện lưu tiền bằng REAL, thiếu khóa ngoại và ràng buộc duy nhất cho mã biên lai. Cần hoàn thiện trước khi chuyển sang lưu chính thức. | `26K1_DotNet/Data/SqlDatabaseContext.cs`, phần tạo bảng |
| P1 | Nút xuất thống kê gọi CSV; in biên lai vẽ ảnh của panel vào PrintDocument. Chưa có bộ sinh báo cáo PDF trực tiếp. | `26K1_DotNet/Controls/PanelStatistics.cs:293–295`, `26K1_DotNet/FormReceipt.cs:299–319` |
| P1 | Regression dừng ở helper reflection dùng cố định kiểu PanelTuition khi đối tượng thực tế là PanelStatistics. Các kiểm tra phía sau chưa chạy. | `26K1_DotNet.RegressionTests/Program.cs:273–274` |

## 4. Kết quả kiểm chứng thực tế

| Lệnh | Kết quả ngày kiểm tra | Giới hạn |
|---|---|---|
| `dotnet build 26K1_DotNet.slnx --no-restore --verbosity minimal` | Thành công: 0 lỗi, 27 cảnh báo | Cảnh báo nullable còn tồn tại |
| `dotnet run --project 26K1_DotNet.RegressionTests --no-build` | 26 kiểm tra đầu thành công, sau đó lỗi và exit code 1 | Không được kết luận toàn bộ regression pass; lỗi helper ở dòng 273 |
| `dotnet run --project 26K1_DotNet --no-build -- --test` | Thành công, exit code 0 | SQL chỉ có 3 học kỳ, 0 sinh viên, 0 học phí, 0 biên lai; email là giả lập |

Hai chương trình test dùng thư mục tạm. Không chạy migration trên dữ liệu người dùng, không gửi email thật, không chỉnh sửa mã nghiệp vụ trong lần đánh giá này.

## 5. Quyết định kiến trúc đề xuất

- Giữ WinForms và SQLite cho phạm vi một máy. Không cần API, máy chủ web hay hệ thống phân quyền phức tạp ở bản đồ án.
- SQLite là nguồn dữ liệu duy nhất sau khi chuyển đổi thành công. JSON chỉ dùng nhập dữ liệu cũ/xuất trao đổi; tránh duy trì hai nguồn ghi song song.
- Giữ lớp service hiện có làm nơi kiểm tra nghiệp vụ; thay phần lưu file bằng truy cập SQL. Không cần viết lại toàn bộ giao diện.
- Đề xuất dùng tiền VND nguyên đồng, lưu kiểu INTEGER; phải xác nhận và xử lý rõ dữ liệu cũ có phần lẻ, không tự làm tròn âm thầm.
- Một thao tác thu tiền phải tạo biên lai và cập nhật số đã thu trong cùng transaction. Mã biên lai duy nhất; không cho thu âm, bằng 0 hay vượt số còn nợ.
- Hồ sơ có lịch sử tài chính được bảo vệ khỏi xóa; dùng trạng thái ngừng theo dõi nếu cần. Hủy khoản thu sau này phải để lại dấu vết, không xóa biên lai đã phát hành.
- Đường dẫn dữ liệu cố định theo người dùng, ví dụ `%LocalAppData%/EduFee`; hiển thị đường dẫn và trạng thái database trong màn cấu hình.

## 6. Backlog triển khai theo thứ tự

Mỗi mục có thể tách thành phiên làm việc nhỏ. Ưu tiên hoàn thành từng luồng chạy được thay vì viết lại toàn bộ tầng dữ liệu rồi mới nối giao diện.

### Mốc A — Nền dữ liệu đáng tin cậy

**T01 — Khôi phục bộ kiểm thử nền (P0, nhỏ).**
- Sửa helper lấy field theo kiểu thực tế của đối tượng; chạy hết bộ regression và phân loại lỗi mới nếu có.
- Nghiệm thu: runner kết thúc thành công, không bỏ qua kiểm tra thống kê, lọc học phí và render đã có.
- Kiểm chứng: build + regression; không coi lỗi test helper là bằng chứng lỗi nghiệp vụ.
- Phụ thuộc: không. Vùng sửa: `26K1_DotNet.RegressionTests/Program.cs`.

**T02 — Schema SQLite và chuyển dữ liệu an toàn (P0, vừa; tách schema và migration thành hai bước).**
- Thêm phiên bản schema, khóa ngoại được bật trên mỗi kết nối, CHECK tiền không âm, mã biên lai duy nhất; quy định một phiếu học phí/sinh viên/học kỳ nếu giữ mô hình hiện tại.
- Chuyển JSON qua database tạm/transaction, kiểm tra số bản ghi, tổng tiền và tham chiếu trước khi nhận làm dữ liệu chính; bảo toàn nguồn cũ khi có lỗi. Phát hiện khoản đã thu thiếu biên lai để người dùng đối soát, không tự dựng biên lai giả.
- Nghiệm thu: bộ dữ liệu hợp lệ giữ nguyên ID và tổng tiền; dữ liệu trùng/mồ côi hoặc lỗi giữa chừng không được chuyển một phần; chạy chuyển đổi lại không tạo bản ghi trùng.
- Kiểm chứng: integration test dữ liệu có sinh viên, học phí, giảm trừ và nhiều lần thu; thử lỗi giữa migration và dữ liệu cũ còn sót trong database snapshot. Không dùng bản SQL snapshot cũ làm nguồn chính mà không đối chiếu.
- Phụ thuộc: T01. Vùng sửa: `Data/SqlDatabaseContext.cs`, `Data/SqlDataMigrator.cs`, test dữ liệu.

**T03 — Sinh viên và học kỳ dùng SQL xuyên suốt (P0, vừa; triển khai riêng từng loại).**
- Đưa CRUD vào service/repository SQL; validation tên, mã, ngày và thông tin hồ sơ dùng chung cho form và import; chặn xóa khi có dữ liệu tài chính.
- Nghiệm thu: thay đổi còn nguyên sau khởi động lại; import lỗi không ghi nửa chừng; lưu lỗi không làm bộ nhớ/UI hiển thị dữ liệu chưa lưu.
- Kiểm chứng: integration test CRUD, xóa có tham chiếu, import trùng và lỗi ghi.
- Phụ thuộc: T02. Vùng sửa: `StudentService`, `SemesterService`, form nhập/sửa liên quan, test.

**T04 — Học phí dùng SQL và bảo vệ quy tắc tính tiền (P0, vừa).**
- Chuyển đọc/ghi học phí sang SQL; kiểm tra trùng học kỳ/sinh viên, tín chỉ, giảm trừ và các sửa đổi sau khi đã thu.
- Nghiệm thu: phải nộp = tiền gốc − giảm trừ; còn nợ = phải nộp − đã thu; không cho sửa tổng phải nộp xuống dưới số đã thu hoặc xóa khoản có lịch sử thu.
- Kiểm chứng: giảm trừ vượt tiền gốc, trùng học phí, thanh toán một phần, hết nợ, hạn hôm nay và quá hạn ngày kế tiếp.
- Phụ thuộc: T03. Vùng sửa: `TuitionService`, `TuitionFee`, form học phí/lập hàng loạt, test.

**T05 — Thu tiền và biên lai nguyên tử (P0, vừa).**
- Một service xử lý transaction tạo biên lai và cập nhật khoản học phí; UI chỉ gửi yêu cầu một lần và hiển thị kết quả sau commit.
- Nghiệm thu: thành công thì có cả số tiền và biên lai; chèn biên lai lỗi thì không tăng tiền đã thu; thao tác lặp không thu trùng. Biên lai phải trỏ đúng sinh viên/học kỳ của phiếu. Lỗi in/email sau commit không làm thu lại.
- Kiểm chứng: giả lập lỗi insert biên lai, thu vượt nợ, submit lặp, khởi động lại và đối soát tổng thu với biên lai.
- Phụ thuộc: T04. Vùng sửa: `TuitionService`, `ReceiptService`, `FormPayment`, test.

**Điểm kiểm tra A:** tạo sinh viên → lập học phí → thu hai lần → khởi động lại → số dư và biên lai vẫn đúng. Tất cả dữ liệu nghiệp vụ đọc/ghi từ SQLite; không phải bấm đồng bộ.

### Mốc B — Hoàn thiện các chức năng bắt buộc

**T06 — Thống kê công nợ thống nhất (P1, vừa).**
- Tập trung truy vấn báo cáo: học kỳ, lớp, sinh viên, còn nợ/quá hạn; màn hình, CSV và PDF dùng cùng dữ liệu đầu vào.
- Nghiệm thu: số tổng bằng tổng các dòng; phân biệt rõ chưa đến hạn và quá hạn; bộ lọc không làm lệch tổng; dữ liệu rỗng có thông báo rõ.
- Kiểm chứng: bộ dữ liệu biết trước kết quả, nhiều học kỳ/lớp, giảm trừ, nộp một phần và thanh toán đủ.
- Phụ thuộc: T05. Vùng sửa: service báo cáo, `PanelStatistics`, test.

**T07 — Xuất biên lai PDF trực tiếp (P1, vừa).**
- Lưu `.pdf` qua hộp thoại chọn tệp; có mã biên lai, ngày thu, sinh viên, học kỳ, người nộp, số tiền và phương thức thanh toán; giữ chức năng in.
- Lưu thông tin cần thiết tại thời điểm phát hành để in lại không thay đổi theo hồ sơ/học phí hiện tại. Thư viện PDF chọn khi triển khai sau khi kiểm tra khả năng tiếng Việt và giấy phép.
- Nghiệm thu: mở được PDF, tiếng Việt đúng dấu, không cắt nội dung; in lại biên lai cũ giữ nguyên nội dung nghiệp vụ; không cần máy in mặc định để xuất file.
- Kiểm chứng: PDF có tên dài, nhiều dấu, khoản thu lẻ và trường hợp tệp bị khóa; mở kiểm tra trực quan.
- Phụ thuộc: T05. Vùng sửa: bộ tạo PDF, dữ liệu biên lai, `FormReceipt`, test.

**T08 — Xuất báo cáo công nợ PDF (P1, vừa).**
- Báo cáo A4 có tiêu đề đơn vị, thời điểm xuất, bộ lọc, chi tiết công nợ, tổng cộng và số trang; lặp tiêu đề bảng khi sang trang.
- Nghiệm thu: 0, 1 và 100+ dòng đều đúng bố cục; tổng khớp màn thống kê; Unicode chuẩn, không mất dòng ở ranh giới trang.
- Kiểm chứng: kiểm tra tổng tiền tự động và mở PDF 1/nhiều trang để xem bố cục.
- Phụ thuộc: T06 và phần dùng chung tạo PDF của T07. Vùng sửa: bộ tạo PDF, `PanelStatistics`, test.

**T09 — Sao lưu, phục hồi và đóng gói (P1, vừa; tách dữ liệu và đóng gói).**
- Dùng đường dẫn dữ liệu cố định, backup database nhất quán; phục hồi qua bản tạm được kiểm tra trước khi thay; giữ backup trước phục hồi.
- Nghiệm thu: chạy ứng dụng từ thư mục khác vẫn thấy đúng dữ liệu; backup–restore khôi phục đúng số bản ghi/tổng tiền; tệp lỗi không ghi đè database tốt.
- Kiểm chứng: restore hợp lệ/hỏng, đường dẫn có dấu/khoảng trắng, thiếu quyền ghi; chạy bản đóng gói trên máy Windows đích. Với phạm vi đồ án, có thể chặn mở đồng thời hai phiên thay vì hỗ trợ ghi song song.
- Phụ thuộc: T05; nghiệm thu cuối sau T08. Vùng sửa: khởi tạo ứng dụng, `FormDatabaseConfig`, lớp backup/restore, hướng dẫn chạy.

**Điểm kiểm tra B:** nhập CSV → tạo học phí → thu một phần → in/xuất biên lai → lọc nợ → xuất PDF → backup → phục hồi. Mọi tổng tiền phải khớp qua toàn bộ luồng.

### Mốc C — Phát triển tiếp sau bản lõi

1. **Hồ sơ tài chính sinh viên:** một màn hình tập hợp học phí các kỳ, biên lai và số còn nợ. Nghiệm thu bằng đối chiếu với báo cáo chi tiết.
2. **Hủy/điều chỉnh khoản thu:** bắt buộc lý do, giữ biên lai gốc và lịch sử điều chỉnh; tính lại số dư và báo cáo nhất quán. Chỉ triển khai sau T05, T06.
3. **Theo dõi tuổi nợ:** các nhóm chưa đến hạn, 1–30, 31–60, trên 60 ngày; kiểm tra ngày biên và tổng các nhóm bằng tổng nợ.
4. **Hoàn thiện nhắc nợ và thông tin đơn vị:** cấu hình tên trường/logo/ngân hàng, preview trước gửi; trình bày đúng việc thanh toán chưa có đối soát ngân hàng tự động. Email thật là tùy chọn sau demo lõi.

Không đặt đăng nhập nhiều vai trò, cổng sinh viên, thanh toán ngân hàng tự động hay hạ tầng nhiều máy vào phạm vi bắt buộc của bản đồ án một máy.

## 7. Phân công multiagent cho giai đoạn triển khai

- Agent chính: chốt schema và quy tắc tiền, tích hợp các luồng, quyết định nghiệm thu.
- Agent dữ liệu: T02, sau đó T03; không đồng thời sửa schema với agent khác.
- Agent tài chính: T04–T05 sau khi hợp đồng lưu dữ liệu ổn định.
- Agent báo cáo: T06–T08 sau khi thống nhất dữ liệu báo cáo/biên lai; PDF và giao diện báo cáo có thể chia việc sau khi chốt hợp đồng.
- Agent kiểm thử/review: test hồi quy và review transaction, migration, báo cáo. Không để hai agent cùng sửa một file.

Thứ tự chính: **T01 → T02 → T03 → T04 → T05 → T06/T07 → T08 → nghiệm thu T09**. Chuẩn bị mẫu PDF và dữ liệu kiểm thử có thể làm song song khi không đụng schema đang thay đổi.

## 8. Điều kiện hoàn thành đồ án

- Build thành công; regression chạy hết và có test SQL chứa dữ liệu tài chính thực sự.
- Quản lý sinh viên/học kỳ/học phí hoạt động trực tiếp với SQLite sau khởi động lại.
- Không tồn tại thu tiền mới thiếu biên lai hoặc xóa hồ sơ làm mất liên kết tài chính.
- In biên lai, xuất PDF biên lai và báo cáo công nợ hoạt động với dữ liệu tiếng Việt và nhiều trang.
- Sao lưu/phục hồi được kiểm chứng; có dữ liệu demo và hướng dẫn thao tác ngắn.
- Ghi rõ các phần tùy chọn chưa triển khai, không dùng kết quả email giả lập hay SQL dữ liệu rỗng để tuyên bố đã nghiệm thu đầy đủ.
