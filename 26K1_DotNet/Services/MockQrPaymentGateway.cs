using System.Globalization;

namespace K26_DotNet.Services;

/// <summary>
/// Cổng QR tại chỗ phục vụ trình diễn. Nó không gọi MoMo, VietQR hoặc ngân hàng nào.
/// Việc thu tiền thật vẫn phải đi qua nghiệp vụ lập biên lai của ứng dụng.
/// </summary>
public sealed class MockQrPaymentGateway : IQrPaymentGateway
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(15);
    private readonly Func<DateTime> _clock;
    private readonly object _sync = new();
    private readonly Dictionary<string, QrPaymentSession> _sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, QrPaymentConfirmation> _confirmations = new(StringComparer.Ordinal);

    public MockQrPaymentGateway(Func<DateTime>? clock = null)
    {
        _clock = clock ?? (() => DateTime.Now);
    }

    public QrPaymentSession CreateSession(QrPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var now = _clock();
        var transactionId = "SIM-" + Guid.NewGuid().ToString("N").ToUpperInvariant();
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
            IsSimulation: true);

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

    private static string BuildPayload(string transactionId, QrPaymentRequest request, string description)
    {
        const string provider = "vietqr";
        return string.Create(CultureInfo.InvariantCulture,
            $"edufee-sim://{provider}/pay?transactionId={transactionId}&feeId={request.TuitionFeeId}&studentId={request.StudentId}&semesterId={request.SemesterId}&amount={request.Amount:0}&description={Uri.EscapeDataString(description)}");
    }

    private static QrPaymentConfirmation Failed(string transactionId, string message, DateTime now) =>
        new(transactionId, IsSuccessful: false, message, now);
}
