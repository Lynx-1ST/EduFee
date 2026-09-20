using K26_DotNet.Services;
using QRCoder;

namespace _26K1_DotNet;

/// <summary>Preview and explicit confirmation for a simulated QR payment session.</summary>
public sealed class FormQrPayment : Form
{
    private readonly IQrPaymentGateway _gateway;
    private readonly QrPaymentSession _session;
    private bool _confirmed;

    public QrPaymentConfirmation? Confirmation { get; private set; }

    public FormQrPayment(IQrPaymentGateway gateway, QrPaymentSession session)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        BuildUI();
    }

    private void BuildUI()
    {
        Text = $"{_session.ProviderName} — Thanh toán mô phỏng";
        ClientSize = new Size(460, 700);
        MinimumSize = new Size(460, 700);
        MaximumSize = new Size(460, 700);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.Background;
        Font = UITheme.FontBody;

        var header = UITheme.CreateDialogHeader("", $"QR { _session.ProviderName }",
            "MÔ PHỎNG — không kết nối tài khoản hoặc ví thật", UITheme.PrimaryDark, 70);

        var body = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.Surface, Padding = new Padding(28, 16, 28, 12) };
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(_session.Payload, QRCodeGenerator.ECCLevel.Q);
        using var qr = new QRCode(data);
        var qrBitmap = qr.GetGraphic(8, Color.FromArgb(19, 30, 52), Color.White, true);
        var picture = new PictureBox
        {
            Image = qrBitmap,
            SizeMode = PictureBoxSizeMode.Zoom,
            Location = new Point(100, 14),
            Size = new Size(260, 260),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        picture.Disposed += (s, e) => qrBitmap.Dispose();
        body.Controls.Add(picture);

        var provider = new Label
        {
            Text = _session.ProviderName,
            Font = UITheme.FontH2,
            ForeColor = _session.Provider == QrPaymentProvider.MoMo ? Color.FromArgb(166, 30, 105) : UITheme.Primary,
            Location = new Point(28, 286), Size = new Size(404, 28), TextAlign = ContentAlignment.MiddleCenter
        };
        var amount = new Label
        {
            Text = $"{_session.Amount:N0} ₫",
            Font = UITheme.FontH1,
            ForeColor = UITheme.Danger,
            Location = new Point(28, 318), Size = new Size(404, 38), TextAlign = ContentAlignment.MiddleCenter
        };
        var description = new Label
        {
            Text = _session.Description,
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.TextPrimary,
            Location = new Point(28, 362), Size = new Size(404, 42), TextAlign = ContentAlignment.MiddleCenter
        };
        var transaction = new Label
        {
            Text = $"Mã giao dịch: {_session.TransactionId}\nHết hạn: {_session.ExpiresAt:HH:mm:ss dd/MM/yyyy}",
            Font = UITheme.FontSmall,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(28, 408), Size = new Size(404, 42), TextAlign = ContentAlignment.MiddleCenter
        };
        var notice = new Label
        {
            Text = "Quét mã chỉ hiển thị nội dung mô phỏng. Bấm xác nhận bên dưới để giả lập callback thành công và ghi nhận biên lai.",
            Font = UITheme.FontSmall,
            ForeColor = UITheme.WarningDark,
            Location = new Point(28, 458), Size = new Size(404, 48), TextAlign = ContentAlignment.MiddleCenter
        };
        body.Controls.AddRange(new Control[] { provider, amount, description, transaction, notice });

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 66, BackColor = UITheme.Surface };
        footer.Controls.Add(UITheme.HSep(DockStyle.Top));
        var confirm = UITheme.PrimaryBtn("Xác nhận đã thanh toán", 190, 38);
        confirm.Click += (s, e) => ConfirmPayment(confirm);
        var cancel = UITheme.GhostBtn("Hủy", 80, 38);
        cancel.Click += (s, e) => Close();
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right, AutoSize = true, WrapContents = false,
            Padding = new Padding(0, 13, 22, 0), BackColor = Color.Transparent
        };
        actions.Controls.AddRange(new Control[] { confirm, cancel });
        footer.Controls.Add(actions);
        AcceptButton = confirm;
        CancelButton = cancel;

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(header);
    }

    private void ConfirmPayment(Button button)
    {
        if (_confirmed) return;
        button.Enabled = false;
        try
        {
            Confirmation = _gateway.Confirm(_session);
            if (!Confirmation.IsSuccessful)
            {
                UiFeedback.ShowError(Confirmation.Message);
                return;
            }
            _confirmed = true;
            DialogResult = DialogResult.OK;
            Close();
        }
        finally
        {
            if (!_confirmed) button.Enabled = true;
        }
    }
}
