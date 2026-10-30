namespace SevenZip.FileManager2.Build;

/// <summary>Format-independent atomic replacement shared by image writers.</summary>
internal static class GeneratedFile
{
    public static void Write(string path, Action<Stream> encode)
    {
        string temporary = path + ".tmp";
        try
        {
            using (FileStream stream = File.Create(temporary))
            {
                encode(stream);
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
