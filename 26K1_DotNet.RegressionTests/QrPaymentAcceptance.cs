using K26_DotNet.Services;

namespace K26_DotNet.RegressionTests;

public static class QrPaymentAcceptance
{
    public static void Run(Action<bool, string> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        var now = new DateTime(2026, 9, 20, 9, 0, 0);
        var gateway = new VietQrPaymentGateway(() => now);
        var request = new QrPaymentRequest(QrPaymentProvider.VietQr, 10, 20, 30, 750_000m, "Nộp học phí SV20");

        var first = gateway.CreateSession(request);
        var second = gateway.CreateSession(request);
        check(first.RequiresManualConfirmation && first.ProviderName == "VietQR" && first.Amount == 750_000m &&
              first.Payload.Contains("vietqr", StringComparison.Ordinal) && first.Payload.Contains("amount=750000", StringComparison.Ordinal),
            "VietQR session has the expected provider, amount and payment payload");
        check(first.TransactionId != second.TransactionId && second.ProviderName == "VietQR",
            "QR sessions use unique transaction IDs");
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

        var generic = gateway.CreatePaymentAsync(new PaymentGatewayRequest(
            "EDUFEE-QR-RENDER", 750_000m, "Nộp học phí SV20", "SV20", "Sinh viên kiểm thử")).GetAwaiter().GetResult();
        using var form = new _26K1_DotNet.FormQrPayment(gateway, generic);
        form.ShowInTaskbar = false;
        form.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
        form.Location = new System.Drawing.Point(-10000, -10000);
        form.Show();
        System.Windows.Forms.Application.DoEvents();
        var visibleButtons = Descendants(form).OfType<System.Windows.Forms.Button>()
            .Where(button => button.Visible).Select(button => button.Text).ToList();
        check(visibleButtons.Contains("Tôi đã chuyển khoản") &&
              visibleButtons.All(text => !text.Contains("Mô phỏng", StringComparison.OrdinalIgnoreCase)),
            "VietQR uses manual transfer confirmation without mock terminology");
        using var bitmap = new System.Drawing.Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height));
        string? artifacts = Environment.GetEnvironmentVariable("EDUFEE_PDF_SAMPLE_DIR");
        if (!string.IsNullOrWhiteSpace(artifacts))
        {
            Directory.CreateDirectory(artifacts);
            bitmap.Save(Path.Combine(artifacts, "qr-payment-vietqr.png"));
        }
        check(bitmap.Width > 400 && bitmap.Height > 500,
            "VietQR payment dialog renders a scannable transfer session");
    }

    private static bool Throws(Action action)
    {
        try { action(); return false; }
        catch (ArgumentException) { return true; }
    }

    private static IEnumerable<System.Windows.Forms.Control> Descendants(System.Windows.Forms.Control parent)
    {
        foreach (System.Windows.Forms.Control child in parent.Controls)
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
}
