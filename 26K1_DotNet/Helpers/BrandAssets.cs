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
}
