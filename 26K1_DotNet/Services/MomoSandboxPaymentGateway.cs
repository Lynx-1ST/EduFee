using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using K26_DotNet.Models;

namespace K26_DotNet.Services;

public sealed partial class MomoSandboxPaymentGateway : IPaymentGateway
{
    private const string CreateEndpoint = "https://test-payment.momo.vn/v2/gateway/api/create";
    private const string QueryEndpoint = "https://test-payment.momo.vn/v2/gateway/api/query";
    private const string RequestType = "captureWallet";
    private const decimal MinimumAmount = 1_000m;
    private const decimal MaximumAmount = 50_000_000m;

    private readonly MomoSettingsService _settingsService;
    private readonly HttpClient _httpClient;
    private readonly MomoSignatureService _signatureService;
    private readonly Func<DateTime> _clock;

    public const string ProviderName = "MoMoSandbox";
    public string Provider => ProviderName;

    public MomoSandboxPaymentGateway(MomoSettingsService settingsService, HttpClient? httpClient = null,
        MomoSignatureService? signatureService = null, Func<DateTime>? clock = null)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _httpClient = httpClient ?? new HttpClient();
        if (_httpClient.Timeout == Timeout.InfiniteTimeSpan || _httpClient.Timeout > TimeSpan.FromSeconds(30))
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _signatureService = signatureService ?? new MomoSignatureService();
        _clock = clock ?? (() => DateTime.Now);
    }

    public async Task<PaymentSession> CreatePaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);
        var settings = GetEnabledSettings();
        var orderId = request.OrderId.Trim();
        var requestId = CreateRequestId("C");
        var orderInfo = request.Description.Trim();
        const string extraData = "";
        var signature = _signatureService.Sign(_signatureService.CreatePaymentRawSignature(
            settings.AccessKey.Trim(), request.Amount, extraData, settings.IpNUrl.Trim(), orderId, orderInfo,
            settings.PartnerCode.Trim(), settings.RedirectUrl.Trim(), requestId, RequestType), settings.SecretKey);
        var payload = new
        {
            partnerCode = settings.PartnerCode.Trim(), requestType = RequestType, ipnUrl = settings.IpNUrl.Trim(),
            redirectUrl = settings.RedirectUrl.Trim(), orderId, amount = decimal.ToInt64(request.Amount), orderInfo,
            requestId, extraData, lang = "vi", autoCapture = true, signature
        };

        using var response = await SendAsync(CreateEndpoint, payload, cancellationToken).ConfigureAwait(false);
        var body = await ReadResponseAsync(response, cancellationToken).ConfigureAwait(false);
        ValidateIdentity(body, settings.PartnerCode, orderId, requestId, request.Amount, requireTransactionId: false);
        var resultCode = GetRequiredInt(body, "resultCode");
        if (resultCode != 0)
            throw new InvalidOperationException($"MoMo Sandbox không thể tạo giao dịch (mã {resultCode.ToString(CultureInfo.InvariantCulture)}).");
        var payUrl = GetRequiredString(body, "payUrl");
        var qrCode = GetOptionalString(body, "qrCodeUrl");
        if (!Uri.TryCreate(payUrl, UriKind.Absolute, out var parsedPayUrl) || parsedPayUrl.Scheme != Uri.UriSchemeHttps ||
            !parsedPayUrl.Host.Equals("test-payment.momo.vn", StringComparison.OrdinalIgnoreCase) || parsedPayUrl.Port != 443 || !string.IsNullOrEmpty(parsedPayUrl.UserInfo))
            throw new InvalidOperationException("MoMo Sandbox trả về đường dẫn thanh toán không hợp lệ.");
        if (qrCode.Length > 1_000 || qrCode.Any(char.IsControl))
            throw new InvalidOperationException("MoMo Sandbox trả về dữ liệu QR không hợp lệ.");

        return new PaymentSession(orderId, requestId, Provider, parsedPayUrl.AbsoluteUri, qrCode, _clock(), GatewayPaymentStatus.Pending)
        {
            Amount = request.Amount,
            QrPayload = qrCode
        };
    }

    public async Task<PaymentGatewayStatus> QueryPaymentAsync(string orderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId) || !OrderIdPattern().IsMatch(orderId.Trim()))
            throw new ArgumentException("Mã đơn thanh toán không hợp lệ.", nameof(orderId));
        var settings = GetEnabledSettings();
        var trimmedOrderId = orderId.Trim();
        var requestId = CreateRequestId("Q");
        var signature = _signatureService.Sign(_signatureService.QueryPaymentRawSignature(
            settings.AccessKey.Trim(), trimmedOrderId, settings.PartnerCode.Trim(), requestId), settings.SecretKey);
        var payload = new { partnerCode = settings.PartnerCode.Trim(), requestId, orderId = trimmedOrderId, lang = "vi", signature };

        using var response = await SendAsync(QueryEndpoint, payload, cancellationToken).ConfigureAwait(false);
        var body = await ReadResponseAsync(response, cancellationToken).ConfigureAwait(false);
        var resultCode = GetRequiredInt(body, "resultCode");
        ValidateIdentity(body, settings.PartnerCode, trimmedOrderId, requestId, expectedAmount: null, requireTransactionId: resultCode == 0);
        var amount = GetRequiredInt64(body, "amount");
        if (amount <= 0) throw new InvalidOperationException("MoMo Sandbox trả về số tiền không hợp lệ.");
        var transactionId = GetOptionalString(body, "transId");
        return new PaymentGatewayStatus(trimmedOrderId, transactionId, amount, MapStatus(resultCode),
            resultCode.ToString(CultureInfo.InvariantCulture), SafeMessage(resultCode)) { Provider = Provider };
    }

    private MomoSettings GetEnabledSettings()
    {
        MomoSettingsService.Validate(_settingsService.Settings);
        return _settingsService.Settings;
    }

    private async Task<HttpResponseMessage> SendAsync(string endpoint, object payload, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                response.Dispose();
                throw new InvalidOperationException("MoMo Sandbox không thể xử lý yêu cầu hiện tại.");
            }
            return response;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("Yêu cầu MoMo Sandbox đã hết thời gian chờ.");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Không thể kết nối MoMo Sandbox.", ex);
        }
    }

    private static async Task<JsonElement> ReadResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false));
            return document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("MoMo Sandbox trả về dữ liệu không hợp lệ.", ex);
        }
    }

    private static void ValidateRequest(PaymentGatewayRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId) || request.OrderId.Trim().Length > 200 || !OrderIdPattern().IsMatch(request.OrderId.Trim())) throw new ArgumentException("Mã đơn thanh toán không hợp lệ.", nameof(request));
        if (request.Amount < MinimumAmount || request.Amount > MaximumAmount || decimal.Truncate(request.Amount) != request.Amount) throw new ArgumentOutOfRangeException(nameof(request), "Số tiền phải là VND nguyên trong giới hạn MoMo Sandbox.");
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 255) throw new ArgumentException("Nội dung thanh toán không hợp lệ.", nameof(request));
    }

    private static void ValidateIdentity(JsonElement body, string partnerCode, string orderId, string requestId, decimal? expectedAmount, bool requireTransactionId)
    {
        if (!string.Equals(GetRequiredString(body, "partnerCode"), partnerCode.Trim(), StringComparison.Ordinal) ||
            !string.Equals(GetRequiredString(body, "orderId"), orderId, StringComparison.Ordinal) ||
            !string.Equals(GetRequiredString(body, "requestId"), requestId, StringComparison.Ordinal))
            throw new InvalidOperationException("MoMo Sandbox trả về giao dịch không khớp yêu cầu.");
        var amount = GetRequiredInt64(body, "amount");
        if (amount <= 0 || amount != decimal.Truncate(amount)) throw new InvalidOperationException("MoMo Sandbox trả về số tiền không hợp lệ.");
        if (expectedAmount.HasValue && amount != expectedAmount.Value) throw new InvalidOperationException("MoMo Sandbox trả về số tiền không khớp yêu cầu.");
        if (requireTransactionId && (!long.TryParse(GetOptionalString(body, "transId"), NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0))
            throw new InvalidOperationException("MoMo Sandbox trả về mã giao dịch không hợp lệ.");
    }

    private static string GetRequiredString(JsonElement body, string property)
    {
        var value = GetOptionalString(body, property);
        return string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException("MoMo Sandbox trả về dữ liệu không hợp lệ.") : value;
    }

    private static string GetOptionalString(JsonElement body, string property) =>
        body.TryGetProperty(property, out var value) ? value.ToString() : string.Empty;

    private static int GetRequiredInt(JsonElement body, string property) =>
        body.TryGetProperty(property, out var value) && value.TryGetInt32(out var number) ? number : throw new InvalidOperationException("MoMo Sandbox trả về dữ liệu không hợp lệ.");

    private static long GetRequiredInt64(JsonElement body, string property) =>
        body.TryGetProperty(property, out var value) && value.TryGetInt64(out var number) ? number : throw new InvalidOperationException("MoMo Sandbox trả về dữ liệu không hợp lệ.");

    private static GatewayPaymentStatus MapStatus(int resultCode) => resultCode switch
    {
        0 => GatewayPaymentStatus.Success,
        1000 or 7000 or 7002 or 8000 or 9000 => GatewayPaymentStatus.Pending,
        1005 => GatewayPaymentStatus.Expired,
        1003 or 1006 => GatewayPaymentStatus.Cancelled,
        98 or 99 or 1001 or 1002 or 1004 or 1007 or 1017 or 1026 or 2019 or 4001 or 4002 or 4100
            => GatewayPaymentStatus.Failed,
        _ => GatewayPaymentStatus.Unknown
    };

    private static string SafeMessage(int resultCode) => MapStatus(resultCode) switch
    {
        GatewayPaymentStatus.Success => "Giao dịch MoMo Sandbox thành công.",
        GatewayPaymentStatus.Pending => "Giao dịch MoMo Sandbox đang chờ xử lý.",
        GatewayPaymentStatus.Expired => "Giao dịch MoMo Sandbox đã hết hạn.",
        GatewayPaymentStatus.Cancelled => "Giao dịch MoMo Sandbox đã bị hủy.",
        GatewayPaymentStatus.Unknown => "MoMo Sandbox trả về trạng thái chưa được nhận diện; giao dịch chưa được ghi nhận.",
        _ => "Giao dịch MoMo Sandbox không thành công."
    };

    private static string CreateRequestId(string purpose) => $"EDUFEE-{purpose}-{Guid.NewGuid():N}";

    [GeneratedRegex("^[0-9A-Za-z](?:[0-9A-Za-z._-]*[0-9A-Za-z])?$")]
    private static partial Regex OrderIdPattern();
}
