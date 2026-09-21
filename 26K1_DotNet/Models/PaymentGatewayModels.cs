namespace K26_DotNet.Services;

public enum GatewayPaymentStatus
{
    Pending,
    Success,
    Failed,
    Cancelled,
    Expired,
    Unknown
}

public sealed record PaymentGatewayRequest(
    string OrderId,
    decimal Amount,
    string Description,
    string StudentCode,
    string StudentName);

public sealed record PaymentSession(
    string OrderId,
    string RequestId,
    string Provider,
    string PaymentUrl,
    string QrCodeUrl,
    DateTime CreatedAt,
    GatewayPaymentStatus Status)
{
    public decimal Amount { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string QrPayload { get; init; } = string.Empty;
}

public sealed record PaymentGatewayStatus(
    string OrderId,
    string ProviderTransactionId,
    decimal Amount,
    GatewayPaymentStatus Status,
    string ResultCode,
    string Message)
{
    public string Provider { get; init; } = string.Empty;
}
