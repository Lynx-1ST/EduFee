using K26_DotNet.Services;

namespace K26_DotNet.RegressionTests;

public static class QrPaymentAcceptance
{
    public static void Run(Action<bool, string> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        var now = new DateTime(2026, 9, 20, 9, 0, 0);
        var gateway = new MockQrPaymentGateway(() => now);
        var request = new QrPaymentRequest(QrPaymentProvider.VietQr, 10, 20, 30, 750_000m, "Nộp học phí SV20");

        var first = gateway.CreateSession(request);
        var second = gateway.CreateSession(request with { Provider = QrPaymentProvider.MoMo });
        check(first.IsSimulation && first.ProviderName == "VietQR" && first.Amount == 750_000m &&
              first.Payload.Contains("vietqr", StringComparison.Ordinal) && first.Payload.Contains("amount=750000", StringComparison.Ordinal),
            "QR session has the expected provider, amount and simulation payload");
        check(first.TransactionId != second.TransactionId && second.ProviderName == "MoMo",
            "QR sessions use unique transaction IDs for each provider");
        check(Throws(() => gateway.CreateSession(request with { Amount = 1.5m })),
            "QR session rejects non-integral VND amounts");

        var confirmed = gateway.Confirm(first);
        var confirmedAgain = gateway.Confirm(first);
        check(confirmed.IsSuccessful && confirmed == confirmedAgain,
            "QR confirmation succeeds once and is idempotent");

        var expiring = gateway.CreateSession(request);
        now = now.AddMinutes(15);
        check(!gateway.Confirm(expiring).IsSuccessful,
            "QR sessions cannot be confirmed at the exact expiry boundary");

        using var form = new _26K1_DotNet.FormQrPayment(gateway, second);
        form.ShowInTaskbar = false;
        form.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
        form.Location = new System.Drawing.Point(-10000, -10000);
        form.Show();
        System.Windows.Forms.Application.DoEvents();
        using var bitmap = new System.Drawing.Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height));
        string? artifacts = Environment.GetEnvironmentVariable("EDUFEE_PDF_SAMPLE_DIR");
        if (!string.IsNullOrWhiteSpace(artifacts))
        {
            Directory.CreateDirectory(artifacts);
            bitmap.Save(Path.Combine(artifacts, "qr-payment-momo.png"));
        }
        check(bitmap.Width > 400 && bitmap.Height > 500,
            "QR payment dialog renders a scannable simulated payment session");
    }

    private static bool Throws(Action action)
    {
        try { action(); return false; }
        catch (ArgumentException) { return true; }
    }
}
