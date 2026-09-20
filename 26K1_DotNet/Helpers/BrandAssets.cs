using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;

namespace _26K1_DotNet;

internal static class BrandAssets
{
    private const string HumgLogoResource = "K26_DotNet.Assets.HumgLogo.png";

    public static Bitmap? TryLoadHumgLogo()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(HumgLogoResource);
            if (stream == null) return null;
            using var source = Image.FromStream(stream);
            return new Bitmap(source);
        }
        catch
        {
            return null;
        }
    }

    private const string VietQrLogoResource = "K26_DotNet.Assets.VietQrLogo.png";
    private const string MomoLogoResource = "K26_DotNet.Assets.MomoLogo.png";

    /// <summary>Tải logo VietQR | NAPAS 247 từ tài nguyên nhúng PNG.</summary>
    public static Bitmap CreateVietQrLogo(int width = 210, int height = 42)
    {
        var loaded = TryLoadResource(VietQrLogoResource, width, height);
        if (loaded != null) return loaded;
        return CreateVietQrLogoFallback(width, height);
    }

    /// <summary>Tải logo Ví MoMo từ tài nguyên nhúng PNG.</summary>
    public static Bitmap CreateMomoLogo(int width = 160, int height = 42)
    {
        var loaded = TryLoadResource(MomoLogoResource, width, height);
        if (loaded != null) return loaded;
        return CreateMomoLogoFallback(width, height);
    }

    private static Bitmap? TryLoadResource(string resourceName, int width, int height)
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null) return null;
            using var source = Image.FromStream(stream);
            var bmp = new Bitmap(width, height);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(Color.Transparent);
            // Giữ tỷ lệ khung hình
            float scale = Math.Min((float)width / source.Width, (float)height / source.Height);
            int w = (int)(source.Width * scale);
            int h = (int)(source.Height * scale);
            g.DrawImage(source, (width - w) / 2, (height - h) / 2, w, h);
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Fallback: Vẽ biểu trưng VietQR bằng GDI+ khi không tìm thấy tài nguyên PNG.</summary>
    private static Bitmap CreateVietQrLogoFallback(int width, int height)
    {
        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(Color.Transparent);

        var cardRect = new Rectangle(0, 0, width - 1, height - 1);
        using (var bgBrush = new SolidBrush(Color.White))
        using (var borderPen = new Pen(Color.FromArgb(226, 232, 240), 1))
        using (var path = UITheme.GetRoundedPath(cardRect, 8))
        {
            g.FillPath(bgBrush, path);
            g.DrawPath(borderPen, path);
        }

        using (var vietFont = new Font("Segoe UI", 15F, FontStyle.Bold))
        using (var vietBrush = new SolidBrush(Color.FromArgb(0, 84, 166)))
        {
            g.DrawString("Viet", vietFont, vietBrush, new PointF(10, 8));
        }

        var qrRect = new Rectangle(62, 7, 40, 28);
        using (var qrBgBrush = new SolidBrush(Color.FromArgb(237, 28, 36)))
        using (var qrPath = UITheme.GetRoundedPath(qrRect, 5))
        {
            g.FillPath(qrBgBrush, qrPath);
        }
        using (var qrFont = new Font("Segoe UI", 13F, FontStyle.Bold))
        using (var whiteBrush = new SolidBrush(Color.White))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("QR", qrFont, whiteBrush, qrRect, sf);
        }

        using (var sepPen = new Pen(Color.FromArgb(203, 213, 225), 1))
        {
            g.DrawLine(sepPen, 110, 10, 110, 34);
        }

        using (var napasFont = new Font("Segoe UI", 8.5F, FontStyle.Bold))
        using (var napasBrush = new SolidBrush(Color.FromArgb(0, 45, 98)))
        using (var orangeBrush = new SolidBrush(Color.FromArgb(245, 130, 32)))
        {
            g.DrawString("napas", napasFont, napasBrush, new PointF(118, 9));
            g.DrawString("247", napasFont, orangeBrush, new PointF(164, 9));
        }
        using (var subFont = new Font("Segoe UI", 6.5F, FontStyle.Regular))
        using (var subBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
        {
            g.DrawString("Chuyển nhanh 24/7", subFont, subBrush, new PointF(118, 24));
        }

        return bmp;
    }

    /// <summary>Fallback: Vẽ biểu trưng Ví MoMo bằng GDI+ khi không tìm thấy tài nguyên PNG.</summary>
    private static Bitmap CreateMomoLogoFallback(int width, int height)
    {
        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(Color.Transparent);

        var cardRect = new Rectangle(0, 0, width - 1, height - 1);
        using (var bgBrush = new SolidBrush(Color.White))
        using (var borderPen = new Pen(Color.FromArgb(226, 232, 240), 1))
        using (var path = UITheme.GetRoundedPath(cardRect, 8))
        {
            g.FillPath(bgBrush, path);
            g.DrawPath(borderPen, path);
        }

        var iconRect = new Rectangle(8, 6, 32, 32);
        using (var momoBrush = new SolidBrush(Color.FromArgb(165, 0, 100)))
        using (var iconPath = UITheme.GetRoundedPath(iconRect, 8))
        {
            g.FillPath(momoBrush, iconPath);
        }

        using (var iconFont = new Font("Segoe UI", 7.5F, FontStyle.Bold))
        using (var whiteBrush = new SolidBrush(Color.White))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("mo", iconFont, whiteBrush, new Rectangle(8, 7, 32, 14), sf);
            g.DrawString("mo", iconFont, whiteBrush, new Rectangle(8, 20, 32, 14), sf);
        }

        using (var brandFont = new Font("Segoe UI", 13.5F, FontStyle.Bold))
        using (var momoTextBrush = new SolidBrush(Color.FromArgb(165, 0, 100)))
        {
            g.DrawString("MoMo", brandFont, momoTextBrush, new PointF(46, 6));
        }
        using (var subFont = new Font("Segoe UI", 7F, FontStyle.Regular))
        using (var subBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
        {
            g.DrawString("Ví Điện Tử An Toàn", subFont, subBrush, new PointF(48, 25));
        }

        return bmp;
    }
}

