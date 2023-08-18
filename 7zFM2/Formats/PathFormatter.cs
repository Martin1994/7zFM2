namespace SevenZip.FileManager2.Formats;

public class PathFormatter
{
    public static string AddTrailingSlash(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar))
        {
            return path;
        }
        else
        {
            return path + Path.DirectorySeparatorChar;
        }
    }
}
