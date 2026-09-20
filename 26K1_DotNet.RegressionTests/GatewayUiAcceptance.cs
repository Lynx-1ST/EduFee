using System.Diagnostics;
using K26_DotNet.Data;
using K26_DotNet.Models;
using K26_DotNet.Services;
using _26K1_DotNet;

namespace K26_DotNet.RegressionTests;

internal static class GatewayUiAcceptance
{
    public static void Run(string root, Action<bool, string> check)
    {
        var dbPath = Path.Combine(root, "gateway-ui-test.db");
        var db = new SqlDatabaseContext(dbPath);
        var stuSvc = new StudentService(db);
        var semSvc = new SemesterService(db);
        var tuiSvc = new TuitionService(db);
        var recSvc = new ReceiptService(db);
        var fee = new TuitionFee { Id = 1, StudentId = 1, SemesterId = 1, TotalAmount = 2_000_000m, PaidAmount = 0m };
        using (var paymentForm = new FormPayment(fee, tuiSvc, semSvc, stuSvc, recSvc))
        {
            var cmbGateway = Descendants(paymentForm).OfType<ComboBox>().Single(c => c.AccessibleName == "Cổng thanh toán QR");
            check(cmbGateway.Items.Contains("VietQR") && cmbGateway.Items.Contains("MoMo Sandbox"),
                "FormPayment always includes VietQR and MoMo Sandbox in the gateway dropdown");

            var cmbMethod = Descendants(paymentForm).OfType<ComboBox>().Single(c => c.AccessibleName == "Hình thức thanh toán");
            check(cmbGateway.Enabled, "QR gateway dropdown is enabled when QR method is selected");
            cmbMethod.SelectedIndex = 1; // "Tiền mặt"
            check(!cmbGateway.Enabled, "QR gateway dropdown is disabled when non-QR method is selected");
        }
        var settings = new MomoSettingsService(Path.Combine(root, "ui-momo-settings.json"));
        settings.Settings.Enabled = true;
        settings.Settings.PartnerCode = "test-partner";
        settings.Settings.AccessKey = "test-access";
        settings.Settings.SecretKey = "stored-test-secret";
        using (var settingsForm = new FormMomoSettings(settings))
        {
            var secret = Descendants(settingsForm).OfType<TextBox>().Single(text => text.AccessibleName == "Secret Key");
            check(secret.UseSystemPasswordChar && secret.Text == string.Empty,
                "MoMo settings masks secret input and never populates the saved secret into the UI");
            settingsForm.ShowInTaskbar = false;
            settingsForm.StartPosition = FormStartPosition.Manual;
            settingsForm.Location = new Point(-10000, -10000);
            settingsForm.Show();
            Application.DoEvents();
            foreach (var label in Descendants(settingsForm).OfType<Label>().Where(label => label.Visible))
                check(label.Parent!.ClientRectangle.Contains(label.Bounds), "MoMo settings label fits its parent: " + label.Text);
            string? samples = Environment.GetEnvironmentVariable("EDUFEE_PDF_SAMPLE_DIR");
            if (!string.IsNullOrWhiteSpace(samples))
            {
                Directory.CreateDirectory(samples);
                using var bitmap = new Bitmap(settingsForm.Width, settingsForm.Height);
                settingsForm.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                bitmap.Save(Path.Combine(samples, "momo-settings.png"));
            }
            settingsForm.Close();
        }

        var session = new PaymentSession("EDUFEE-UI-TEST", "request-ui", "MoMoSandbox",
            "https://test-payment.momo.vn/test", "", DateTime.UtcNow, GatewayPaymentStatus.Pending)
        { Amount = 10_000m };

        var blocked = new TestGateway(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            throw new InvalidOperationException("Unreachable");
        });
        using (var form = Open(blocked, session))
        {
            PumpUntil(() => blocked.Calls == 1);
            form.Close();
            PumpUntil(() => blocked.Cancelled);
            check(blocked.Calls == 1 && form.PaymentStatus is null,
                "Closing the gateway dialog cancels its in-flight query without confirming payment");
        }

        var retry = new TestGateway(async (call, token) =>
        {
            await Task.Delay(30, token);
            if (call == 1) throw new HttpRequestException("Offline test");
            return Status(session, call < 4 ? GatewayPaymentStatus.Pending : GatewayPaymentStatus.Success);
        });
        using (var form = Open(retry, session))
        {
            check(!Descendants(form).OfType<Button>().Any(button => button.Visible && button.Text.Contains("Mô phỏng")),
                "The MoMo dialog has no manual simulation confirmation button");
            PumpUntil(() => form.PaymentStatus?.Status == GatewayPaymentStatus.Success);
            check(form.DialogResult == DialogResult.OK && retry.Calls == 4 && retry.MaxActive == 1,
                "Network errors remain retryable and polling stays sequential until provider success");
        }

        var failed = new TestGateway((_, _) => Task.FromResult(Status(session, GatewayPaymentStatus.Failed)));
        using (var form = Open(failed, session))
        {
            PumpUntil(() => form.PaymentStatus is not null);
            check(form.PaymentStatus!.Status == GatewayPaymentStatus.Failed && form.DialogResult != DialogResult.OK,
                "A failed gateway response never returns a successful payment dialog result");
            string? artifacts = Environment.GetEnvironmentVariable("EDUFEE_PDF_SAMPLE_DIR");
            if (!string.IsNullOrWhiteSpace(artifacts))
            {
                Directory.CreateDirectory(artifacts);
                using var bitmap = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                bitmap.Save(Path.Combine(artifacts, "momo-payment.png"));
            }
            form.Close();
        }
    }

    private static PaymentGatewayStatus Status(PaymentSession session, GatewayPaymentStatus status) =>
        new(session.OrderId, status == GatewayPaymentStatus.Success ? "123456" : "", session.Amount,
            status, status == GatewayPaymentStatus.Success ? "0" : "1000", "") { Provider = session.Provider };

    private static FormQrPayment Open(IPaymentGateway gateway, PaymentSession session)
    {
        var form = new FormQrPayment(gateway, session, TimeSpan.FromMilliseconds(15))
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-10000, -10000)
        };
        form.Show();
        Application.DoEvents();
        return form;
    }

    private static void PumpUntil(Func<bool> condition)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition() && elapsed.Elapsed < TimeSpan.FromSeconds(5))
        {
            Application.DoEvents();
            Thread.Sleep(5);
        }
        if (!condition()) throw new TimeoutException("Gateway UI did not reach the expected state.");
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    private sealed class TestGateway(Func<int, CancellationToken, Task<PaymentGatewayStatus>> query) : IPaymentGateway
    {
        public string Provider => "MoMoSandbox";
        public int Calls { get; private set; }
        public int MaxActive { get; private set; }
        public bool Cancelled { get; private set; }
        private int _active;
        public Task<PaymentSession> CreatePaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("UI tests only query an existing session.");
        public async Task<PaymentGatewayStatus> QueryPaymentAsync(string orderId, CancellationToken cancellationToken = default)
        {
            Calls++;
            MaxActive = Math.Max(MaxActive, ++_active);
            try { return await query(Calls, cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            { Cancelled = true; throw; }
            finally { _active--; }
        }
    }
}
