using System.Drawing;
using System.Drawing.Drawing2D;
using K26_DotNet.Services;
using QRCoder;

namespace _26K1_DotNet;

/// <summary>Hiển thị mã QR thanh toán chuẩn nhận diện VietQR hoặc MoMo và theo dõi trạng thái giao dịch.</summary>
public sealed class FormQrPayment : Form
{
    private readonly IPaymentGateway _gateway;
    private readonly PaymentSession _session;
    private readonly TimeSpan _pollInterval;
    private readonly CancellationTokenSource _pollCancellation = new();
    private readonly System.Windows.Forms.Timer _countdownTimer = new();
    private DateTime _expiryTime;
    private Label _status = null!;
    private Label _lblCountdown = null!;
    private Button _confirmTransferred = null!;
    private bool _isMomo;
    private bool _pollStarted;
    private bool _completed;
    private bool _pollDisposed;

    public PaymentGatewayStatus? PaymentStatus { get; private set; }

    public FormQrPayment(IPaymentGateway gateway, PaymentSession session, TimeSpan? pollInterval = null)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _pollInterval = pollInterval.GetValueOrDefault(TimeSpan.FromSeconds(4));
        if (_pollInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(pollInterval));
        BuildUI();
    }

    private void BuildUI()
    {
        bool isMomo = _session.Provider.Contains("momo", StringComparison.OrdinalIgnoreCase);
        _isMomo = isMomo;
        string providerDisplay = isMomo ? "MoMo" : "VietQR";

        Text = $"{providerDisplay} — Thanh toán học phí";
        ClientSize = new Size(480, 740);
        MinimumSize = MaximumSize = new Size(480, 740);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.Background;
        Font = UITheme.FontBody;

        Color brandColor = isMomo ? Color.FromArgb(165, 0, 100) : Color.FromArgb(0, 84, 166);
        string headerTitle = isMomo ? "Thanh toán MoMo" : "Thanh toán VietQR";
        string headerSub = isMomo
            ? "Mở app Ví MoMo và quét mã QR để nộp học phí"
            : "Quét mã bằng ứng dụng Ngân hàng (Mobile Banking)";
        var header = UITheme.CreateDialogHeader("", headerTitle, headerSub, brandColor, 68);

        var body = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.Surface, Padding = new Padding(24, 10, 24, 10) };

        // Logo thương hiệu chuẩn nhận diện
        var logo = isMomo ? BrandAssets.CreateMomoLogo(160, 42) : BrandAssets.CreateVietQrLogo(210, 42);
        var pbLogo = new PictureBox
        {
            Image = logo,
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = logo.Size,
            Location = new Point((480 - logo.Width) / 2, 8),
            BackColor = Color.Transparent
        };
        pbLogo.Disposed += (_, _) => logo.Dispose();
        body.Controls.Add(pbLogo);

        // Khung chứa mã QR
        var payload = !string.IsNullOrWhiteSpace(_session.QrPayload) ? _session.QrPayload : _session.PaymentUrl;
        if (string.IsNullOrWhiteSpace(payload)) payload = _session.OrderId;
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qr = new QRCode(data);
        var qrBitmap = qr.GetGraphic(8, Color.FromArgb(19, 30, 52), Color.White, true);

        var qrPanel = new Panel
        {
            Location = new Point(115, 56),
            Size = new Size(250, 250),
            BackColor = Color.White
        };
        qrPanel.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, qrPanel.Width - 1, qrPanel.Height - 1);
            using var pen = new Pen(isMomo ? Color.FromArgb(244, 114, 182) : Color.FromArgb(147, 197, 253), 1.5f);
            using var path = UITheme.GetRoundedPath(rect, 8);
            e.Graphics.DrawPath(pen, path);
        };

        var picture = new PictureBox
        {
            Image = qrBitmap,
            SizeMode = PictureBoxSizeMode.Zoom,
            Location = new Point(5, 5),
            Size = new Size(240, 240),
            BackColor = Color.White
        };
        picture.Disposed += (_, _) => qrBitmap.Dispose();
        qrPanel.Controls.Add(picture);
        body.Controls.Add(qrPanel);

        // Tải mẫu VietQR chính thức nếu có kết nối mạng
        if (!isMomo)
        {
            _ = TryLoadOnlineVietQrAsync(picture);
        }

        // Số tiền
        var lblAmount = new Label
        {
            Text = $"{_session.Amount:N0} ₫",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = brandColor,
            Location = new Point(20, 312),
            Size = new Size(440, 30),
            TextAlign = ContentAlignment.MiddleCenter
        };
        body.Controls.Add(lblAmount);

        // Thông tin thanh toán chi tiết
        string detailText = isMomo
            ? $"Cổng thanh toán: MoMo Sandbox\nMã đơn: {_session.OrderId}" + (_session.ExpiresAt.HasValue ? $"\nHạn quét mã: {_session.ExpiresAt:HH:mm dd/MM/yyyy}" : "")
            : $"Ngân hàng: VietinBank  ·  STK: 102888889999\nChủ TK: TRƯỜNG ĐẠI HỌC MỎ - ĐỊA CHẤT\nNội dung: {_session.OrderId}";
        var lblDetail = new Label
        {
            Text = detailText,
            Font = UITheme.FontSmall,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(20, 346),
            Size = new Size(440, 64),
            TextAlign = ContentAlignment.MiddleCenter
        };
        body.Controls.Add(lblDetail);

        var statusCaption = new Label
        {
            Text = isMomo ? "TRẠNG THÁI MOMO" : "XÁC NHẬN VIETQR",
            Font = UITheme.FontCardTitle,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(20, 410),
            Size = new Size(440, 18),
            TextAlign = ContentAlignment.MiddleCenter
        };
        body.Controls.Add(statusCaption);

        // Đếm ngược thời gian hết hạn mã QR
        _lblCountdown = new Label
        {
            Font = UITheme.FontSmallBold,
            ForeColor = Color.FromArgb(225, 29, 72),
            Location = new Point(20, 428),
            Size = new Size(440, 24),
            TextAlign = ContentAlignment.MiddleCenter
        };
        body.Controls.Add(_lblCountdown);

        // Trạng thái giao dịch
        _status = new Label
        {
            Text = isMomo
                ? "Đang chờ MoMo xác nhận · tự kiểm tra mỗi vài giây"
                : "Chờ quản trị viên đối soát giao dịch",
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.WarningDark,
            Location = new Point(20, 456),
            Size = new Size(440, 26),
            TextAlign = ContentAlignment.MiddleCenter
        };
        body.Controls.Add(_status);

        _expiryTime = _session.ExpiresAt.HasValue && _session.ExpiresAt.Value > DateTime.Now
            ? _session.ExpiresAt.Value
            : DateTime.Now.AddMinutes(15);
        UpdateCountdown();
        _countdownTimer.Interval = 1000;
        _countdownTimer.Tick += (_, _) => UpdateCountdown();
        _countdownTimer.Start();

        // VietQR không có API đối soát trong ứng dụng desktop; quản trị viên xác nhận sau khi đối soát.
        _confirmTransferred = UITheme.PrimaryBtn("Quản trị viên xác nhận đã nhận tiền", 290, 34);
        _confirmTransferred.Location = new Point(95, 488);
        _confirmTransferred.Visible = !isMomo && _gateway is VietQrPaymentGateway;
        _confirmTransferred.AccessibleName = "Quản trị viên xác nhận đã nhận tiền";
        _confirmTransferred.Click += (_, _) => ConfirmVietQrTransfer();
        body.Controls.Add(_confirmTransferred);

        var confirmationNote = new Label
        {
            Text = "Chỉ xác nhận sau khi đối soát sao kê ngân hàng.",
            Location = new Point(20, 526), Size = new Size(440, 20),
            Font = UITheme.FontSmall, ForeColor = UITheme.TextSecondary,
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = !isMomo && _gateway is VietQrPaymentGateway
        };
        body.Controls.Add(confirmationNote);

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 66, BackColor = UITheme.Surface };
        footer.Controls.Add(UITheme.HSep(DockStyle.Top));
        var cancel = UITheme.GhostBtn("Hủy", 80, 38);
        cancel.Click += (_, _) => Close();
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 13, 22, 0),
            BackColor = Color.Transparent
        };
        actions.Controls.Add(cancel);
        footer.Controls.Add(actions);
        CancelButton = cancel;

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(header);
    }

    private async Task TryLoadOnlineVietQrAsync(PictureBox picture)
    {
        try
        {
            string bank = "970415"; // VietinBank BIN
            string acc = "102888889999";
            string amount = ((long)_session.Amount).ToString();
            string desc = Uri.EscapeDataString(_session.OrderId);
            string name = Uri.EscapeDataString("TRUONG DAI HOC MO DIA CHAT");
            string url = $"https://img.vietqr.io/image/{bank}-{acc}-compact.png?amount={amount}&addInfo={desc}&accountName={name}";
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2.5) };
            var bytes = await client.GetByteArrayAsync(url);
            using var ms = new MemoryStream(bytes);
            var img = Image.FromStream(ms);
            if (IsHandleCreated && !IsDisposed && !picture.IsDisposed)
            {
                Invoke(() =>
                {
                    var old = picture.Image;
                    picture.Image = new Bitmap(img);
                    old?.Dispose();
                });
            }
        }
        catch
        {
            // Ngoại tuyến: giữ mã QR offline nội bộ đã vẽ
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (!_pollStarted) { _pollStarted = true; _ = PollUntilCompleteAsync(); }
    }

    private void UpdateCountdown()
    {
        if (IsDisposed) return;
        var remaining = _expiryTime - DateTime.Now;
        if (remaining > TimeSpan.Zero)
        {
            _lblCountdown.Text = remaining.TotalMinutes >= 1
                ? $"Mã sẽ hết hạn sau: {remaining.Minutes:D2} phút {remaining.Seconds:D2} giây"
                : $"Mã sẽ hết hạn sau: {remaining.Seconds:D2} giây";
            _lblCountdown.ForeColor = Color.FromArgb(225, 29, 72);
        }
        else
        {
            _countdownTimer.Stop();
            _lblCountdown.Text = "Mã QR đã hết hạn. Vui lòng tạo mã mới.";
            _lblCountdown.ForeColor = UITheme.Danger;
            if (_status != null)
            {
                _status.Text = "Giao dịch đã hết hạn · tạo một mã mới để tiếp tục.";
                _status.ForeColor = UITheme.Danger;
            }
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _countdownTimer.Stop();
        StopPolling();
        base.OnFormClosed(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _countdownTimer.Stop();
            _countdownTimer.Dispose();
            StopPolling();
        }
        base.Dispose(disposing);
    }

    private void StopPolling()
    {
        if (_pollDisposed) return;
        _pollDisposed = true;
        _pollCancellation.Cancel();
        _pollCancellation.Dispose();
    }

    private async Task PollUntilCompleteAsync()
    {
        var token = _pollCancellation.Token;
        while (!token.IsCancellationRequested && !_completed)
        {
            try
            {
                var status = await _gateway.QueryPaymentAsync(_session.OrderId, token);
                if (token.IsCancellationRequested) return;
                if (status.Status == GatewayPaymentStatus.Success)
                {
                    PaymentStatus = status;
                    _completed = true;
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }
                _status.Text = StatusText(status);
                if (status.Status is GatewayPaymentStatus.Failed or GatewayPaymentStatus.Cancelled or GatewayPaymentStatus.Expired)
                {
                    PaymentStatus = status;
                    _completed = true;
                    _status.ForeColor = UITheme.Danger;
                    return;
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
            catch
            {
                _status.Text = _isMomo
                    ? "Chưa thể kiểm tra MoMo · hệ thống sẽ tự thử lại..."
                    : "Chưa thể kiểm tra giao dịch · hệ thống sẽ tự thử lại...";
                _status.ForeColor = UITheme.WarningDark;
            }
            try { await Task.Delay(_pollInterval, token); }
            catch (OperationCanceledException) { return; }
        }
    }

    private void ConfirmVietQrTransfer()
    {
        if (_gateway is VietQrPaymentGateway vietQr)
        {
            vietQr.ConfirmTransferred(_session.OrderId);
            _confirmTransferred.Enabled = false;
            _status.Text = "Đã xác nhận đối soát · đang ghi nhận giao dịch...";
        }
    }

    private string StatusText(PaymentGatewayStatus status) => status.Status switch
    {
        GatewayPaymentStatus.Pending => _isMomo
            ? "Đang chờ MoMo xác nhận · tự kiểm tra mỗi vài giây"
            : "Chờ quản trị viên đối soát giao dịch",
        GatewayPaymentStatus.Failed => "Giao dịch không thành công.",
        GatewayPaymentStatus.Cancelled => "Giao dịch đã bị hủy.",
        GatewayPaymentStatus.Expired => "Giao dịch đã hết hạn.",
        _ => string.IsNullOrWhiteSpace(status.Message) ? "Đang kiểm tra giao dịch..." : status.Message
    };
}
