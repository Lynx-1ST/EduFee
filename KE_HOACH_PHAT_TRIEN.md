# EduFee — Roadmap Hoàn thiện Dự án

> **Phạm vi:** Đồ án .NET 10 Windows Forms quản lý sinh viên, học kỳ, học phí, công nợ và biên lai trên một máy Windows.  
> **Mục tiêu:** Ưu tiên độ tin cậy dữ liệu, mô hình sinh viên rõ ràng, kiểm thử tự động, cấu hình học phí, dữ liệu demo và chất lượng bản Release trước khi nộp/bảo vệ.  
> **Không phát triển module quản lý lớp riêng.** `ClassName` tiếp tục được dùng để nhập liệu, lọc và thống kê trong phạm vi đồ án.

---

## 1. Trạng thái hiện tại

| Khu vực | Trạng thái |
| --- | --- |
| Sinh viên | CRUD, tìm kiếm, lọc lớp, CSV import/export |
| Học kỳ | CRUD, học kỳ hiện tại, hạn nộp |
| Học phí | Tính theo tín chỉ, miễn giảm, công nợ, trạng thái thanh toán |
| Thanh toán | Thu một phần/đủ, chặn thu vượt số dư |
| Biên lai | Snapshot lịch sử, Print Preview, PDF, email |
| VietQR | Luồng mô phỏng phục vụ demo, không kết nối ngân hàng thật |
| SQLite | Source of truth, FK, constraints, schema versioning |
| Transaction | Update học phí + tạo biên lai trong cùng transaction |
| Backup/Restore | Có integrity/schema validation và safety backup |
| Báo cáo | CSV, PDF biên lai, PDF công nợ đa trang |
| Demo mode | `--demo` tách biệt dữ liệu thật |
| Regression | Có acceptance/regression runner cho finance, DB, migration, PDF, report, QR, startup và UI |
| UI/UX | Design system Indigo + Slate, High-DPI, HUMG branding |

---

## 2. Nguyên tắc phát triển

Các thay đổi tiếp theo phải giữ các invariant sau:

- Giữ **.NET 10 WinForms**.
- Giữ **SQLite** là nguồn dữ liệu vận hành.
- Tiền tiếp tục lưu dưới dạng **INTEGER VND** trong SQLite.
- Không sửa trực tiếp `PaidAmount` ngoài luồng thanh toán có biên lai.
- Payment + Receipt luôn phải **atomic**.
- Receipt lịch sử phải giữ **snapshot bất biến**.
- Migration phải có rollback và regression test.
- Không thêm Web API, cloud database, WPF/WinUI hoặc framework UI nặng.
- Không phát triển module quản lý lớp riêng.
- VietQR chỉ là mô phỏng trong phạm vi đồ án.

---

# Phase 1 — Sửa độ tin cậy Schema Migration

## 1.1. Sửa version transition v1 → v2 → v3

### Vấn đề cần xử lý

`MigrateV1ToV2()` không được ghi thẳng `CurrentSchemaVersion` nếu vẫn còn bước v2 → v3 phía sau.

### Thực hiện

- [ ] `MigrateV1ToV2()` chỉ đặt `PRAGMA user_version = 2`.
- [ ] `MigrateV2ToV3()` chỉ đặt version 3 sau khi toàn bộ migration thành công.
- [ ] Version không thay đổi nếu transaction rollback.
- [ ] Kiểm tra schema thực tế tương ứng với version khai báo.

### Regression

- [ ] v1 → v2 → v3 thành công.
- [ ] v2 → v3 thành công.
- [ ] Inject failure giữa migration v2 → v3.
- [ ] Sau failure database vẫn ở version 2.
- [ ] Lần chạy tiếp có thể retry.
- [ ] Record count và tổng tiền không đổi sau retry.

### Definition of Done

- [ ] Không còn trường hợp `user_version` và schema thực tế lệch nhau.
- [ ] Regression pass.
- [ ] Self-test pass.

---

# Phase 2 — Chuẩn hóa Mã sinh viên

## 2.1. Tách `StudentCode` khỏi `Student.Id`

### Mục tiêu

Không sử dụng khóa chính database làm mã sinh viên nghiệp vụ.

Thiết kế:

```text
Id          = khóa chính nội bộ
StudentCode = mã sinh viên hiển thị cho người dùng
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

- [ ] `Student.cs`
- [ ] SQLite schema + migration
- [ ] `SqliteRepository`
- [ ] `SqlDataMigrator`
- [ ] `StudentService`
- [ ] CSV import/export
- [ ] Search/filter sinh viên
- [ ] PanelStudents
- [ ] PanelTuition
- [ ] PanelStatistics
- [ ] FormStudentDetail
- [ ] Receipt snapshot
- [ ] Email receipt
- [ ] PDF receipt
- [ ] Debt report
- [ ] Demo data
- [ ] Regression tests

### Business rules

- [ ] StudentCode bắt buộc.
- [ ] StudentCode unique.
- [ ] Trim trước khi lưu.
- [ ] Foreign key vẫn dùng `StudentId`, không dùng StudentCode.
- [ ] Receipt cũ giữ snapshot cũ.
- [ ] Receipt mới lưu StudentCode thật.

### Migration dữ liệu cũ

Nếu dữ liệu cũ chưa có mã sinh viên:

```text
SV0001
SV0002
...
```

được dùng làm mã legacy tạm thời.

Không đổi `Id`, không phá foreign key.

### Definition of Done

- [ ] UI không còn suy ra mã SV từ database Id.
- [ ] Duplicate StudentCode bị từ chối.
- [ ] CSV round-trip giữ đúng StudentCode.
- [ ] Receipt/PDF/email hiển thị StudentCode thật.
- [ ] Migration + regression pass.

---

# Phase 3 — Đơn giá học phí theo học kỳ

## 3.1. Bổ sung `TuitionPerCredit`

### Mục tiêu

Không hard-code 620.000 VNĐ/tín chỉ trong domain chính.

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
- [ ] Migration schema.
- [ ] Validation `TuitionPerCredit > 0`.
- [ ] Form quản lý học kỳ cho nhập đơn giá.
- [ ] Form lập học phí dùng đơn giá học kỳ.
- [ ] Batch tuition dùng đúng đơn giá học kỳ.
- [ ] Demo data cập nhật.
- [ ] Regression tests.

### Quy tắc lịch sử

- Phiếu học phí đã tạo giữ nguyên `TotalAmount`.
- Thay đổi đơn giá học kỳ không tự sửa dữ liệu lịch sử.
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

- [ ] Test chỉ dùng dữ liệu tạm.
- [ ] Không truy cập database người dùng.
- [ ] Không yêu cầu SMTP thật.
- [ ] Không yêu cầu máy in thật.
- [ ] Build/regression fail phải làm CI fail.

### Definition of Done

- [ ] Mỗi commit/PR có trạng thái CI.
- [ ] Release build pass.
- [ ] Regression pass.
- [ ] Self-test pass.

---

# Phase 5 — Nâng Demo Mode

## 5.1. Mở rộng dataset của `--demo`

### Hiện tại

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

### Cần có đầy đủ trạng thái

- [ ] Chưa nộp.
- [ ] Nộp một phần.
- [ ] Đã nộp đủ.
- [ ] Quá hạn.
- [ ] Nộp muộn.
- [ ] Có miễn giảm.
- [ ] Một sinh viên có nhiều lần thanh toán.

### Yêu cầu

- [ ] Demo luôn dùng database riêng.
- [ ] Không gửi email thật.
- [ ] Không tác động dữ liệu chính.
- [ ] Tổng tiền deterministic để regression test được.
- [ ] Dashboard/Statistics có dữ liệu đủ đẹp khi trình chiếu.

---

# Phase 6 — Logging và Global Error Handling

## 6.1. AppLogger

Tạo:

```text
Helpers/AppLogger.cs
```

Log tại:

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

Không log:

- SMTP password.
- Credential plaintext.
- Thông tin nhạy cảm không cần thiết.

## 6.2. Global exception handling

Trong `Program.cs`:

```csharp
Application.ThreadException += ...
AppDomain.CurrentDomain.UnhandledException += ...
```

Thông báo UI chỉ nên cho biết:

```text
EduFee gặp lỗi khi xử lý yêu cầu.
Chi tiết đã được ghi vào nhật ký.
```

---

# Phase 7 — Củng cố Regression Suite

Không đặt mục tiêu bằng một con số test cố định.

Ưu tiên coverage các invariant quan trọng.

## Test cần bổ sung

- [ ] Schema migration failure/retry.
- [ ] StudentCode unique.
- [ ] StudentCode migration.
- [ ] CSV StudentCode round-trip.
- [ ] Receipt snapshot StudentCode.
- [ ] Semester tuition rate.
- [ ] Historical fee không đổi khi rate đổi.
- [ ] Demo totals deterministic.
- [ ] Logger failure không làm app crash.

## Invariant phải luôn được bảo vệ

- [ ] Payment update + receipt insert atomic.
- [ ] Receipt insert failure rollback payment.
- [ ] PaidAmount không được sửa trực tiếp.
- [ ] Không thu vượt số còn lại.
- [ ] Financial history không bị xóa qua FK.
- [ ] SQLite chỉ nhận số tiền nguyên VND.
- [ ] Backup/Restore từ chối ledger không hợp lệ.
- [ ] Historical receipt snapshot không thay đổi sau khi sửa hồ sơ sinh viên.

---

# Phase 8 — UI Cleanup nhỏ

UI hiện tại đã đủ tốt; không redesign lớn nữa.

Chỉ cleanup:

- [ ] Xóa dead constants/fonts trong `UITheme`.
- [ ] Kiểm tra TextBox/ComboBox còn lệch style.
- [ ] Kiểm tra tab order.
- [ ] Kiểm tra Enter/Escape trong dialog.
- [ ] Kiểm tra action nguy hiểm.
- [ ] Kiểm tra text tiếng Việt bị cắt.
- [ ] Giảm emoji còn sót nếu không cần.

Không làm:

- Dark mode.
- Animation framework.
- Custom title bar.
- Material UI framework.
- DevExpress/Telerik.
- Blur/shadow phức tạp.

---

# Phase 9 — Windows / DPI / Print QA

## Resolution

Test tối thiểu:

```text
1160 × 680
1320 × 800
1366 × 768
1920 × 1080
```

## DPI

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
- [ ] FormPayment xử lý số tiền lớn.
- [ ] Print Preview biên lai hoạt động.
- [ ] Microsoft Print to PDF hoạt động.
- [ ] Nếu có điều kiện, thử máy in vật lý.

---

# Phase 10 — Release và Tài liệu bảo vệ

## 10.1. Release build

```powershell
dotnet publish 26K1_DotNet -c Release -o output/publish --self-contained false
```

Sau đó chạy trực tiếp từ thư mục publish trên máy Windows đích.

## 10.2. README

README cuối phải mô tả đúng:

- Build/run.
- Demo mode.
- Test.
- Database path.
- Backup/Restore.
- VietQR là mô phỏng.
- PDF là raster.
- Print Preview.
- Windows/.NET Runtime requirements.

## 10.3. Tài liệu kỹ thuật

Nên có:

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

Nếu lỗi:

```text
ROLLBACK
```

---

# Phase 11 — Kịch bản Demo

Luồng trình diễn khuyến nghị:

1. Chạy `--demo`.
2. Xem dashboard/thống kê.
3. Tìm kiếm sinh viên.
4. Thêm hoặc sửa sinh viên.
5. Lập học phí theo tín chỉ.
6. Áp dụng miễn giảm.
7. Thu một phần bằng tiền mặt.
8. Xem biên lai.
9. Xuất biên lai PDF.
10. Thu khoản tiếp theo bằng VietQR mô phỏng.
11. Kiểm tra trạng thái thanh toán.
12. Lọc công nợ theo học kỳ/lớp.
13. Xuất báo cáo PDF.
14. Backup database.
15. Trình bày transaction + receipt snapshot + rollback test.

---

# Phase 12 — Tính năng tùy chọn sau bản nộp

Chỉ thực hiện khi toàn bộ roadmap bắt buộc đã ổn định.

## Hồ sơ tài chính sinh viên

Một màn hình tổng hợp:

- Học kỳ.
- Phiếu học phí.
- Biên lai.
- Tổng đã nộp.
- Tổng còn nợ.

## Điều chỉnh / hủy khoản thu

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

## Aging công nợ

```text
Chưa đến hạn
1–30 ngày
31–60 ngày
> 60 ngày
```

## Cấu hình tổ chức

Cho phép cấu hình:

- Tên trường.
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
8. UI cleanup nhỏ
        ↓
9. Windows / DPI / Print QA
        ↓
10. Documentation + Demo Script
        ↓
11. Final Release
```

---

# Ưu tiên

## P0 — Trước khi nộp

- [ ] Fix migration version.
- [ ] Regression test migration failure/retry.
- [ ] Tách StudentCode khỏi Id.
- [ ] Regression StudentCode.
- [ ] Build + regression + self-test pass.
- [ ] Release QA trên Windows.

## P1 — Rất nên làm

- [ ] GitHub Actions CI.
- [ ] TuitionPerCredit theo Semester.
- [ ] Demo dataset lớn hơn.
- [ ] DPI QA.
- [ ] Print Preview / Print to PDF QA.
- [ ] README + demo script cuối.

## P2 — Nếu còn thời gian

- [ ] Persistent logging.
- [ ] Global exception handling.
- [ ] UITheme cleanup.
- [ ] Hồ sơ tài chính sinh viên.
- [ ] Aging công nợ.
- [ ] Adjustment/cancellation audit trail.

---

# Definition of Done

EduFee được coi là sẵn sàng nộp khi:

- [ ] Release build thành công.
- [ ] Regression suite pass.
- [ ] Self-test pass.
- [ ] CI pass.
- [ ] Migration schema cũ được test.
- [ ] StudentCode hoạt động xuyên suốt hệ thống.
- [ ] Payment/Receipt vẫn atomic.
- [ ] Receipt snapshot đúng.
- [ ] Demo không chạm dữ liệu thật.
- [ ] PDF receipt/debt report hoạt động.
- [ ] Print Preview hoạt động.
- [ ] Backup/Restore hoạt động.
- [ ] UI không vỡ ở DPI phổ biến.
- [ ] README phản ánh đúng chức năng.
- [ ] Demo script chạy liền mạch.

---

## Ngoài phạm vi bắt buộc

Không đưa các hạng mục sau vào roadmap bắt buộc:

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

> **Mục tiêu cuối:** một ứng dụng WinForms quản lý học phí hoàn chỉnh, dữ liệu tài chính đáng tin cậy, UI đủ đẹp, có kiểm thử tốt và dễ bảo vệ trước giảng viên.
