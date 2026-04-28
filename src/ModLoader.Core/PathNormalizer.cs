using System.IO;

namespace ModLoader.Core;

public static class PathNormalizer
{
    public static string NormalizeAbsolutePath(string path)
    {
        return Path.GetFullPath(path);
    }
}
