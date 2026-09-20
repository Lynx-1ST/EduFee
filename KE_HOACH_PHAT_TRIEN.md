# EduFee — Roadmap Hoàn thiện Hệ thống

> **Phạm vi:** Ứng dụng .NET 10 Windows Forms quản lý sinh viên, học kỳ, học phí, công nợ và biên lai trên một máy Windows.  
> **Mục tiêu:** Nâng cao độ tin cậy dữ liệu, chuẩn hóa mô hình sinh viên, tự động hóa kiểm thử, hoàn thiện cấu hình học phí, dữ liệu demo và chất lượng bản Release.  
> **Ngoài phạm vi:** Không phát triển module quản lý lớp riêng. Trường `ClassName` tiếp tục được sử dụng cho nhập liệu, lọc và thống kê.

---

## 1. Trạng thái hiện tại

| Khu vực | Trạng thái |
| --- | --- |
| Sinh viên | CRUD, tìm kiếm, lọc lớp, CSV import/export |
| Học kỳ | CRUD, học kỳ hiện tại, hạn nộp |
| Học phí | Tính theo tín chỉ, miễn giảm, công nợ, trạng thái thanh toán |
| Thanh toán | Thu một phần/đủ, chặn thu vượt số dư |
| Biên lai | Snapshot lịch sử, Print Preview, PDF, email |
| VietQR | Luồng mô phỏng, không kết nối ngân hàng thật |
| SQLite | Nguồn dữ liệu vận hành, khóa ngoại, constraints, schema versioning |
| Transaction | Cập nhật học phí và tạo biên lai trong cùng transaction |
| Backup/Restore | Kiểm tra integrity/schema và tạo safety backup |
| Báo cáo | CSV, PDF biên lai, PDF công nợ đa trang |
| Demo mode | `--demo` sử dụng dữ liệu độc lập |
| Regression | Acceptance/regression runner cho finance, database, migration, PDF, report, QR, startup và UI |
| UI/UX | Design system Indigo + Slate, High-DPI, HUMG branding |

---

## 2. Nguyên tắc phát triển

Mọi thay đổi tiếp theo phải duy trì các invariant sau:

- Giữ **.NET 10 WinForms**.
- Giữ **SQLite** là nguồn dữ liệu vận hành.
- Tiền được lưu dưới dạng **INTEGER VND** trong SQLite.
- Không sửa trực tiếp `PaidAmount` ngoài luồng thanh toán có biên lai.
- Payment và Receipt phải được ghi nhận **atomic**.
- Receipt lịch sử phải giữ **snapshot bất biến**.
- Migration phải hỗ trợ rollback và có regression test.
- Không thêm Web API, cloud database, WPF/WinUI hoặc framework UI nặng.
- Không phát triển module quản lý lớp riêng.
- VietQR chỉ là mô phỏng trong phạm vi hệ thống hiện tại.

---

# Phase 1 — Củng cố Schema Migration

## 1.1. Chuẩn hóa version transition v1 → v2 → v3 → v4

### Vấn đề

`MigrateV1ToV2()` không được ghi trực tiếp `CurrentSchemaVersion` nếu vẫn còn bước migration v2 → v3.

### Thực hiện

- [x] `MigrateV1ToV2()` chỉ đặt `PRAGMA user_version = 2`.
- [x] `MigrateV2ToV3()` chỉ đặt version 3 sau khi toàn bộ migration thành công.
- [x] Schema version không thay đổi nếu transaction rollback.
- [x] Xác thực schema thực tế tương ứng với version khai báo.

### Regression

- [x] v1 → v2 → v3 thành công.
- [x] v2 → v3 thành công.
- [x] Inject failure giữa migration v2 → v3.
- [x] Sau failure database vẫn ở version 2.
- [x] Lần khởi tạo tiếp theo có thể retry migration.
- [x] Record count và tổng tiền không thay đổi sau retry.

### Definition of Done

- [x] Không tồn tại trạng thái `user_version` và schema thực tế không đồng nhất.
- [x] Regression suite pass.
- [x] Self-test pass.

---

# Phase 2 — Chuẩn hóa Mã sinh viên

## 2.1. Tách `StudentCode` khỏi `Student.Id`

### Mục tiêu

Tách định danh nghiệp vụ của sinh viên khỏi khóa chính nội bộ của cơ sở dữ liệu.

Thiết kế:

```text
Id          = khóa chính nội bộ
StudentCode = mã sinh viên nghiệp vụ
```

Ví dụ:

```text
Id          17
StudentCode 2121050123
```

### Model

```csharp
public int Id { get; set; }
public string StudentCode { get; set; } = string.Empty;
```

### SQLite

```sql
Id INTEGER PRIMARY KEY,
StudentCode TEXT NOT NULL UNIQUE
```

### Phạm vi cập nhật

- [x] `Student.cs`
- [x] SQLite schema và migration
- [x] `SqliteRepository`
- [x] `SqlDataMigrator`
- [x] `StudentService`
- [x] CSV import/export
- [x] Search/filter sinh viên
- [x] PanelStudents
- [x] PanelTuition
- [x] PanelStatistics
- [x] FormStudentDetail
- [x] Receipt snapshot
- [x] Email receipt
- [x] PDF receipt
- [x] Debt report
- [x] Demo data
- [x] Regression tests

### Business rules

- [x] StudentCode bắt buộc.
- [x] StudentCode unique.
- [x] Trim trước khi lưu.
- [x] Foreign key tiếp tục sử dụng `StudentId`.
- [x] Receipt cũ giữ nguyên snapshot hiện có.
- [x] Receipt mới lưu StudentCode thực tế.

### Migration dữ liệu cũ

Đối với dữ liệu chưa có StudentCode riêng, sinh mã legacy theo quy tắc xác định:

```text
SV0001
SV0002
...
```

Không thay đổi `Id` và không phá vỡ foreign key.

### Definition of Done

- [x] UI không còn suy ra mã sinh viên trực tiếp từ database Id.
- [x] Duplicate StudentCode bị từ chối.
- [x] CSV round-trip giữ đúng StudentCode.
- [x] Receipt, PDF và email hiển thị StudentCode nhất quán.
- [x] Migration và regression pass.

---

# Phase 3 — Đơn giá học phí theo học kỳ

## 3.1. Bổ sung `TuitionPerCredit`

### Mục tiêu

Loại bỏ hard-code đơn giá 620.000 VNĐ/tín chỉ khỏi domain chính.

Bổ sung:

```csharp
Semester.TuitionPerCredit
```

Ví dụ:

| Học kỳ | Đơn giá/tín chỉ |
| --- | ---: |
| HK1 2025–2026 | 580.000 |
| HK2 2025–2026 | 580.000 |
| HK1 2026–2027 | 620.000 |

### Công thức

```text
OriginalAmount = Credits × TuitionPerCredit
TotalAmount    = OriginalAmount - DiscountAmount
```

### Thực hiện

- [ ] Thêm `TuitionPerCredit` vào `Semester`.
- [ ] Bổ sung migration schema.
- [ ] Validation `TuitionPerCredit > 0`.
- [ ] Form quản lý học kỳ cho phép nhập đơn giá.
- [ ] Form lập học phí sử dụng đơn giá học kỳ.
- [ ] Batch tuition sử dụng đơn giá học kỳ.
- [ ] Demo data cập nhật.
- [ ] Regression tests.

### Quy tắc lịch sử

- Phiếu học phí đã tạo giữ nguyên `TotalAmount`.
- Thay đổi đơn giá học kỳ không tự động sửa dữ liệu lịch sử.
- Receipt snapshot không thay đổi.

---

# Phase 4 — GitHub Actions CI

## 4.1. Thêm pipeline Windows

Tạo:

```text
.github/workflows/ci.yml
```

### Trigger

- Push lên `main`.
- Pull request vào `main`.

### Runner

```yaml
runs-on: windows-latest
```

### Pipeline

```powershell
dotnet restore 26K1_DotNet.slnx
dotnet build 26K1_DotNet.slnx -c Release --no-restore
dotnet run --project 26K1_DotNet.RegressionTests -c Release
dotnet run --project 26K1_DotNet -c Release -- --test
```

### Yêu cầu

- [x] Test chỉ sử dụng dữ liệu tạm.
- [x] Không truy cập database người dùng.
- [x] Không yêu cầu SMTP thật.
- [x] Không yêu cầu máy in thật.
- [x] Build hoặc regression failure phải làm workflow fail.

### Definition of Done

- [x] Mỗi commit hoặc pull request có trạng thái CI sau khi workflow được GitHub kích hoạt.
- [ ] Release build pass.
- [ ] Regression pass.
- [ ] Self-test pass.

---

# Phase 5 — Nâng cấp Demo Mode

## 5.1. Mở rộng dataset của `--demo`

### Hiện trạng

```text
3 sinh viên
1 học kỳ
3 phiếu học phí
1 biên lai
```

### Mục tiêu

```text
20–30 sinh viên
3 giá trị ClassName
2–3 học kỳ
20+ phiếu học phí
nhiều biên lai
```

### Trạng thái dữ liệu cần có

- [ ] Chưa nộp.
- [ ] Nộp một phần.
- [ ] Đã nộp đủ.
- [ ] Quá hạn.
- [ ] Nộp muộn.
- [ ] Có miễn giảm.
- [ ] Một sinh viên có nhiều lần thanh toán.

### Yêu cầu

- [ ] Demo luôn sử dụng database riêng.
- [ ] Không gửi email thật.
- [ ] Không tác động dữ liệu vận hành.
- [ ] Tổng tiền deterministic để regression test được.
- [ ] Dashboard và Statistics có dữ liệu đủ đa dạng để kiểm thử các trạng thái.

---

# Phase 6 — Logging và Global Error Handling

## 6.1. AppLogger

Tạo:

```text
Helpers/AppLogger.cs
```

Đường dẫn log:

```text
%LocalAppData%\EduFee\Logs\
```

Ghi log cho:

- Startup.
- Database.
- Migration.
- Backup/Restore.
- CSV.
- PDF.
- Print.
- Email.
- QR demo.
- Unhandled exceptions.

Không ghi:

- SMTP password.
- Credential plaintext.
- Dữ liệu nhạy cảm không cần thiết.

## 6.2. Global exception handling

Trong `Program.cs`:

```csharp
Application.ThreadException += ...
AppDomain.CurrentDomain.UnhandledException += ...
```

Thông báo UI:

```text
EduFee gặp lỗi khi xử lý yêu cầu.
Chi tiết đã được ghi vào nhật ký.
```

---

# Phase 7 — Củng cố Regression Suite

Không đặt mục tiêu bằng số lượng test cố định. Ưu tiên coverage các invariant quan trọng.

## Test cần bổ sung

- [x] Schema migration failure/retry.
- [x] StudentCode unique.
- [x] StudentCode migration.
- [x] CSV StudentCode round-trip.
- [x] Receipt snapshot StudentCode.
- [ ] Semester tuition rate.
- [ ] Historical fee không đổi khi rate đổi.
- [ ] Demo totals deterministic.
- [ ] Logger failure không làm ứng dụng crash.

## Invariant phải luôn được bảo vệ

- [x] Payment update và receipt insert atomic.
- [x] Receipt insert failure rollback payment.
- [x] PaidAmount không được sửa trực tiếp.
- [x] Không thu vượt số còn lại.
- [x] Financial history không bị xóa qua foreign key.
- [x] SQLite chỉ nhận số tiền nguyên VND.
- [x] Backup/Restore từ chối ledger không hợp lệ.
- [x] Historical receipt snapshot không thay đổi sau khi sửa hồ sơ sinh viên.

---

# Phase 8 — UI Cleanup

UI hiện tại đã có design system ổn định. Phase này chỉ thực hiện cleanup nhỏ, không redesign toàn bộ.

### Thực hiện

- [ ] Xóa dead constants/fonts trong `UITheme`.
- [ ] Kiểm tra TextBox/ComboBox còn lệch style.
- [ ] Kiểm tra tab order.
- [ ] Kiểm tra Enter/Escape trong dialog.
- [ ] Kiểm tra action destructive.
- [ ] Kiểm tra text tiếng Việt bị cắt.
- [ ] Giảm emoji còn sót nếu không cần thiết.

### Ngoài phạm vi

- Dark mode.
- Animation framework.
- Custom title bar.
- Material UI framework.
- DevExpress/Telerik.
- Blur/shadow phức tạp.

---

# Phase 9 — Windows, DPI và Print QA

## 9.1. Resolution

Kiểm thử tối thiểu:

```text
1160 × 680
1320 × 800
1366 × 768
1920 × 1080
```

## 9.2. DPI

```text
100%
125%
150%
```

### Checklist

- [ ] Không cắt chữ tiếng Việt.
- [ ] Button không mất text.
- [ ] DataGridView không vỡ layout.
- [ ] Dialog không vượt màn hình.
- [ ] Sidebar không overlap.
- [ ] Header semester badge không đè title.
- [ ] QR dialog hiển thị đúng.
- [ ] FormPayment xử lý đúng các giá trị tiền lớn.
- [ ] Print Preview biên lai hoạt động.
- [ ] Microsoft Print to PDF hoạt động.
- [ ] Kiểm thử trên máy in vật lý khi môi trường kiểm thử hỗ trợ.

---

# Phase 10 — Release và Tài liệu kỹ thuật

## 10.1. Release build

```powershell
dotnet publish 26K1_DotNet -c Release -o output/publish --self-contained false
```

Bản publish phải được kiểm tra trực tiếp trên máy Windows mục tiêu.

## 10.2. README

README phải phản ánh đúng trạng thái hệ thống:

- Build/run.
- Demo mode.
- Test.
- Database path.
- Backup/Restore.
- VietQR là mô phỏng.
- PDF sử dụng raster rendering.
- Print Preview.
- Windows/.NET Runtime requirements.

## 10.3. Tài liệu kỹ thuật

Cấu trúc khuyến nghị:

```text
Docs/
├── requirements.md
├── use-case.md
├── architecture.md
├── database.md
├── test-plan.md
└── demo-script.md
```

### Architecture

```text
WinForms UI
    ↓
Service Layer
    ↓
SqliteRepository
    ↓
SQLite
```

### Payment sequence

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

Khi có lỗi:

```text
ROLLBACK
```

---

# Phase 11 — Kịch bản nghiệm thu chức năng

Luồng kiểm thử end-to-end:

1. Khởi chạy `--demo`.
2. Kiểm tra dashboard và thống kê.
3. Tìm kiếm sinh viên.
4. Thêm hoặc sửa sinh viên.
5. Lập học phí theo tín chỉ.
6. Áp dụng miễn giảm.
7. Thu một phần bằng tiền mặt.
8. Kiểm tra biên lai.
9. Xuất biên lai PDF.
10. Thực hiện thanh toán VietQR mô phỏng.
11. Kiểm tra trạng thái thanh toán.
12. Lọc công nợ theo học kỳ/lớp.
13. Xuất báo cáo PDF.
14. Backup database.
15. Xác nhận transaction, receipt snapshot và rollback behavior.

---

# Phase 12 — Tính năng tùy chọn sau bản lõi

Chỉ triển khai khi toàn bộ roadmap bắt buộc đã ổn định.

## 12.1. Hồ sơ tài chính sinh viên

Màn hình tổng hợp:

- Học kỳ.
- Phiếu học phí.
- Biên lai.
- Tổng đã nộp.
- Tổng còn nợ.

## 12.2. Điều chỉnh / hủy khoản thu

Không xóa biên lai gốc.

Thiết kế theo audit trail:

```text
Original Receipt
      ↓
Adjustment / Cancellation
      ↓
Reason
CreatedAt
ReferenceReceiptId
```

## 12.3. Aging công nợ

```text
Chưa đến hạn
1–30 ngày
31–60 ngày
> 60 ngày
```

## 12.4. Cấu hình tổ chức

Cho phép cấu hình:

- Tên đơn vị.
- Logo.
- Thông tin tài khoản hiển thị.
- Nội dung nhắc nợ.
- Footer/chữ ký báo cáo.

---

# Thứ tự triển khai

```text
1. Fix migration version
        ↓
2. StudentCode
        ↓
3. Regression cho migration + StudentCode
        ↓
4. GitHub Actions CI
        ↓
5. TuitionPerCredit theo Semester
        ↓
6. Demo data
        ↓
7. Logging + global exception handling
        ↓
8. UI cleanup
        ↓
9. Windows / DPI / Print QA
        ↓
10. Documentation
        ↓
11. Final Release
```

---

# Mức ưu tiên

## P0 — Bắt buộc

- [x] Fix migration version.
- [x] Regression test migration failure/retry.
- [x] Tách StudentCode khỏi Id.
- [x] Regression StudentCode.
- [x] Build, regression và self-test pass.
- [ ] Release QA trên Windows.

## P1 — Khuyến nghị

- [x] GitHub Actions CI.
- [ ] TuitionPerCredit theo Semester.
- [ ] Demo dataset lớn hơn.
- [ ] DPI QA.
- [ ] Print Preview / Print to PDF QA.
- [ ] README và tài liệu kỹ thuật cuối.

## P2 — Tùy chọn

- [ ] Persistent logging.
- [ ] Global exception handling.
- [ ] UITheme cleanup.
- [ ] Hồ sơ tài chính sinh viên.
- [ ] Aging công nợ.
- [ ] Adjustment/cancellation audit trail.

---

# Definition of Done

Một bản Release được coi là hoàn thiện khi:

- [ ] Release build thành công.
- [ ] Regression suite pass.
- [ ] Self-test pass.
- [ ] CI pass.
- [ ] Migration schema cũ được kiểm thử.
- [ ] StudentCode hoạt động xuyên suốt hệ thống.
- [ ] Payment/Receipt giữ tính atomic.
- [ ] Receipt snapshot chính xác.
- [ ] Demo không tác động dữ liệu vận hành.
- [ ] PDF receipt/debt report hoạt động.
- [ ] Print Preview hoạt động.
- [ ] Backup/Restore hoạt động.
- [ ] UI ổn định ở các DPI mục tiêu.
- [ ] README phản ánh đúng chức năng.
- [ ] Kịch bản nghiệm thu end-to-end chạy thành công.

---

## Ngoài phạm vi bắt buộc

- Module quản lý lớp riêng.
- Đăng nhập/phân quyền nhiều vai trò.
- Web/API.
- Mobile app.
- Cloud sync.
- Cổng sinh viên.
- Thanh toán ngân hàng tự động.
- Microservices.
- AI.
- Dark mode.
- Rewrite sang WPF/WinUI.

> **Mục tiêu cuối:** duy trì EduFee như một ứng dụng WinForms quản lý học phí ổn định, có dữ liệu tài chính đáng tin cậy, giao diện nhất quán, kiểm thử tự động và quy trình phát hành rõ ràng.
