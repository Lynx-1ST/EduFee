namespace K26_DotNet.Services;

/// <summary>Nhà cung cấp mã QR được mô phỏng trong bản đồ án.</summary>
public enum QrPaymentProvider
{
    VietQr,
    MoMo
}

/// <summary>Dữ liệu cần có để tạo một yêu cầu thanh toán QR.</summary>
public sealed record QrPaymentRequest(
    QrPaymentProvider Provider,
    int TuitionFeeId,
    int StudentId,
    int SemesterId,
    decimal Amount,
    string Description);

/// <summary>Phiên QR ngắn hạn; chỉ dùng cho mô phỏng, không phải xác nhận ngân hàng thật.</summary>
public sealed record QrPaymentSession(
    string TransactionId,
    QrPaymentProvider Provider,
    string ProviderName,
    decimal Amount,
    string Description,
    string Payload,
    DateTime ExpiresAt,
    bool IsSimulation);

/// <summary>Kết quả xác nhận của cổng QR mô phỏng.</summary>
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
