using System.Security.Cryptography;
using System.Text;
using System.Globalization;

namespace K26_DotNet.Services;

public sealed class MomoSignatureService
{
    public string CreatePaymentRawSignature(
        string accessKey, decimal amount, string extraData, string ipnUrl, string orderId,
        string orderInfo, string partnerCode, string redirectUrl, string requestId, string requestType) =>
        string.Create(CultureInfo.InvariantCulture,
            $"accessKey={accessKey}&amount={amount:0}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}");

    public string QueryPaymentRawSignature(string accessKey, string orderId, string partnerCode, string requestId) =>
        $"accessKey={accessKey}&orderId={orderId}&partnerCode={partnerCode}&requestId={requestId}";

    public string Sign(string rawSignature, string secretKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawSignature);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawSignature))).ToLowerInvariant();
    }
}
