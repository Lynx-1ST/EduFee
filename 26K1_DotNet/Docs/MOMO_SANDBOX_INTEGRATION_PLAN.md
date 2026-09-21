# EduFee — Kế hoạch tích hợp MoMo Sandbox

## 1. Mục tiêu

Tích hợp MoMo Sandbox như một phương thức thanh toán thử nghiệm bên cạnh VietQR hiện có.

Phần tích hợp cần đáp ứng các nguyên tắc:

- Giữ nguyên kiến trúc .NET 10 WinForms + SQLite.
- Không thay đổi cơ chế ghi nhận học phí và biên lai hiện tại.
- Chỉ ghi nhận thanh toán khi giao dịch MoMo được xác nhận thành công.
- Giữ `VietQrPaymentGateway` để sử dụng trong Demo Mode và môi trường không có Internet; trạng thái được xác nhận thủ công do không có API đối soát ngân hàng.
- Không yêu cầu backend public trong giai đoạn đầu.
- Không sử dụng tiền thật hoặc môi trường Production.

---

## 2. Kiến trúc mục tiêu

```text
FormPayment
    ↓
IPaymentGateway
    ├── VietQrPaymentGateway
    │       ↓
    │   Manual transfer confirmation
    │
    └── MomoSandboxPaymentGateway
            ↓
       MoMo Sandbox API
            ↓
       Create Payment
            ↓
       Payment URL / QR
            ↓
       Query Transaction
            ↓
       Payment Success
            ↓
TuitionService.RecordPaymentWithReceipt()
            ↓
SQLite Transaction
    ├── UPDATE TuitionFees
    └── INSERT PaymentReceipts
```

Việc tạo giao dịch MoMo không đồng nghĩa với việc học phí đã được thu.

Biên lai chỉ được phát hành sau khi trạng thái giao dịch được xác nhận thành công.

---

# Phase 1 — Chuẩn hóa Payment Gateway

## 1.1. Tạo interface chung

### Files

```text
Models/
└── PaymentGatewayModels.cs

Services/
└── IPaymentGateway.cs
```

### Interface dự kiến

```csharp
public interface IPaymentGateway
{
    Task<PaymentSession> CreatePaymentAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentGatewayStatus> QueryPaymentAsync(
        string orderId,
        CancellationToken cancellationToken = default);
}
```

### Models

```text
PaymentGatewayRequest
├── OrderId
├── Amount
├── Description
├── StudentCode
└── StudentName

PaymentSession
├── OrderId
├── RequestId
├── Provider
├── PaymentUrl
├── QrCodeUrl
├── CreatedAt
└── Status

PaymentGatewayStatus
├── OrderId
├── ProviderTransactionId
├── Amount
├── Status
├── ResultCode
└── Message
```

### Trạng thái chung

```csharp
public enum GatewayPaymentStatus
{
    Pending,
    Success,
    Failed,
    Cancelled,
    Expired,
    Unknown
}
```

---

# Phase 2 — Chuẩn hóa VietQR Gateway

`VietQrPaymentGateway` triển khai `IPaymentGateway`; người dùng xác nhận thủ công sau khi chuyển khoản.

Mục tiêu:

```text
FormPayment
      ↓
IPaymentGateway
      ↓
Provider cụ thể
```

UI và nghiệp vụ thanh toán không phụ thuộc trực tiếp vào một gateway cụ thể.

### Regression

- [x] VietQR Create Payment hoạt động.
- [x] VietQR Query Status hoạt động.
- [x] VietQR xác nhận thành công tạo đúng một receipt.
- [x] VietQR chưa xác nhận không tạo receipt.
- [x] Demo Mode không gọi dịch vụ bên ngoài.
- [x] CI không phụ thuộc Internet.

---

# Phase 3 — Cấu hình MoMo Sandbox

## 3.1. Settings model

### Files

```text
Models/
└── MomoSettings.cs

Services/
└── MomoSettingsService.cs
```

### Cấu trúc

```csharp
public class MomoSettings
{
    public bool Enabled { get; set; }

    public string PartnerCode { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    public bool UseSandbox { get; set; } = true;
}
```

## 3.2. Bảo vệ credential

`SecretKey` không được lưu plaintext trong repository hoặc file cấu hình.

Tận dụng cơ chế Windows DPAPI đang dùng cho SMTP password.

Ví dụ file cấu hình:

```text
momo-settings.json
├── PartnerCode
├── AccessKey
├── EncryptedSecretKey
└── Environment
```

### Yêu cầu

- [x] Không commit credential.
- [x] Không log `SecretKey`.
- [x] Không hiển thị secret hiện tại dưới dạng plaintext.
- [x] Demo Mode không yêu cầu MoMo credential.

---

# Phase 4 — Signature Service

## 4.1. HMAC-SHA256

### File

```text
Services/
└── MomoSignatureService.cs
```

Service chịu trách nhiệm:

- tạo raw signature theo đúng thứ tự field của API;
- encode UTF-8;
- ký HMAC-SHA256;
- trả về signature dạng hex theo yêu cầu của provider.

### Tests

- [x] Raw signature đúng thứ tự.
- [x] UTF-8 encoding ổn định.
- [x] HMAC-SHA256 deterministic.
- [x] Không đưa secret vào exception message.
- [x] Không ghi secret vào log.

---

# Phase 5 — MoMo Sandbox Gateway

## 5.1. Service

### File

```text
Services/
└── MomoSandboxPaymentGateway.cs
```

Sử dụng `HttpClient` có sẵn trong .NET.

Không thêm HTTP library nếu không cần thiết.

## 5.2. Create Payment

Luồng:

```text
EduFee
   ↓
Create Payment Request
   ↓
MoMo Sandbox
   ↓
Payment Session
   ↓
Payment URL / QR
```

Order ID cần độc lập với database ID.

Ví dụ:

```text
EDUFEE-20260920-A7B3C9
```

### Yêu cầu

- [x] `OrderId` unique.
- [x] `RequestId` unique.
- [x] Amount là số nguyên VND.
- [x] Request được ký đúng.
- [x] Response được validate trước khi sử dụng.

## 5.3. Query Payment

Gateway cung cấp chức năng kiểm tra trạng thái giao dịch dựa trên `OrderId`.

Response của provider được map về:

```text
GatewayPaymentStatus
```

Không để UI phụ thuộc trực tiếp vào mã trạng thái riêng của MoMo.

---

# Phase 6 — Lưu giao dịch gateway

## 6.1. Schema v5

Bổ sung bảng:

```text
PaymentGatewayTransactions
```

Schema dự kiến:

```sql
CREATE TABLE PaymentGatewayTransactions
(
    Id INTEGER PRIMARY KEY,
    Provider TEXT NOT NULL,
    OrderId TEXT NOT NULL UNIQUE,
    ProviderTransactionId TEXT,
    TuitionFeeId INTEGER NOT NULL,
    Amount INTEGER NOT NULL,
    Status INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    CompletedAt TEXT,
    RawResultCode TEXT,

    FOREIGN KEY (TuitionFeeId)
        REFERENCES TuitionFees(Id)
        ON DELETE RESTRICT
);
```

### Mục tiêu

Bảng này dùng để:

- theo dõi vòng đời giao dịch;
- chống ghi nhận thanh toán hai lần;
- hỗ trợ khôi phục trạng thái sau khi ứng dụng restart;
- lưu mã giao dịch của provider;
- phục vụ audit và troubleshooting.

## 6.2. Migration

Schema version:

```text
v4 → v5
```

### Yêu cầu

- [x] Migration chạy trong transaction.
- [x] Failure rollback về schema v4.
- [x] Retry migration thành công.
- [x] Không thay đổi dữ liệu tài chính cũ.
- [x] Regression test failure/retry.

---

# Phase 7 — UI thanh toán

## 7.1. Tái sử dụng FormQrPayment

Ưu tiên chuyển `FormQrPayment` thành form dùng model chung thay vì tạo thêm một form gần giống.

Form nhận:

```text
PaymentSession
IPaymentGateway
```

thay vì phụ thuộc trực tiếp vào `VietQrPaymentGateway`.

### UI dự kiến

```text
┌────────────────────────────────┐
│ Thanh toán MoMo                │
│                                │
│ Nguyễn Văn A                   │
│ 2121050001                     │
│                                │
│ Số tiền                        │
│ 3.000.000 ₫                    │
│                                │
│        [ QR MoMo ]             │
│                                │
│ Đang chờ thanh toán...         │
│                                │
│ Mã đơn: EDUFEE-...             │
│                                │
│             [Hủy]              │
└────────────────────────────────┘
```

---

# Phase 8 — Polling trạng thái

Giai đoạn đầu không triển khai IPN/Webhook.

WinForms sẽ kiểm tra trạng thái giao dịch định kỳ:

```text
Create Payment
     ↓
Show QR
     ↓
Query Status
     ↓
Pending
     ↓
Query lại
```

Khoảng thời gian đề xuất:

```text
3–5 giây
```

### Yêu cầu

- [x] Dùng `CancellationToken`.
- [x] Đóng form phải dừng polling.
- [x] Không tạo nhiều query loop cho cùng một session.
- [x] Timeout mạng không làm ứng dụng crash.
- [x] Không coi lỗi mạng là thanh toán thất bại ngay lập tức.

---

# Phase 9 — Xác nhận thanh toán

Đây là boundary quan trọng nhất của integration.

## Không hợp lệ

```text
Create Payment
      ↓
RecordPaymentWithReceipt
```

## Hợp lệ

```text
Create Payment
      ↓
Pending
      ↓
Query Status
      ↓
Success
      ↓
Validate Transaction
      ↓
RecordPaymentWithReceipt
```

Trước khi ghi nhận học phí cần kiểm tra:

- OrderId.
- Amount.
- Provider.
- TransactionId.
- Trạng thái.
- Giao dịch chưa được ghi nhận trước đó.

Sau đó mới gọi nghiệp vụ hiện có:

```csharp
TuitionService.RecordPaymentWithReceipt(...)
```

Transaction tài chính SQLite hiện tại tiếp tục là nguồn quyết định cuối cùng cho receipt và PaidAmount.

---

# Phase 10 — Idempotency và duplicate protection

Polling có thể nhận trạng thái thành công nhiều lần.

Ví dụ:

```text
Query 1 → Pending
Query 2 → Success
Query 3 → Success
```

Kết quả hợp lệ:

```text
1 payment
1 receipt
```

Không được tạo receipt thứ hai.

### Quy tắc

- `OrderId` unique.
- `ProviderTransactionId` được lưu khi có.
- Transaction đã Completed không được ghi nhận lại.
- Việc tạo receipt và đánh dấu gateway transaction hoàn tất cần được thiết kế để không tạo trạng thái nửa vời.

### Regression

- [x] Success lặp lại chỉ tạo một receipt.
- [x] App restart không làm ghi nhận lại transaction đã hoàn tất.
- [x] Amount mismatch không tạo receipt.
- [x] Transaction ID không hợp lệ không tạo receipt.

---

# Phase 11 — Settings UI

Bổ sung khu vực cấu hình payment provider.

```text
Thanh toán

Provider
(•) VietQR
( ) MoMo Sandbox

MoMo Sandbox
─────────────────────────
Partner Code
[....................]

Access Key
[....................]

Secret Key
[••••••••••••••••••••]

[Test cấu hình]

[Lưu]
```

### Yêu cầu

- [x] Secret field luôn masked.
- [x] Không tự động bật Sandbox khi cấu hình chưa hợp lệ.
- [x] Không lưu secret plaintext.
- [x] Có thông báo rõ khi thiếu cấu hình.

---

# Phase 12 — Demo Mode

`--demo` luôn sử dụng:

```text
VietQrPaymentGateway
```

Không gọi MoMo Sandbox.

Mục tiêu:

- deterministic;
- không yêu cầu Internet;
- không yêu cầu API key;
- không phụ thuộc availability của provider;
- không ảnh hưởng CI.

---

# Phase 13 — Regression Tests

## Gateway

- [x] Signature đúng.
- [x] OrderId generation.
- [x] RequestId uniqueness.
- [x] Create Payment response mapping.
- [x] Query response mapping.
- [x] Pending mapping.
- [x] Failure mapping.

## Nghiệp vụ

- [x] Success tạo receipt.
- [x] Pending không tạo receipt.
- [x] Failed không tạo receipt.
- [x] Cancelled không tạo receipt.
- [x] Amount mismatch không tạo receipt.
- [x] Duplicate Success chỉ tạo một receipt.
- [x] Receipt insert failure rollback đúng.
- [x] App restart không duplicate payment.
- [x] Demo Mode không gọi network.

## Migration

- [x] v4 → v5 thành công.
- [x] v4 → v5 failure rollback.
- [x] Retry migration thành công.
- [x] Financial data không thay đổi.

---

# Phase 14 — CI

GitHub Actions không gọi MoMo Sandbox thật.

Tests sử dụng mocked HTTP response hoặc custom `HttpMessageHandler`.

Ví dụ:

```json
{
  "resultCode": 0,
  "message": "Successful."
}
```

Pipeline tiếp tục chạy offline đối với payment provider:

```text
CI
 ↓
Mock HTTP response
 ↓
MomoSandboxPaymentGateway
 ↓
Signature / Parser / Mapping
 ↓
Regression Tests
```

Không đưa các thành phần sau vào GitHub Secrets nếu CI chưa thực sự cần integration test ngoài mạng:

- PartnerCode.
- AccessKey.
- SecretKey.

---

# Phase 15 — Error Handling

Các trường hợp cần xử lý:

- mất kết nối mạng;
- timeout;
- invalid signature;
- credential không hợp lệ;
- duplicate order;
- response không hợp lệ;
- provider unavailable;
- giao dịch pending quá lâu;
- giao dịch bị hủy;
- amount mismatch.

Thông báo lỗi không được làm thay đổi số dư học phí nếu trạng thái thanh toán chưa được xác nhận.

Ví dụ:

```text
Không thể kiểm tra trạng thái giao dịch.

Khoản thanh toán chưa được ghi nhận vào học phí.
Trạng thái giao dịch có thể được kiểm tra lại sau.
```

---

# Phase 16 — Logging

Có thể ghi:

- Provider.
- OrderId.
- RequestId.
- ProviderTransactionId.
- ResultCode.
- Payment status.
- Timestamp.

Không ghi:

- SecretKey.
- credential plaintext.
- DPAPI plaintext.
- dữ liệu nhạy cảm không cần thiết.

---

# Phase 17 — Documentation

Sau khi hoàn thành integration, cập nhật:

```text
README.md
README_VI.md
IMPROVEMENTS.md
KE_HOACH_PHAT_TRIEN.md
```

Tài liệu cần phân biệt rõ:

```text
MoMo Sandbox Integration
```

với:

```text
Production Payment Integration
```

Không mô tả Sandbox như hệ thống thanh toán Production.

---

# Thứ tự triển khai

```text
1. IPaymentGateway
        ↓
2. Chuẩn hóa VietQrPaymentGateway
        ↓
3. MomoSettings + DPAPI
        ↓
4. MomoSignatureService
        ↓
5. MomoSandboxPaymentGateway
        ↓
6. PaymentGatewayTransactions
        ↓
7. Migration v4 → v5
        ↓
8. Generic FormQrPayment
        ↓
9. Query / Polling
        ↓
10. Payment Success Validation
        ↓
11. RecordPaymentWithReceipt
        ↓
12. Duplicate Protection
        ↓
13. Settings UI
        ↓
14. Regression Tests
        ↓
15. Documentation
```

---

# Mức ưu tiên

## P0 — Bắt buộc

- [x] Payment gateway abstraction.
- [x] MoMo request signing.
- [x] Create Payment.
- [x] Query Payment.
- [x] Schema v5.
- [x] Transaction persistence.
- [x] Duplicate protection.
- [x] Chỉ tạo receipt sau Success.
- [x] Regression tests.

## P1 — Khuyến nghị

- [x] Settings UI.
- [x] DPAPI credential storage.
- [x] Generic QR payment form.
- [x] Polling cancellation.
- [x] Network/error handling.
- [x] Logging an toàn.
- [x] Cập nhật documentation.

## P2 — Sau khi integration cơ bản ổn định

- [ ] Manual transaction re-check.
- [ ] Payment history UI.
- [ ] Retry pending transaction sau restart.
- [ ] Provider diagnostics trong Settings.

---

# Ngoài phạm vi

Không triển khai trong giai đoạn này:

- MoMo Production.
- IPN/Webhook public endpoint.
- ASP.NET backend.
- Refund API.
- Recurring payment.
- Tokenized wallet.
- Real-money acceptance.
- Multi-provider payment orchestration.

---

# Definition of Done

Integration được coi là hoàn thành khi:

- [x] VietQR và MoMo Sandbox cùng triển khai `IPaymentGateway`.
- [x] MoMo request signing có regression test.
- [x] Create Payment hoạt động với cấu hình Sandbox hợp lệ.
- [x] Query Status hoạt động.
- [x] Pending/Failed/Cancelled không thay đổi học phí.
- [x] Success hợp lệ tạo đúng một receipt.
- [x] Duplicate Success không tạo receipt thứ hai.
- [x] Payment transaction được lưu trong SQLite.
- [x] Migration v4 → v5 có rollback/retry test.
- [x] Demo Mode không gọi MoMo.
- [x] CI không yêu cầu Internet hoặc MoMo credential.
- [x] SecretKey không xuất hiện trong repository hoặc log.
- [x] Build, regression suite và self-test đều pass.

---

## Kết quả mong đợi

EduFee có hai chế độ thanh toán rõ ràng:

```text
Payment Provider
├── VietQR
│   └── VietQrPaymentGateway
│
└── MoMo Sandbox
    └── MomoSandboxPaymentGateway
```

Cơ chế thanh toán bên ngoài chỉ chịu trách nhiệm xác nhận trạng thái giao dịch. Việc cập nhật công nợ và phát hành biên lai vẫn do domain tài chính hiện tại của EduFee kiểm soát, bảo đảm dữ liệu SQLite tiếp tục là nguồn dữ liệu tài chính chính thức của ứng dụng.
