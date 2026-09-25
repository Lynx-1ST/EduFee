using K26_DotNet.Data;
using K26_DotNet.Services;

namespace K26_DotNet.RegressionTests;

internal static class GatewayPersistenceAcceptance
{
    public static void Run(string root, Action<bool, string> check)
    {
        string databasePath = Path.Combine(root, "gateway-status.db");
        var database = new SqlDatabaseContext(databasePath);
        SeedFee(database);
        var tuition = new TuitionService(database);
        var receipts = new ReceiptService(database);
        var persistence = new GatewayPaymentPersistenceService(database, tuition, receipts);

        var terminalStatuses = new[]
        {
            GatewayPaymentStatus.Failed,
            GatewayPaymentStatus.Expired,
            GatewayPaymentStatus.Cancelled
        };

        foreach (var status in terminalStatuses)
        {
            string orderId = "STATUS-" + status.ToString().ToUpperInvariant();
            persistence.PersistIntent(1, MomoSandboxPaymentGateway.ProviderName,
                new PaymentGatewayRequest(orderId, 100_000m, "Kiểm thử trạng thái", "SV1", "Sinh viên 1"));
            var result = new PaymentGatewayStatus(orderId, "", 100_000m, status,
                ((int)status).ToString(), status.ToString())
            { Provider = MomoSandboxPaymentGateway.ProviderName };

            persistence.RecordGatewayStatus(result);
            persistence.RecordGatewayStatus(result); // Polling/provider retries must be idempotent.

            var stored = persistence.GetTransaction(orderId)!;
            check(stored.Status == status && stored.RawResultCode == ((int)status).ToString() &&
                  stored.CompletedAt.HasValue && stored.ReceiptId is null,
                $"Gateway {status} is persisted idempotently as a terminal transaction");

            var differentTerminal = status == GatewayPaymentStatus.Failed
                ? GatewayPaymentStatus.Cancelled : GatewayPaymentStatus.Failed;
            check(Throws(() => persistence.RecordGatewayStatus(result with { Status = differentTerminal })),
                $"Gateway {status} cannot be rewritten as another terminal state");
        }

        var pendingRequest = new PaymentGatewayRequest("STATUS-PENDING", 100_000m,
            "Kiểm thử pending", "SV1", "Sinh viên 1");
        persistence.PersistIntent(1, MomoSandboxPaymentGateway.ProviderName, pendingRequest);
        persistence.RecordGatewayStatus(new PaymentGatewayStatus(pendingRequest.OrderId, "", pendingRequest.Amount,
            GatewayPaymentStatus.Unknown, "9999", "Unknown") { Provider = MomoSandboxPaymentGateway.ProviderName });
        var pending = persistence.GetTransaction(pendingRequest.OrderId)!;
        check(pending.Status == GatewayPaymentStatus.Unknown && pending.RawResultCode == "9999" &&
              pending.CompletedAt is null && pending.ReceiptId is null,
            "Unknown gateway status remains recoverable and stores its raw result code");
        check(Throws(() => persistence.RecordGatewayStatus(new PaymentGatewayStatus(
                pendingRequest.OrderId, "", pendingRequest.Amount + 1, GatewayPaymentStatus.Failed,
                "1001", "Mismatch") { Provider = MomoSandboxPaymentGateway.ProviderName })),
            "Gateway status update rejects a provider amount mismatch");

        var fee = tuition.GetById(1)!;
        check(fee.PaidAmount == 0m && receipts.GetAll().Count == 0,
            "Non-success gateway statuses never change tuition or create receipts");

        VerifyRestartRecovery(root, check);
    }

    private static void VerifyRestartRecovery(string root, Action<bool, string> check)
    {
        string path = Path.Combine(root, "gateway-restart.db");
        var firstDatabase = new SqlDatabaseContext(path);
        SeedFee(firstDatabase);
        var firstPersistence = new GatewayPaymentPersistenceService(firstDatabase,
            new TuitionService(firstDatabase), new ReceiptService(firstDatabase));
        foreach (string orderId in new[] { "RECOVER-SUCCESS", "RECOVER-FAILED", "RECOVER-OFFLINE" })
            firstPersistence.PersistIntent(1, MomoSandboxPaymentGateway.ProviderName,
                new PaymentGatewayRequest(orderId, 100_000m, "Khôi phục", "SV1", "Sinh viên 1"));

        // Recreate every service to model a real application restart.
        var restartedDatabase = new SqlDatabaseContext(path);
        var restartedTuition = new TuitionService(restartedDatabase);
        var restartedReceipts = new ReceiptService(restartedDatabase);
        var restartedPersistence = new GatewayPaymentPersistenceService(restartedDatabase,
            restartedTuition, restartedReceipts);
        var gateway = new RecoveryGateway(new Dictionary<string, PaymentGatewayStatus>
        {
            ["RECOVER-SUCCESS"] = Result("RECOVER-SUCCESS", GatewayPaymentStatus.Success, "0", "987654"),
            ["RECOVER-FAILED"] = Result("RECOVER-FAILED", GatewayPaymentStatus.Failed, "1001", "")
        });
        var recovery = new GatewayPaymentRecoveryService(restartedPersistence);
        var result = recovery.RecoverAsync(gateway,
            _ => new GatewayRecoveryContext("Sinh viên 1", new DateTime(2026, 5, 1)))
            .GetAwaiter().GetResult();

        check(result.Checked == 2 && result.ReceiptsCreated == 1 && result.StatusesUpdated == 1 &&
              result.Errors.Count == 1,
            "Restart recovery confirms success, persists failure and isolates network errors");
        check(restartedPersistence.GetTransaction("RECOVER-SUCCESS")!.Status == GatewayPaymentStatus.Success &&
              restartedPersistence.GetTransaction("RECOVER-FAILED")!.Status == GatewayPaymentStatus.Failed &&
              restartedPersistence.GetTransaction("RECOVER-OFFLINE")!.Status == GatewayPaymentStatus.Pending &&
              restartedTuition.GetById(1)!.PaidAmount == 100_000m && restartedReceipts.GetAll().Count == 1,
            "Restart recovery commits exactly one receipt and leaves unreachable transactions recoverable");

        var second = recovery.RecoverAsync(gateway,
            _ => new GatewayRecoveryContext("Sinh viên 1", null)).GetAwaiter().GetResult();
        check(second.ReceiptsCreated == 0 && restartedReceipts.GetAll().Count == 1 &&
              gateway.QueriesByOrder.GetValueOrDefault("RECOVER-SUCCESS") == 1,
            "Repeated recovery never requeries completed transactions or duplicates receipts");
    }

    private static PaymentGatewayStatus Result(string orderId, GatewayPaymentStatus status,
        string resultCode, string transactionId) =>
        new(orderId, transactionId, 100_000m, status, resultCode, status.ToString())
        { Provider = MomoSandboxPaymentGateway.ProviderName };

    private static void SeedFee(SqlDatabaseContext database)
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Students(Id, StudentCode, FullName, Email, PhoneNumber, DateOfBirth, ClassName)
            VALUES(1, 'SV1', 'Sinh viên 1', '', '', '2000-01-01T00:00:00.0000000', 'K1');
            INSERT INTO Semesters(Id, Name, StartDate, EndDate, DueDate, IsActive)
            VALUES(1, 'HK kiểm thử', '2026-01-01T00:00:00.0000000', '2026-06-01T00:00:00.0000000', '2026-05-01T00:00:00.0000000', 1);
            INSERT INTO TuitionFees(Id, StudentId, SemesterId, Credits, TotalAmount, DiscountAmount,
                DiscountReason, PaidAmount, PaidDate, DueDate, Status, Note)
            VALUES(1, 1, 1, 1, 1000000, 0, '', 0, NULL, NULL, 0, '');
            """;
        command.ExecuteNonQuery();
    }

    private static bool Throws(Action action)
    {
        try { action(); return false; }
        catch (InvalidOperationException) { return true; }
    }

    private sealed class RecoveryGateway(IReadOnlyDictionary<string, PaymentGatewayStatus> results) : IPaymentGateway
    {
        public string Provider => MomoSandboxPaymentGateway.ProviderName;
        public Dictionary<string, int> QueriesByOrder { get; } = new(StringComparer.Ordinal);

        public Task<PaymentSession> CreatePaymentAsync(PaymentGatewayRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<PaymentGatewayStatus> QueryPaymentAsync(string orderId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            QueriesByOrder[orderId] = QueriesByOrder.GetValueOrDefault(orderId) + 1;
            if (!results.TryGetValue(orderId, out var result))
                throw new HttpRequestException("Offline test");
            return Task.FromResult(result);
        }
    }
}
