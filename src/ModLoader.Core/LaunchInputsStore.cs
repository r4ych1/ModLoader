using System;
using System.Collections.Generic;
using System.IO;

namespace ModLoader.Core;

public sealed class LaunchInputsStore
{
    private static readonly HashSet<string> SourcePortExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe"
    };

    private static readonly HashSet<string> IwadExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wad",
        ".pk3",
        ".iwad",
        ".ipk3",
        ".ipk7",
        ".pk7"
    };

    private static readonly HashSet<string> ModExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wad",
        ".pwad",
        ".pk3",
        ".pk7",
        ".ipk3",
        ".ipk7",
        ".pkz"
    };

    private readonly List<string> _iwads = [];
    private readonly List<string> _mods = [];
    private readonly HashSet<string> _iwadPathSet = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _modPathSet = new(StringComparer.OrdinalIgnoreCase);

    public string? SourcePortPath { get; private set; }

    public IReadOnlyList<string> Iwads => _iwads;

    public IReadOnlyList<string> Mods => _mods;

    public void ProcessSourcePortDrop(IEnumerable<string> droppedPaths)
    {
        string? finalAcceptedSourcePort = null;
        foreach (var filePath in DropPathExpander.ExpandFiles(droppedPaths, includeTopLevelDirectoryFiles: false))
        {
            if (HasAllowedExtension(filePath, SourcePortExtensions))
            {
                finalAcceptedSourcePort = PathNormalizer.NormalizeAbsolutePath(filePath);
            }
        }

        if (!string.IsNullOrWhiteSpace(finalAcceptedSourcePort))
        {
            SourcePortPath = finalAcceptedSourcePort;
        }
    }

    public void ProcessIwadDrop(IEnumerable<string> droppedPaths)
    {
        foreach (var filePath in DropPathExpander.ExpandFiles(droppedPaths, includeTopLevelDirectoryFiles: true))
        {
            if (!HasAllowedExtension(filePath, IwadExtensions))
            {
                continue;
            }

            AddUniquePath(filePath, _iwads, _iwadPathSet);
        }
    }

    public void ProcessModDrop(IEnumerable<string> droppedPaths)
    {
        foreach (var filePath in DropPathExpander.ExpandFiles(droppedPaths, includeTopLevelDirectoryFiles: true))
        {
            if (!HasAllowedExtension(filePath, ModExtensions))
            {
                continue;
            }

            AddUniquePath(filePath, _mods, _modPathSet);
        }
    }

    public void ClearSourcePort()
    {
        SourcePortPath = null;
    }

    public void RemoveIwad(string path)
    {
        RemovePath(path, _iwads, _iwadPathSet);
    }

    public void RemoveMod(string path)
    {
        RemovePath(path, _mods, _modPathSet);
    }

    private static bool HasAllowedExtension(string path, HashSet<string> allowedExtensions)
    {
        var extension = Path.GetExtension(path);
        return allowedExtensions.Contains(extension);
    }

    private static void AddUniquePath(string path, List<string> orderedPaths, HashSet<string> dedupeSet)
    {
        var normalizedPath = PathNormalizer.NormalizeAbsolutePath(path);
        if (!dedupeSet.Add(normalizedPath))
        {
            return;
        }

        orderedPaths.Add(normalizedPath);
    }

    private static void RemovePath(string path, List<string> orderedPaths, HashSet<string> dedupeSet)
    {
        var normalizedPath = PathNormalizer.NormalizeAbsolutePath(path);
        if (!dedupeSet.Remove(normalizedPath))
        {
            return;
        }

        orderedPaths.RemoveAll(existingPath => string.Equals(existingPath, normalizedPath, StringComparison.OrdinalIgnoreCase));
    }
}
