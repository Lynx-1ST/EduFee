using K26_DotNet.Models;

namespace K26_DotNet.Services;

/// <summary>Rechecks durable pending/unknown transactions after an application restart.</summary>
public sealed class GatewayPaymentRecoveryService
{
    private readonly GatewayPaymentPersistenceService _persistence;

    public GatewayPaymentRecoveryService(GatewayPaymentPersistenceService persistence) =>
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));

    public async Task<GatewayRecoveryResult> RecoverAsync(IPaymentGateway gateway,
        Func<PaymentGatewayTransaction, GatewayRecoveryContext> resolveContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentNullException.ThrowIfNull(resolveContext);
        var transactions = _persistence.GetRecoverableTransactions(gateway.Provider);
        int checkedCount = 0, receiptsCreated = 0, statusesUpdated = 0;
        var errors = new List<string>();

        foreach (var transaction in transactions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var status = await gateway.QueryPaymentAsync(transaction.OrderId, cancellationToken)
                    .ConfigureAwait(true);
                checkedCount++;
                if (!string.Equals(status.Provider, transaction.Provider, StringComparison.Ordinal) ||
                    !string.Equals(status.OrderId, transaction.OrderId, StringComparison.Ordinal) ||
                    status.Amount != transaction.Amount)
                    throw new InvalidOperationException("Kết quả truy vấn không khớp giao dịch đã lưu.");

                if (status.Status == GatewayPaymentStatus.Success)
                {
                    var context = resolveContext(transaction);
                    PaymentReceipt? receipt = _persistence.RecordConfirmedSuccess(status,
                        context.PayerName, context.DueDate);
                    if (receipt != null) receiptsCreated++;
                }
                else
                {
                    _persistence.RecordGatewayStatus(status);
                    statusesUpdated++;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                errors.Add($"{transaction.OrderId}: {ex.Message}");
            }
        }

        return new GatewayRecoveryResult(checkedCount, receiptsCreated, statusesUpdated, errors);
    }
}

public sealed record GatewayRecoveryContext(string PayerName, DateTime? DueDate);

public sealed record GatewayRecoveryResult(int Checked, int ReceiptsCreated, int StatusesUpdated,
    IReadOnlyList<string> Errors);
