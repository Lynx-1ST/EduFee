using K26_DotNet.Data;
using K26_DotNet.Models;

namespace K26_DotNet.Services;

/// <summary>
/// Persists external-payment intent before a provider call and commits a verified success with its receipt.
/// </summary>
public sealed class GatewayPaymentPersistenceService
{
    private readonly SqliteRepository _repository;
    private readonly TuitionService _tuitionService;
    private readonly ReceiptService _receiptService;

    public GatewayPaymentPersistenceService(SqlDatabaseContext database, TuitionService tuitionService,
        ReceiptService receiptService)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(tuitionService);
        ArgumentNullException.ThrowIfNull(receiptService);
        if (tuitionService.DatabasePath != database.DbPath || receiptService.DatabasePath != database.DbPath)
            throw new InvalidOperationException("Dịch vụ thanh toán, học phí và biên lai phải dùng cùng một cơ sở dữ liệu.");

        _repository = new SqliteRepository(database);
        _tuitionService = tuitionService;
        _receiptService = receiptService;
    }

    /// <summary>Call this before CreatePaymentAsync. A provider failure deliberately leaves the Pending intent recoverable.</summary>
    public PaymentGatewayTransaction PersistIntent(int tuitionFeeId, string provider, PaymentGatewayRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Amount <= 0 || request.Amount != decimal.Truncate(request.Amount))
            throw new ArgumentOutOfRangeException(nameof(request), "Số tiền thanh toán phải là số nguyên VND dương.");
        var fee = _tuitionService.GetById(tuitionFeeId)
            ?? throw new InvalidOperationException($"Không tìm thấy học phí ID {tuitionFeeId}.");
        if (request.Amount > fee.RemainingAmount)
            throw new InvalidOperationException("Số tiền giao dịch vượt quá số học phí còn lại.");

        return ToModel(_repository.CreateGatewayTransaction(provider, request.OrderId, tuitionFeeId,
            request.Amount, (int)GatewayPaymentStatus.Pending, DateTime.Now));
    }

    public PaymentGatewayTransaction? GetTransaction(string orderId)
    {
        var row = _repository.GetGatewayTransaction(orderId);
        return row == null ? null : ToModel(row);
    }

    /// <summary>Accepts only a verified provider success. Duplicate polling returns null and creates no second receipt.</summary>
    public PaymentReceipt? RecordConfirmedSuccess(PaymentGatewayStatus confirmation, string payerName,
        DateTime? dueDate = null)
    {
        ArgumentNullException.ThrowIfNull(confirmation);
        if (confirmation.Status != GatewayPaymentStatus.Success)
            throw new InvalidOperationException("Chỉ giao dịch đã xác nhận thành công mới được ghi nhận học phí.");
        if (string.IsNullOrWhiteSpace(confirmation.Provider))
            throw new ArgumentException("Thiếu nhà cung cấp trong kết quả thanh toán.", nameof(confirmation));

        return _tuitionService.RecordGatewayPaymentWithReceipt(confirmation.Provider, confirmation.OrderId,
            confirmation.ProviderTransactionId, confirmation.Amount, confirmation.ResultCode, payerName, _receiptService, dueDate);
    }

    /// <summary>Stores a non-success provider result for recovery/audit without changing the tuition ledger.</summary>
    public void RecordGatewayStatus(PaymentGatewayStatus confirmation)
    {
        ArgumentNullException.ThrowIfNull(confirmation);
        if (confirmation.Status == GatewayPaymentStatus.Success)
            throw new InvalidOperationException("Kết quả thành công phải được ghi nhận cùng biên lai.");
        if (string.IsNullOrWhiteSpace(confirmation.Provider))
            throw new ArgumentException("Thiếu nhà cung cấp trong kết quả thanh toán.", nameof(confirmation));
        bool isTerminal = confirmation.Status is GatewayPaymentStatus.Failed
            or GatewayPaymentStatus.Cancelled or GatewayPaymentStatus.Expired;
        _repository.UpdateGatewayTransactionStatus(confirmation.Provider, confirmation.OrderId,
            confirmation.ProviderTransactionId, confirmation.Amount, (int)confirmation.Status,
            confirmation.ResultCode, (int)GatewayPaymentStatus.Success, (int)GatewayPaymentStatus.Pending,
            (int)GatewayPaymentStatus.Unknown, isTerminal);
    }

    private static PaymentGatewayTransaction ToModel(GatewayTransactionRow row) => new(
        row.Id, row.Provider, row.OrderId, row.ProviderTransactionId, row.TuitionFeeId, row.ReceiptId, row.Amount,
        (GatewayPaymentStatus)row.Status, row.CreatedAt, row.CompletedAt, row.RawResultCode);
}

public sealed record PaymentGatewayTransaction(int Id, string Provider, string OrderId,
    string? ProviderTransactionId, int TuitionFeeId, int? ReceiptId, decimal Amount, GatewayPaymentStatus Status,
    DateTime CreatedAt, DateTime? CompletedAt, string? RawResultCode);
