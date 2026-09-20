using System.Drawing;
using K26_DotNet.Models;
using K26_DotNet.Services;

namespace _26K1_DotNet;

/// <summary>Edits local MoMo Sandbox credentials without revealing a stored secret.</summary>
public sealed class FormMomoSettings : Form
{
    private readonly MomoSettingsService _settingsService;
    private CheckBox _enabled = null!;
    private TextBox _partnerCode = null!, _accessKey = null!, _secretKey = null!;
    private ErrorProvider _errors = null!;

    public FormMomoSettings(MomoSettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        BuildUI();
        LoadSettings();
    }

    private void BuildUI()
    {
        Text = "Cấu hình MoMo Sandbox";
        ClientSize = new Size(540, 430);
        MinimumSize = MaximumSize = Size;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        BackColor = UITheme.Surface;
        Font = UITheme.FontBody;
        _errors = new ErrorProvider(this) { BlinkStyle = ErrorBlinkStyle.NeverBlink };

        var header = UITheme.CreateDialogHeader("", "MoMo Sandbox", "Chỉ dùng môi trường thử nghiệm; không hỗ trợ Production.", UITheme.PrimaryDark, 64);
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 18, 28, 12), BackColor = UITheme.Surface };
        _enabled = new CheckBox { Text = "Bật MoMo Sandbox", Location = new Point(28, 18), AutoSize = true, Font = UITheme.FontSmallBold, ForeColor = UITheme.Primary, AccessibleName = "Bật MoMo Sandbox" };
        _enabled.CheckedChanged += (_, _) => UpdateFieldState();
        body.Controls.Add(_enabled);
        body.Controls.Add(new Label { Text = "Partner Code *", Location = new Point(28, 60), AutoSize = true, Font = UITheme.FontSmallBold, ForeColor = UITheme.TextSecondary });
        _partnerCode = Field(160, 56, "Partner Code"); body.Controls.Add(_partnerCode);
        body.Controls.Add(new Label { Text = "Access Key *", Location = new Point(28, 104), AutoSize = true, Font = UITheme.FontSmallBold, ForeColor = UITheme.TextSecondary });
        _accessKey = Field(160, 100, "Access Key"); body.Controls.Add(_accessKey);
        body.Controls.Add(new Label { Text = "Secret Key *", Location = new Point(28, 148), AutoSize = true, Font = UITheme.FontSmallBold, ForeColor = UITheme.TextSecondary });
        _secretKey = Field(160, 144, "Secret Key"); _secretKey.UseSystemPasswordChar = true; body.Controls.Add(_secretKey);
        body.Controls.Add(new Label { Text = "Để trống Secret Key để giữ khóa bí mật đã lưu. Khóa luôn được che và lưu bằng Windows DPAPI.", Location = new Point(160, 180), Size = new Size(344, 38), Font = UITheme.FontSmall, ForeColor = UITheme.TextMuted });
        body.Controls.Add(new Label { Text = "Môi trường: Sandbox", Location = new Point(28, 238), AutoSize = true, Font = UITheme.FontSmallBold, ForeColor = UITheme.SimText });
        var validate = UITheme.GhostBtn("Kiểm tra cấu hình", 145, 30);
        validate.Location = new Point(160, 232);
        validate.Click += (_, _) => ValidateLocally();
        body.Controls.Add(validate);

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = UITheme.SurfaceAlt };
        footer.Controls.Add(UITheme.HSep(DockStyle.Top));
        var save = UITheme.PrimaryBtn("Lưu cấu hình", 125, 38); save.Click += (_, _) => Save();
        var close = UITheme.GhostBtn("Đóng", 80, 38); close.Click += (_, _) => Close();
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, Padding = new Padding(0, 13, 24, 0), BackColor = Color.Transparent };
        buttons.Controls.AddRange(new Control[] { save, close }); footer.Controls.Add(buttons);
        AcceptButton = save; CancelButton = close;
        Controls.Add(body); Controls.Add(footer); Controls.Add(header);
    }

    private static TextBox Field(int x, int y, string accessibleName) => new() { Location = new Point(x, y), Size = new Size(344, 28), Font = UITheme.FontBody, BackColor = UITheme.SurfaceAlt, AccessibleName = accessibleName };

    private void LoadSettings()
    {
        var settings = _settingsService.Settings;
        _enabled.Checked = settings.Enabled;
        _partnerCode.Text = settings.PartnerCode;
        _accessKey.Text = settings.AccessKey;
        _secretKey.Clear(); // Never display even the decrypted in-memory secret.
        UpdateFieldState();
    }

    private void UpdateFieldState()
    {
        var enabled = _enabled.Checked;
        _partnerCode.Enabled = _accessKey.Enabled = _secretKey.Enabled = enabled;
    }

    private void Save()
    {
        _errors.Clear();
        var settings = _settingsService.Settings;
        var prior = new MomoSettings
        {
            Enabled = settings.Enabled, PartnerCode = settings.PartnerCode, AccessKey = settings.AccessKey,
            SecretKey = settings.SecretKey, UseSandbox = settings.UseSandbox
        };
        settings.Enabled = _enabled.Checked;
        settings.UseSandbox = true;
        settings.PartnerCode = _partnerCode.Text.Trim();
        settings.AccessKey = _accessKey.Text.Trim();
        if (!string.IsNullOrWhiteSpace(_secretKey.Text)) settings.SecretKey = _secretKey.Text.Trim();
        try
        {
            MomoSettingsService.Validate(settings, allowDisabledIncomplete: true);
            _settingsService.SaveSettings();
            UiFeedback.ShowSuccess("Đã lưu cấu hình MoMo Sandbox.");
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ArgumentException ex)
        {
            Restore(settings, prior);
            if (_enabled.Checked)
            {
                if (string.IsNullOrWhiteSpace(settings.PartnerCode)) _errors.SetError(_partnerCode, "Vui lòng nhập Partner Code.");
                if (string.IsNullOrWhiteSpace(settings.AccessKey)) _errors.SetError(_accessKey, "Vui lòng nhập Access Key.");
                if (string.IsNullOrWhiteSpace(settings.SecretKey)) _errors.SetError(_secretKey, "Vui lòng nhập Secret Key.");
            }
            UiFeedback.ShowWarning(ex.Message);
        }
        catch (Exception ex)
        {
            Restore(settings, prior);
            UiFeedback.ShowException(ex, "Lỗi lưu cấu hình MoMo");
        }
    }

    private void ValidateLocally()
    {
        var candidate = new MomoSettings
        {
            Enabled = _enabled.Checked,
            PartnerCode = _partnerCode.Text.Trim(),
            AccessKey = _accessKey.Text.Trim(),
            SecretKey = string.IsNullOrWhiteSpace(_secretKey.Text) ? _settingsService.Settings.SecretKey : _secretKey.Text.Trim(),
            UseSandbox = true
        };
        try
        {
            MomoSettingsService.Validate(candidate, allowDisabledIncomplete: true);
            UiFeedback.ShowSuccess("Cấu hình hợp lệ tại máy này. Chưa gửi yêu cầu đến MoMo.");
        }
        catch (ArgumentException ex) { UiFeedback.ShowWarning(ex.Message); }
    }

    private static void Restore(MomoSettings target, MomoSettings source)
    {
        target.Enabled = source.Enabled;
        target.PartnerCode = source.PartnerCode;
        target.AccessKey = source.AccessKey;
        target.SecretKey = source.SecretKey;
        target.UseSandbox = source.UseSandbox;
    }
}
