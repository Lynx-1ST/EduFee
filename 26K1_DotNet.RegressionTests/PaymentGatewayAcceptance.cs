using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using K26_DotNet.Models;
using K26_DotNet.Services;

namespace K26_DotNet.RegressionTests;

public static class PaymentGatewayAcceptance
{
    public static void Run(string root, Action<bool, string> check)
    {
        var signature = new MomoSignatureService();
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("vi-VN");
            var raw = signature.CreatePaymentRawSignature("access", 1_000m, "", "https://localhost/", "ORDER-1", "Học phí", "partner", "https://localhost/", "REQ-1", "captureWallet");
            check(raw == "accessKey=access&amount=1000&extraData=&ipnUrl=https://localhost/&orderId=ORDER-1&orderInfo=Học phí&partnerCode=partner&redirectUrl=https://localhost/&requestId=REQ-1&requestType=captureWallet" &&
                  signature.Sign("what do ya want for nothing?", "Jefe") == "5bdcc146bf60754e6a042426089575c75a003f089d2739839dec58b964ec3843",
                "MoMo signatures use deterministic invariant UTF-8 HMAC-SHA256 input");
        }
        finally { CultureInfo.CurrentCulture = originalCulture; }

        var settingsPath = Path.Combine(root, "momo-settings.json");
        var settingsService = new MomoSettingsService(settingsPath);
        settingsService.Settings.Enabled = true;
        settingsService.Settings.PartnerCode = "partner";
        settingsService.Settings.AccessKey = "access";
        settingsService.Settings.SecretKey = "secret";
        settingsService.SaveSettings();
        check(!File.ReadAllText(settingsPath).Contains("secret", StringComparison.Ordinal) && new MomoSettingsService(settingsPath).Settings.SecretKey == "secret",
            "MoMo secret key is DPAPI encrypted on disk");
        check(Throws(() => MomoSettingsService.Validate(new MomoSettings { UseSandbox = false }, allowDisabledIncomplete: true)),
            "MoMo production settings are rejected even when disabled");

        var handler = new StubHandler("""{"partnerCode":"partner","requestId":"REQUEST","orderId":"ORDER-1","amount":1000,"resultCode":0,"transId":123456}""");
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(1) };
        var gateway = new MomoSandboxPaymentGateway(settingsService, client);
        var status = gateway.QueryPaymentAsync("ORDER-1").GetAwaiter().GetResult();
        check(status.Status == GatewayPaymentStatus.Success && status.ProviderTransactionId == "123456" && status.Provider == MomoSandboxPaymentGateway.ProviderName,
            "MoMo query maps a validated success response");

        using var pendingClient = new HttpClient(new StubHandler("""{"partnerCode":"partner","requestId":"REQUEST","orderId":"ORDER-2","amount":1000,"resultCode":7000,"transId":0}""")) { Timeout = TimeSpan.FromSeconds(1) };
        var pending = new MomoSandboxPaymentGateway(settingsService, pendingClient).QueryPaymentAsync("ORDER-2").GetAwaiter().GetResult();
        check(pending.Status == GatewayPaymentStatus.Pending,
            "MoMo processing result codes remain pending for polling");

        var mock = new MockQrPaymentGateway();
        var session = mock.CreatePaymentAsync(new PaymentGatewayRequest("MOCK-1", 1_000m, "Học phí", "SV1", "Sinh viên 1")).GetAwaiter().GetResult();
        mock.SimulatePayment(session.OrderId);
        check(mock.QueryPaymentAsync(session.OrderId).GetAwaiter().GetResult().Status == GatewayPaymentStatus.Success,
            "Mock gateway exposes an explicit simulated payment status");
    }

    private static bool Throws(Action action)
    {
        try { action(); return false; }
        catch (ArgumentException) { return true; }
    }

    private sealed class StubHandler(string response) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestJson = await request.Content!.ReadAsStringAsync(cancellationToken);
            var requestId = System.Text.Json.JsonDocument.Parse(requestJson).RootElement.GetProperty("requestId").GetString();
            var reply = response.Replace("REQUEST", requestId, StringComparison.Ordinal);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(reply, Encoding.UTF8, "application/json") };
        }
    }
}
