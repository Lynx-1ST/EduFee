using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using K26_DotNet.Data;
using K26_DotNet.Models;

namespace K26_DotNet.Services;

public sealed class MomoSettingsService
{
    private const string EncryptedSecretKeyName = "EncryptedSecretKey";
    private readonly string _settingsFilePath;

    public MomoSettings Settings { get; private set; } = new();

    public MomoSettingsService(string settingsFilePath = "momo-settings.json")
    {
        _settingsFilePath = settingsFilePath;
        LoadSettings();
    }

    public void LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsFilePath)) return;
            var json = File.ReadAllText(_settingsFilePath);
            if (string.IsNullOrWhiteSpace(json)) return;

            var root = JsonNode.Parse(json)?.AsObject()
                ?? throw new JsonException("Cấu hình MoMo không hợp lệ.");
            var settings = new MomoSettings
            {
                Enabled = root[nameof(MomoSettings.Enabled)]?.GetValue<bool>() ?? false,
                PartnerCode = root[nameof(MomoSettings.PartnerCode)]?.GetValue<string>() ?? string.Empty,
                AccessKey = root[nameof(MomoSettings.AccessKey)]?.GetValue<string>() ?? string.Empty,
                UseSandbox = root[nameof(MomoSettings.UseSandbox)]?.GetValue<bool>() ?? true,
                RedirectUrl = root[nameof(MomoSettings.RedirectUrl)]?.GetValue<string>() ?? "https://localhost/",
                IpNUrl = root[nameof(MomoSettings.IpNUrl)]?.GetValue<string>() ?? "https://localhost/"
            };
            var encrypted = root[EncryptedSecretKeyName]?.GetValue<string>();
            if (!string.IsNullOrEmpty(encrypted))
            {
                if (!encrypted.StartsWith("dpapi:", StringComparison.Ordinal))
                    throw new CryptographicException("Khóa bí mật MoMo không được mã hóa bằng DPAPI.");
                settings.SecretKey = Encoding.UTF8.GetString(ProtectedData.Unprotect(
                    Convert.FromBase64String(encrypted[6..]), null, DataProtectionScope.CurrentUser));
            }

            Validate(settings, allowDisabledIncomplete: true);
            Settings = settings;
        }
        catch (Exception ex) when (ex is IOException or JsonException or CryptographicException or FormatException or ArgumentException or InvalidOperationException)
        {
            throw new IOException("Không thể đọc cấu hình MoMo. Khóa đã mã hóa chỉ dùng được với tài khoản Windows đã lưu nó.", ex);
        }
    }

    public void SaveSettings()
    {
        try
        {
            Validate(Settings, allowDisabledIncomplete: true);
            var stored = new JsonObject
            {
                [nameof(MomoSettings.Enabled)] = Settings.Enabled,
                [nameof(MomoSettings.PartnerCode)] = Settings.PartnerCode.Trim(),
                [nameof(MomoSettings.AccessKey)] = Settings.AccessKey.Trim(),
                [nameof(MomoSettings.UseSandbox)] = Settings.UseSandbox,
                [nameof(MomoSettings.RedirectUrl)] = Settings.RedirectUrl.Trim(),
                [nameof(MomoSettings.IpNUrl)] = Settings.IpNUrl.Trim(),
                [EncryptedSecretKeyName] = string.IsNullOrEmpty(Settings.SecretKey) ? string.Empty : "dpapi:" + Convert.ToBase64String(
                    ProtectedData.Protect(Encoding.UTF8.GetBytes(Settings.SecretKey), null, DataProtectionScope.CurrentUser))
            };
            AtomicFile.WriteAllText(_settingsFilePath, stored.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException or CryptographicException or ArgumentException)
        {
            throw new IOException("Không thể lưu cấu hình MoMo.", ex);
        }
    }

    public static void Validate(MomoSettings settings, bool allowDisabledIncomplete = false)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.UseSandbox)
            throw new ArgumentException("Chỉ hỗ trợ MoMo Sandbox.", nameof(settings));
        if (allowDisabledIncomplete && !settings.Enabled) return;
        if (string.IsNullOrWhiteSpace(settings.PartnerCode) || string.IsNullOrWhiteSpace(settings.AccessKey) || string.IsNullOrWhiteSpace(settings.SecretKey))
            throw new ArgumentException("Cấu hình MoMo Sandbox chưa đầy đủ.", nameof(settings));
        if (!IsHttpsUrl(settings.RedirectUrl) || !IsHttpsUrl(settings.IpNUrl))
            throw new ArgumentException("URL trả về và IPN của MoMo Sandbox phải dùng HTTPS hợp lệ.", nameof(settings));
    }

    private static bool IsHttpsUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}
