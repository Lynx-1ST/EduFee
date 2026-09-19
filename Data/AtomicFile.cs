using System.Text;

namespace K26_DotNet.Data;

/// <summary>Replace a complete file only after the new contents have reached disk.</summary>
public static class AtomicFile
{
    public static void WriteAllText(string path, string contents)
    {
        var destination = Path.GetFullPath(path);
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = Encoding.UTF8.GetBytes(contents);
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            if (File.Exists(destination))
                File.Replace(temporary, destination, destination + ".bak");
            else
                File.Move(temporary, destination);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
