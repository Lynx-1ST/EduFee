# Kế hoạch cải thiện UI/UX cho EduFee

## 1. Mục tiêu

Nâng cấp giao diện EduFee theo hướng:

- Hiện đại hơn WinForms mặc định.
- Đồng bộ màu sắc, font, spacing và control.
- Giữ nguyên **.NET WinForms thuần**.
- Không thay đổi business logic hiện tại.
- Không thêm UI framework nặng.
- Không over-engineer.
- Tối ưu hóa trải nghiệm trực quan và khả năng trình diễn dữ liệu rõ ràng.
- Đảm bảo UI vẫn hoạt động ổn ở nhiều độ phân giải và DPI khác nhau.

### Style mục tiêu

EduFee sẽ theo phong cách:

> **Modern Desktop Admin / Education Management**

Định hướng hình ảnh:

- Sidebar tối.
- Nội dung nền sáng.
- Indigo làm màu thương hiệu.
- Card đơn giản, bo góc nhẹ.
- DataGrid sạch.
- Ít màu nhưng có hierarchy rõ.
- Các trạng thái học phí dùng màu có ý nghĩa.
- Hạn chế emoji.
- Ưu tiên icon đơn sắc.

---

# 2. Phạm vi chỉnh sửa

Các file chính cần tác động:

```text
26K1_DotNet/
├── Helpers/
│   └── UITheme.cs
│
├── Controls/
│   ├── NavButton.cs
│   ├── PanelStudents.cs
│   ├── PanelTuition.cs
│   └── PanelStatistics.cs
│
└── Forms/
    ├── Form1.Designer.cs
    ├── Form1.cs
    ├── FormStudentDetail.cs
    ├── FormTuitionDetail.cs
    ├── FormPayment.cs
    ├── FormReceipt.cs
    ├── FormReceiptHistory.cs
    ├── FormSemesterManage.cs
    ├── FormBatchTuition.cs
    ├── FormImportStudents.cs
    ├── FormDebtNotice.cs
    ├── FormDatabaseConfig.cs
    └── FormEmailSettings.cs
```

Bảo toàn kiến trúc và logic nghiệp vụ lõi:

```text
Services/
Data/
Models/
Reports/
```

Chỉ mở rộng các API hoặc phương thức đọc khi cần thiết để hỗ trợ hiển thị dữ liệu lên giao diện.

---

# 3. Phase 1 — Chuẩn hóa Design System

## Mục tiêu

Biến `UITheme.cs` thành nguồn duy nhất quản lý style toàn ứng dụng.

Hiện tại project đã có nền khá tốt, nên không viết lại mà mở rộng.

---

## 3.1. Chuẩn hóa Color Palette

Giữ hệ màu Indigo + Slate hiện tại.

```text
Primary          #6366F1
Primary Dark     #4F46E5
Primary Light    #EEF2FF

Background       #F1F5F9
Surface          #FFFFFF
Surface Alt      #F8FAFC

Text Primary     #0F172A
Text Secondary   #64748B
Border           #E2E8F0

Success          #10B981
Warning          #F59E0B
Danger           #EF4444
Info             #0EA5E9
```

### Quy tắc sử dụng

- Indigo: hành động chính.
- Green: đã thanh toán / thành công.
- Amber: thanh toán một phần.
- Red: còn nợ / quá hạn / nguy hiểm.
- Slate: thông tin phụ.

Không thêm quá nhiều màu mới.

---

# 4. Phase 2 — Chuẩn hóa Typography

Sử dụng `Segoe UI` toàn bộ ứng dụng.

## Font hierarchy

```text
Page title        16pt Bold
Section title     11–12pt Bold
Card value        16–18pt Bold
Body              9.5–10pt
Secondary         8.5–9pt
Button            9.5pt Semibold/Bold
Table header      9pt Bold
```

### Điều chỉnh

Hạn chế:

```text
TỔNG HỌC PHÍ
ĐÃ THU
CÒN PHẢI THU
```

Ưu tiên:

```text
Tổng học phí
Đã thu
Còn phải thu
```

Chữ hoa toàn bộ chỉ dùng cho những vị trí thật sự cần nhấn mạnh.

---

# 5. Phase 3 — Chuẩn hóa Spacing và kích thước

## Spacing scale

```text
4px
8px
12px
16px
24px
28px
32px
```

Quy ước:

```text
Page horizontal padding     28px
Card padding                16–20px
Section spacing             24px
Control spacing             8–12px
```

## Control height

```text
Small button        32px
Normal button       36px
Primary button      38px
Dialog action       40px

TextBox             36–38px
ComboBox            36–38px
Grid row            42–44px
Grid header         44–46px
```

---

# 6. Phase 4 — Xây reusable UI components

## File

```text
Helpers/UITheme.cs
```

## Bổ sung helper

Dự kiến thêm:

```csharp
CreateRoundedCard()
CreateStatCard()
CreateSearchInput()
CreateInputContainer()
CreateStyledComboBox()
CreateSectionHeader()
CreateIconButton()
CreateEmptyState()
CreateToolbar()
CreateStatusBadge()
```

## Mục tiêu

Không còn tình trạng:

```csharp
var panel = new Panel();
panel.BackColor = ...
panel.Paint += ...
```

lặp lại ở hàng chục form khác nhau.

Thay bằng:

```csharp
var card = UITheme.CreateRoundedCard();
```

Điều này giúp chỉnh theme sau này chỉ cần sửa một nơi.

---

# 7. Phase 5 — Cải thiện Card

Card hiện tại phần lớn vẫn dùng rectangle vuông.

## Thiết kế mới

```text
Radius: 8–10px
Border: 1px
Background: White
Shadow: Không cần hoặc cực nhẹ
```

Ví dụ:

```text
┌─────────────────────────┐
│ Tổng phải thu           │
│                         │
│ 245.800.000 ₫           │
│                         │
│ 124 phiếu học phí       │
└─────────────────────────┘
```

### Stat card

Có accent line ở bên trái:

```text
▌ Tổng phải thu
▌ 245.800.000 ₫
```

Không cần nhiều gradient.

---

# 8. Phase 6 — Cải thiện Input Controls

Đây là điểm hiện tại còn khá "WinForms".

## TextBox

Hiện tại:

```text
BorderStyle.FixedSingle
```

Sẽ chuyển sang container custom:

```text
╭──────────────────────────────╮
│ 🔍 Tìm tên hoặc mã sinh viên │
╰──────────────────────────────╯
```

### States

Có:

- Normal
- Hover
- Focus
- Error
- Disabled

Focus:

```text
Border = Primary
```

Error:

```text
Border = Danger
```

---

## ComboBox

ComboBox cần đồng bộ với TextBox:

```text
╭────────────────────╮
│ Học kỳ 1 2026–2027 ▾
╰────────────────────╯
```

Không để ComboBox native quá lệch so với card và button.

---

# 9. Phase 7 — Làm lại Icon System

## Vấn đề hiện tại

Đang dùng khá nhiều:

```text
🎓
💰
📊
⚙️
📅
🔄
➕
⚡
📥
```

Emoji có màu và style riêng của Windows nên không đồng bộ.

## Hướng mới

Giảm emoji xuống mức tối thiểu.

### Có thể giữ

```text
🎓
```

chỉ ở logo EduFee.

### Menu

Chuyển sang icon monochrome:

```text
Students
Tuition
Statistics
Settings
Calendar
Search
Export
Payment
```

Có thể tự vẽ bằng `System.Drawing`.

Không cần thêm thư viện icon nếu không cần thiết.

---

# 10. Phase 8 — Main Shell

## Files

```text
Forms/Form1.Designer.cs
Forms/Form1.cs
Controls/NavButton.cs
```

---

## 10.1. Sidebar

Giữ:

- Dark sidebar.
- Logo EduFee.
- Navigation.
- Semester widget.

### Bỏ

```text
PRO
Quản trị viên
● Trực tuyến · v1.2
```

Vì đây là những thông tin không có nghiệp vụ thực sự.

---

## 10.2. Sidebar mới

```text
┌────────────────────────┐
│  🎓  EduFee            │
│      Quản lý học phí   │
│                        │
│ MENU                   │
│                        │
│  Sinh viên             │
│  Học phí               │
│  Thống kê              │
│  Cài đặt               │
│                        │
│ ┌────────────────────┐ │
│ │ Học kỳ hiện tại    │ │
│ │ HK1 · 2026–2027    │ │
│ │ ● Đang áp dụng     │ │
│ └────────────────────┘ │
│                        │
│ ────────────────────── │
│ ĐH Mỏ - Địa chất      │
└────────────────────────┘
```

---

# 11. Phase 9 — Navigation Button

## File

```text
Controls/NavButton.cs
```

Hiện tại NavButton khá tốt.

Chỉ cần simplify.

### Active

```text
Background: Primary
Text: White
Icon: White
Radius: 8px
```

### Hover

```text
Background: SidebarHover
```

### Normal

```text
Transparent
Text: Slate light
```

Giảm gradient để UI bớt "template SaaS".

---

# 12. Phase 10 — Header

Header mới:

```text
Quản lý học phí
Theo dõi tình trạng đóng học phí của sinh viên

                          Học kỳ 1 · 2026–2027
```

## Cải thiện

- Title rõ hơn.
- Subtitle nhỏ và muted.
- Semester badge bo tròn.
- Padding trái/phải đồng nhất.
- Không chứa quá nhiều action.

---

# 13. Phase 11 — Màn Sinh viên

## File

```text
Controls/PanelStudents.cs
```

## Toolbar mới

```text
Tìm kiếm sinh viên

[ Tên, mã SV, SĐT... ] [Lớp ▼] [Xóa bộ lọc]

                         [Nhập CSV] [Xuất CSV] [+ Thêm sinh viên]
```

Hoặc ở màn hình nhỏ:

```text
[Search...................................]

[Lớp ▼] [Xóa lọc]

[Nhập CSV] [Xuất CSV] [+ Thêm]
```

---

## Data table

```text
┌─────────────────────────────────────────────────────────────┐
│ 124 sinh viên                                              │
├────────┬───────────────────┬─────────────┬──────────┬───────┤
│ Mã SV  │ Họ và tên         │ Email       │ SĐT      │ Lớp   │
├────────┼───────────────────┼─────────────┼──────────┼───────┤
│ ...                                                         │
└─────────────────────────────────────────────────────────────┘
```

### Selection actions

Hiện tại:

```text
Xem học phí
Sửa hồ sơ
Xóa
```

Đổi thành:

```text
[Xem học phí] [Sửa] [⋯]
```

Menu `⋯`:

```text
Xóa sinh viên
Xuất thông tin
```

---

# 14. Phase 12 — Empty State

Nếu không có sinh viên:

```text
           Chưa có sinh viên

Thêm sinh viên đầu tiên hoặc nhập danh sách CSV.

         [+ Thêm sinh viên]
```

Nếu search không có kết quả:

```text
Không tìm thấy sinh viên

Thử thay đổi từ khóa hoặc bộ lọc.

[Xóa bộ lọc]
```

---

# 15. Phase 13 — Màn Học phí

## File

```text
Controls/PanelTuition.cs
```

Đây sẽ là màn UI chính của ứng dụng.

---

## Summary cards

```text
┌─────────────────┐
│ Tổng phải thu   │
│ 245.800.000 ₫   │
└─────────────────┘

┌─────────────────┐
│ Đã thu          │
│ 192.400.000 ₫   │
└─────────────────┘

┌─────────────────┐
│ Còn phải thu    │
│ 53.400.000 ₫    │
└─────────────────┘

┌─────────────────┐
│ Số phiếu        │
│ 124             │
└─────────────────┘
```

### Màu

```text
Tổng phải thu      Indigo
Đã thu             Green
Còn phải thu       Red
Số phiếu           Purple
```

---

# 16. Phase 14 — Toolbar Học phí

## Row 1

```text
[+ Lập phiếu] [Tạo theo lớp]

                          [🔍 Tìm sinh viên................]
```

## Row 2

```text
Học kỳ
[2026–2027 HK1 ▼]

Trạng thái
[Tất cả ▼]

[Xóa bộ lọc]
```

Các tác vụ ít dùng đưa vào:

```text
[Tác vụ khác ▾]
```

---

# 17. Phase 15 — Bảng học phí

Tập trung vào các trường quan trọng:

```text
Mã SV
Họ tên
Lớp
Học kỳ
Phải nộp
Đã nộp
Còn lại
Trạng thái
```

## Format tiền

```text
12.400.000 ₫
```

Căn phải.

## Trạng thái

```text
Đã nộp đủ       Green
Nộp một phần    Amber
Chưa nộp        Gray
Quá hạn         Red
```

Dùng pill badge.

---

# 18. Phase 16 — Màn Thống kê

## File

```text
Controls/PanelStatistics.cs
```

Màn hiện tại đã khá tốt, chủ yếu cần polish.

## Layout đề xuất

```text
Thống kê học phí

[Học kỳ 1 · 2026–2027 ▼]                     [Làm mới]


┌───────────────┐ ┌───────────────┐
│ Tổng học phí  │ │ Đã thu        │
│ 245.8 triệu   │ │ 192.4 triệu   │
└───────────────┘ └───────────────┘

┌───────────────┐ ┌───────────────┐
│ Còn phải thu  │ │ Số phiếu      │
│ 53.4 triệu    │ │ 124           │
└───────────────┘ └───────────────┘


Tiến độ thu học phí

██████████████████░░░░░░░░    78%


Đã nộp đủ     Nộp một phần      Chưa nộp       Quá hạn
83             21                14              6
```

Sau đó là bảng công nợ.

---

# 19. Phase 17 — Bảng công nợ

Header:

```text
Sinh viên còn nợ

[Còn nợ] [Tất cả] [Theo lớp]

                   [Thu tiền] [Giấy báo nợ] [Xuất PDF] [⋯]
```

Menu `⋯`:

```text
Xuất CSV
```

PDF là yêu cầu chính nên được ưu tiên visual hơn CSV.

---

# 20. Phase 18 — Form Thu tiền

## File

```text
Forms/FormPayment.cs
```

Đây là form nghiệp vụ quan trọng nhất.

## Layout

```text
┌───────────────────────────────────────┐
│ Thu học phí                           │
│ Nguyễn Văn A · SV001                  │
├───────────────────────────────────────┤
│                                       │
│ Học kỳ                                │
│ HK1 · 2026–2027                       │
│                                       │
│ Tổng phải nộp           12.400.000 ₫  │
│ Đã nộp                   5.000.000 ₫  │
│ ───────────────────────────────────── │
│ Còn phải nộp             7.400.000 ₫  │
│                                       │
│ Số tiền thanh toán                    │
│ ╭───────────────────────────────────╮ │
│ │ 5.000.000                         │ │
│ ╰───────────────────────────────────╯ │
│                                       │
│ Phương thức thanh toán                │
│ [Tiền mặt ▼]                          │
│                                       │
│              [Hủy] [Xác nhận thu]     │
└───────────────────────────────────────┘
```

### Số tiền thanh toán

Là field nổi bật nhất:

```text
Font lớn hơn
Border Primary khi focus
```

---

# 21. Phase 19 — Form Sinh viên

## Files

```text
Forms/FormStudentDetail.cs
Forms/FormStudentDetail.Designer.cs
```

## Layout mới

```text
Thông tin sinh viên

Mã sinh viên
[............................]

Họ và tên
[............................]

Lớp                     Ngày sinh
[...............]        [..............]

Email
[............................]

Số điện thoại
[............................]


                        [Hủy] [Lưu thay đổi]
```

Không nhồi quá nhiều field trên một hàng.

---

# 22. Phase 20 — Form Học phí

## File

```text
Forms/FormTuitionDetail.cs
```

Phân thành section:

```text
Sinh viên
────────────

Học kỳ
────────────

Thông tin học phí
────────────
Số tín chỉ
Đơn giá
Miễn giảm

Tổng phải nộp
────────────
12.400.000 ₫
```

Tổng tiền nên được highlight mạnh.

---

# 23. Phase 21 — Biên lai

## File

```text
Forms/FormReceipt.cs
```

Không cần quá màu mè.

## Layout

```text
TRƯỜNG ĐẠI HỌC MỎ - ĐỊA CHẤT

             BIÊN LAI THU HỌC PHÍ

Mã biên lai: BL000123
Ngày: 19/09/2026

──────────────────────────────────

Sinh viên
Nguyễn Văn A

Mã sinh viên
212105xxxx

Lớp
DCCTCT66A

Học kỳ
HK1 · 2026–2027

──────────────────────────────────

SỐ TIỀN THANH TOÁN

5.000.000 ₫

──────────────────────────────────

Đã nộp:       10.000.000 ₫
Còn lại:       2.400.000 ₫


Người nộp                Người thu
```

---

# 24. Phase 22 — PDF Report

## Mục tiêu

Giữ PDF chuyên nghiệp nhưng dễ in.

### Header

```text
Trường Đại học Mỏ - Địa chất

BÁO CÁO CÔNG NỢ HỌC PHÍ

Học kỳ: HK1 · 2026–2027
Ngày xuất: 19/09/2026
```

### Table

```text
STT
Mã SV
Họ tên
Lớp
Phải nộp
Đã nộp
Còn nợ
```

### Footer

```text
Tổng phải thu
Tổng đã thu
Tổng còn nợ
```

Tránh dùng nền màu quá đậm để PDF in trắng đen vẫn đọc tốt.

---

# 25. Phase 23 — Các dialog phụ

Sau khi các màn chính hoàn chỉnh mới chỉnh:

```text
FormSemesterManage
FormBatchTuition
FormReceiptHistory
FormImportStudents
FormDebtNotice
FormDatabaseConfig
FormEmailSettings
```

Tất cả dùng cùng:

- Dialog header.
- Input style.
- Button layout.
- Padding.
- Footer.
- Validation.

---

# 26. Phase 24 — Responsive WinForms

Test tối thiểu:

```text
1160 × 680
1320 × 800
1366 × 768
1920 × 1080
```

## Nguyên tắc

Ưu tiên:

```text
Dock
Anchor
FlowLayoutPanel
TableLayoutPanel
```

Hạn chế absolute positioning nếu không thực sự cần.

---

# 27. Phase 25 — High DPI

Test:

```text
100%
125%
150%
```

Kiểm tra:

- Text không bị cắt.
- Button không mất chữ.
- Grid không vỡ.
- Dialog không vượt màn hình.
- Sidebar không bị lệch.
- ComboBox không quá thấp.

---

# 28. Phase 26 — UX polish

Sau khi UI đã đẹp mới thêm polish.

## Hover

Button:

```text
Normal
Hover
Pressed
Disabled
```

Navigation:

```text
Normal
Hover
Active
```

Input:

```text
Normal
Focus
Error
Disabled
```

---

# 29. Phase 27 — Confirm dialog

Chuẩn hóa message khi xóa.

Không dùng text kiểu:

```text
Xác nhận thực hiện thao tác?
```

Mà dùng:

```text
Xóa sinh viên?

Sinh viên Nguyễn Văn A sẽ bị xóa khỏi hệ thống.

Hành động này không thể hoàn tác.

[Hủy] [Xóa sinh viên]
```

Nếu dữ liệu có liên quan tài chính thì không cho xóa.

---

# 30. Phase 28 — Loading và feedback

Các thao tác như:

```text
Import CSV
Export PDF
Backup database
Restore database
```

nên có feedback rõ.

Ví dụ:

```text
✓ Xuất báo cáo thành công
```

hoặc:

```text
Đang tạo báo cáo...
```

Không cần animation phức tạp.

---

# 31. Các ràng buộc và giới hạn kỹ thuật

Để duy trì kiến trúc gọn nhẹ và kiểm soát độ phức tạp:

- Không chuyển sang WPF.
- Không chuyển WinUI.
- Không dùng DevExpress.
- Không dùng Telerik.
- Không dùng MaterialSkin nếu không cần.
- Không làm dark mode.
- Không làm custom Windows title bar.
- Không acrylic/blur.
- Không animation phức tạp.
- Không thêm dependency chỉ để có shadow.
- Không viết lại service layer.
- Không đổi database vì lý do UI.

---

# 32. Thứ tự triển khai

```text
Phase 1
UITheme
   ↓
Phase 2
Reusable Controls
   ↓
Phase 3
Main Shell
   ↓
Phase 4
Sidebar / Navigation
   ↓
Phase 5
PanelStudents
   ↓
Phase 6
PanelTuition
   ↓
Phase 7
PanelStatistics
   ↓
Phase 8
FormPayment
   ↓
Phase 9
Student + Tuition Detail
   ↓
Phase 10
Receipt
   ↓
Phase 11
Secondary Dialogs
   ↓
Phase 12
Responsive + DPI
   ↓
Phase 13
Regression Testing
   ↓
Final Polish
```

---

# 33. Ưu tiên công việc

## P0 — Phải làm

- [ ] Chuẩn hóa `UITheme`
- [ ] Rounded card
- [ ] Button style
- [ ] TextBox / ComboBox style
- [ ] Sidebar cleanup
- [ ] `PanelStudents`
- [ ] `PanelTuition`
- [ ] `PanelStatistics`
- [ ] `FormPayment`
- [ ] Test DPI
- [ ] Không phá regression

## P1 — Nên làm

- [ ] Đồng bộ icon
- [ ] Empty state
- [ ] Context menu đẹp hơn
- [ ] Dialog thống nhất
- [ ] Receipt visual polish
- [ ] Confirm dialog
- [ ] Better validation feedback

## P2 — Nếu còn thời gian

- [ ] Custom tooltip
- [ ] Toast notification
- [ ] Skeleton/loading state
- [ ] Micro interaction nhẹ
- [ ] Custom chart đơn giản

---

# 34. Tiêu chí nghiệm thu UI

## Visual

- [ ] Không còn cảm giác WinForms mặc định rõ rệt.
- [ ] Màu sắc đồng nhất.
- [ ] Font đồng nhất.
- [ ] Button đồng nhất.
- [ ] Input đồng nhất.
- [ ] Card đồng nhất.
- [ ] Grid đồng nhất.
- [ ] Icon cùng phong cách.
- [ ] Không lạm dụng emoji.
- [ ] Không có thông tin giả như `PRO`, `Online`.

## UX

- [ ] Action chính luôn rõ.
- [ ] Action nguy hiểm không nằm cạnh primary action.
- [ ] Search/filter dễ hiểu.
- [ ] Empty state rõ ràng.
- [ ] Disabled state dễ nhận biết.
- [ ] Form nhập liệu dễ đọc.
- [ ] Thanh toán dễ thao tác.
- [ ] PDF export dễ tìm.

## Technical

- [ ] Build thành công.
- [ ] Regression tests pass.
- [ ] Self-test pass.
- [ ] Không thêm dependency UI ngoài không cần thiết.
- [ ] Không thay đổi business logic.
- [ ] Không phá SQLite.
- [ ] Không phá PDF.
- [ ] Không phá import/export.
- [ ] Không phá High-DPI.

---

# 35. Kết quả cuối cùng mong muốn

Sau khi hoàn thành, EduFee nên có cảm giác như:

```text
Modern WinForms Desktop Application
        +
Education Management Dashboard
        +
Simple Financial Administration UI
```

chứ không còn cảm giác:

```text
Form
+ Button
+ DataGridView
+ ComboBox
```

Mục tiêu cốt lõi là **giao diện hiện đại trên nền .NET WinForms thuần**, kiến trúc code sáng sủa, dễ bảo trì và không phụ thuộc vào các thư viện UI bên ngoài.
