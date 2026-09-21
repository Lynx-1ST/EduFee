using System.Globalization;

namespace K26_DotNet.Services;

/// <summary>
/// Cổng VietQR tạo yêu cầu chuyển khoản và chờ người dùng xác nhận đã chuyển tiền.
/// VietQR không cung cấp trạng thái giao dịch cho ứng dụng desktop này, vì vậy xác nhận là thủ công.
/// </summary>
public sealed class VietQrPaymentGateway : IQrPaymentGateway, IPaymentGateway
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(15);
    private readonly Func<DateTime> _clock;
    private readonly object _sync = new();
    private readonly Dictionary<string, QrPaymentSession> _sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, QrPaymentConfirmation> _confirmations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PaymentSession> _paymentSessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GatewayPaymentStatus> _paymentStatuses = new(StringComparer.Ordinal);

    public const string ProviderName = "VietQR";
    public string Provider => ProviderName;

    public VietQrPaymentGateway(Func<DateTime>? clock = null)
    {
        _clock = clock ?? (() => DateTime.Now);
    }

    public QrPaymentSession CreateSession(QrPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var now = _clock();
        var transactionId = "VQR-" + Guid.NewGuid().ToString("N").ToUpperInvariant();
        const string providerName = "VietQR";
        var description = request.Description.Trim();
        var payload = BuildPayload(transactionId, request, description);
        var session = new QrPaymentSession(
            transactionId,
            request.Provider,
            providerName,
            request.Amount,
            description,
            payload,
            now.Add(SessionLifetime),
            RequiresManualConfirmation: true);

        lock (_sync)
        {
            _sessions.Add(transactionId, session);
        }

        return session;
    }

    public QrPaymentConfirmation Confirm(QrPaymentSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var now = _clock();

        lock (_sync)
        {
            if (!_sessions.TryGetValue(session.TransactionId, out var stored) || stored != session)
                return Failed(session.TransactionId, "Phiên QR không hợp lệ.", now);

            if (_confirmations.TryGetValue(session.TransactionId, out var prior))
                return prior;

            if (now >= stored.ExpiresAt)
                return Failed(session.TransactionId, "Mã QR đã hết hạn. Hãy tạo mã mới.", now);

            var confirmation = new QrPaymentConfirmation(
                stored.TransactionId,
                IsSuccessful: true,
                "Đã xác nhận thanh toán. Hãy lập biên lai để ghi nhận học phí.",
                now);
            _confirmations.Add(stored.TransactionId, confirmation);
            return confirmation;
        }
    }

    public Task<PaymentSession> CreatePaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateGatewayRequest(request);

        var now = _clock();
        var requestId = "VQR-" + Guid.NewGuid().ToString("N").ToUpperInvariant();
        var session = new PaymentSession(
            request.OrderId.Trim(), requestId, Provider, string.Empty, string.Empty, now, GatewayPaymentStatus.Pending)
        {
            Amount = request.Amount,
            ExpiresAt = now.Add(SessionLifetime),
            QrPayload = string.Create(CultureInfo.InvariantCulture,
                $"vietqr://pay?orderId={Uri.EscapeDataString(request.OrderId.Trim())}&requestId={requestId}&amount={request.Amount:0}")
        };
        lock (_sync)
        {
            if (!_paymentSessions.TryAdd(session.OrderId, session))
                throw new InvalidOperationException("Mã đơn thanh toán VietQR đã tồn tại.");
            _paymentStatuses[session.OrderId] = GatewayPaymentStatus.Pending;
        }
        return Task.FromResult(session);
    }

    public Task<PaymentGatewayStatus> QueryPaymentAsync(string orderId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(orderId)) throw new ArgumentException("Mã đơn thanh toán không được để trống.", nameof(orderId));
        lock (_sync)
        {
            if (!_paymentSessions.TryGetValue(orderId.Trim(), out var session))
                return Task.FromResult(new PaymentGatewayStatus(orderId.Trim(), string.Empty, 0, GatewayPaymentStatus.Unknown, "NOT_FOUND", "Không tìm thấy giao dịch VietQR.") { Provider = Provider });
            var status = _paymentStatuses[session.OrderId];
            if (status == GatewayPaymentStatus.Pending && session.ExpiresAt <= _clock()) status = GatewayPaymentStatus.Expired;
            return Task.FromResult(new PaymentGatewayStatus(session.OrderId,
                status == GatewayPaymentStatus.Success ? session.RequestId : string.Empty,
                session.Amount, status, ToResultCode(status), ToMessage(status)) { Provider = Provider });
        }
    }

    public void ConfirmTransferred(string orderId, GatewayPaymentStatus status = GatewayPaymentStatus.Success)
    {
        if (string.IsNullOrWhiteSpace(orderId)) throw new ArgumentException("Mã đơn thanh toán không được để trống.", nameof(orderId));
        if (status is GatewayPaymentStatus.Unknown) throw new ArgumentOutOfRangeException(nameof(status));
        lock (_sync)
        {
            if (!_paymentSessions.ContainsKey(orderId.Trim())) throw new InvalidOperationException("Không tìm thấy giao dịch VietQR.");
            _paymentStatuses[orderId.Trim()] = status;
        }
    }

    private static void Validate(QrPaymentRequest request)
    {
        if (!Enum.IsDefined(request.Provider)) throw new ArgumentOutOfRangeException(nameof(request.Provider));
        if (request.TuitionFeeId <= 0) throw new ArgumentOutOfRangeException(nameof(request.TuitionFeeId));
        if (request.StudentId <= 0) throw new ArgumentOutOfRangeException(nameof(request.StudentId));
        if (request.SemesterId <= 0) throw new ArgumentOutOfRangeException(nameof(request.SemesterId));
        if (request.Amount <= 0 || decimal.Truncate(request.Amount) != request.Amount)
            throw new ArgumentOutOfRangeException(nameof(request.Amount), "Số tiền phải là VND nguyên dương.");
        if (string.IsNullOrWhiteSpace(request.Description))
            throw new ArgumentException("Nội dung thanh toán không được để trống.", nameof(request.Description));
    }

    private static void ValidateGatewayRequest(PaymentGatewayRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId)) throw new ArgumentException("Mã đơn thanh toán không được để trống.", nameof(request));
        if (request.Amount <= 0 || decimal.Truncate(request.Amount) != request.Amount) throw new ArgumentOutOfRangeException(nameof(request), "Số tiền phải là VND nguyên dương.");
        if (string.IsNullOrWhiteSpace(request.Description)) throw new ArgumentException("Nội dung thanh toán không được để trống.", nameof(request));
    }

    private static string ToResultCode(GatewayPaymentStatus status) => status switch
    {
        GatewayPaymentStatus.Success => "0",
        GatewayPaymentStatus.Pending => "PENDING",
        GatewayPaymentStatus.Cancelled => "CANCELLED",
        GatewayPaymentStatus.Expired => "EXPIRED",
        _ => "FAILED"
    };

    private static string ToMessage(GatewayPaymentStatus status) => status switch
    {
        GatewayPaymentStatus.Success => "Đã xác nhận chuyển khoản VietQR.",
        GatewayPaymentStatus.Pending => "Đang chờ xác nhận chuyển khoản VietQR.",
        GatewayPaymentStatus.Cancelled => "Thanh toán VietQR đã bị hủy.",
        GatewayPaymentStatus.Expired => "Mã VietQR đã hết hạn.",
        _ => "Thanh toán VietQR không thành công."
    };

    private static string BuildPayload(string transactionId, QrPaymentRequest request, string description)
    {
        return string.Create(CultureInfo.InvariantCulture,
            $"vietqr://pay?transactionId={transactionId}&feeId={request.TuitionFeeId}&studentId={request.StudentId}&semesterId={request.SemesterId}&amount={request.Amount:0}&description={Uri.EscapeDataString(description)}");
    }

    private static QrPaymentConfirmation Failed(string transactionId, string message, DateTime now) =>
        new(transactionId, IsSuccessful: false, message, now);
}
