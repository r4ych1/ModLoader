using System.Collections.Generic;
using System.IO;

namespace ModLoader.Core;

public static class DropPathExpander
{
    public static IEnumerable<string> ExpandFiles(IEnumerable<string> droppedPaths, bool includeTopLevelDirectoryFiles)
    {
        foreach (var droppedPath in droppedPaths)
        {
            if (File.Exists(droppedPath))
            {
                yield return droppedPath;
                continue;
            }

            if (!includeTopLevelDirectoryFiles || !Directory.Exists(droppedPath))
            {
                continue;
            }

            foreach (var filePath in Directory.EnumerateFiles(droppedPath, "*", SearchOption.TopDirectoryOnly))
            {
                yield return filePath;
            }
        }
    }
}
