using System.Text.Json.Serialization;

namespace K26_DotNet.Models;

public sealed class MomoSettings
{
    public bool Enabled { get; set; }
    public string PartnerCode { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>Only kept in memory. It is written to disk as EncryptedSecretKey.</summary>
    [JsonIgnore]
    public string SecretKey { get; set; } = string.Empty;
    public bool UseSandbox { get; set; } = true;
    /// <summary>HTTPS callback placeholders. A public endpoint is required before relying on redirects or IPN.</summary>
    public string RedirectUrl { get; set; } = "https://localhost/";
    public string IpNUrl { get; set; } = "https://localhost/";
}
