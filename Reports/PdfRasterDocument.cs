using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text;

namespace K26_DotNet.Reports;

/// <summary>
/// Writes GDI+ rendered pages as a standards-compliant PDF. Rendering to an image keeps
/// Vietnamese text intact without requiring a printer driver or a third-party PDF package.
/// </summary>
internal static class PdfRasterDocument
{
    internal const int A4PortraitWidth = 1240;
    internal const int A4PortraitHeight = 1754;
    internal const int A4LandscapeWidth = A4PortraitHeight;
    internal const int A4LandscapeHeight = A4PortraitWidth;

    public static void Write(string filePath, IReadOnlyList<Bitmap> pages, bool landscape)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(pages);
        if (pages.Count == 0) throw new ArgumentException("PDF phải có ít nhất một trang.", nameof(pages));

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var objects = new List<byte[]>();
        const int catalogObject = 1;
        const int pagesObject = 2;
        int nextObject = 3;
        var pageObjects = new List<int>();

        foreach (var page in pages)
        {
            using var imageData = new MemoryStream();
            page.Save(imageData, ImageFormat.Jpeg);
            int imageObject = nextObject++;
            int contentObject = nextObject++;
            int pageObject = nextObject++;
            pageObjects.Add(pageObject);

            AddObjectAt(objects, imageObject, StreamObject(
                $"<< /Type /XObject /Subtype /Image /Width {page.Width} /Height {page.Height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode",
                imageData.ToArray()));

            var mediaWidth = landscape ? 841.89m : 595.28m;
            var mediaHeight = landscape ? 595.28m : 841.89m;
            var content = Encoding.ASCII.GetBytes($"q {mediaWidth.ToString(CultureInfo.InvariantCulture)} 0 0 {mediaHeight.ToString(CultureInfo.InvariantCulture)} 0 0 cm /PageImage Do Q");
            AddObjectAt(objects, contentObject, StreamObject("<<", content));
            AddObjectAt(objects, pageObject, Encoding.ASCII.GetBytes(
                $"<< /Type /Page /Parent {pagesObject} 0 R /MediaBox [0 0 {mediaWidth.ToString(CultureInfo.InvariantCulture)} {mediaHeight.ToString(CultureInfo.InvariantCulture)}] /Resources << /XObject << /PageImage {imageObject} 0 R >> >> /Contents {contentObject} 0 R >>"));
        }

        AddObjectAt(objects, catalogObject, Encoding.ASCII.GetBytes($"<< /Type /Catalog /Pages {pagesObject} 0 R >>"));
        AddObjectAt(objects, pagesObject, Encoding.ASCII.GetBytes($"<< /Type /Pages /Count {pageObjects.Count} /Kids [{string.Join(' ', pageObjects.Select(id => $"{id} 0 R"))}] >>"));

        string temporaryPath = filePath + ".tmp";
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true))
            {
                writer.Write(Encoding.ASCII.GetBytes("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n"));
                var offsets = new long[objects.Count + 1];
                for (int id = 1; id <= objects.Count; id++)
                {
                    offsets[id] = stream.Position;
                    writer.Write(Encoding.ASCII.GetBytes($"{id} 0 obj\n"));
                    writer.Write(objects[id - 1]);
                    writer.Write(Encoding.ASCII.GetBytes("\nendobj\n"));
                }

                long xrefOffset = stream.Position;
                writer.Write(Encoding.ASCII.GetBytes($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n"));
                for (int id = 1; id <= objects.Count; id++)
                    writer.Write(Encoding.ASCII.GetBytes($"{offsets[id]:D10} 00000 n \n"));
                writer.Write(Encoding.ASCII.GetBytes($"trailer\n<< /Size {objects.Count + 1} /Root {catalogObject} 0 R >>\nstartxref\n{xrefOffset}\n%%EOF"));
            }

            File.Move(temporaryPath, filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static byte[] StreamObject(string dictionaryStart, byte[] data)
    {
        return Encoding.ASCII.GetBytes($"{dictionaryStart} /Length {data.Length} >>\nstream\n")
            .Concat(data)
            .Concat(Encoding.ASCII.GetBytes("\nendstream"))
            .ToArray();
    }

    private static void AddObjectAt(List<byte[]> objects, int id, byte[] data)
    {
        while (objects.Count < id) objects.Add(Array.Empty<byte>());
        objects[id - 1] = data;
    }
}
