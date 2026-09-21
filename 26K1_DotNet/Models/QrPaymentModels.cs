namespace K26_DotNet.Services;

/// <summary>Nhà cung cấp mã thanh toán QR trong hệ thống.</summary>
public enum QrPaymentProvider
{
    VietQr
}

/// <summary>Dữ liệu cần có để tạo một yêu cầu thanh toán QR.</summary>
public sealed record QrPaymentRequest(
    QrPaymentProvider Provider,
    int TuitionFeeId,
    int StudentId,
    int SemesterId,
    decimal Amount,
    string Description);

/// <summary>Phiên VietQR ngắn hạn; việc xác nhận chuyển khoản được thực hiện thủ công.</summary>
public sealed record QrPaymentSession(
    string TransactionId,
    QrPaymentProvider Provider,
    string ProviderName,
    decimal Amount,
    string Description,
    string Payload,
    DateTime ExpiresAt,
    bool RequiresManualConfirmation);

/// <summary>Kết quả xác nhận thủ công của cổng VietQR.</summary>
public sealed record QrPaymentConfirmation(
    string TransactionId,
    bool IsSuccessful,
    string Message,
    DateTime ConfirmedAt);

public interface IQrPaymentGateway
{
    QrPaymentSession CreateSession(QrPaymentRequest request);
    QrPaymentConfirmation Confirm(QrPaymentSession session);
}
