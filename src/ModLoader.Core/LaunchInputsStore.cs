using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

    public LaunchInputsStore()
    {
    }

    public LaunchInputsStore(LaunchInputsConfig initialState)
    {
        LoadFromConfig(initialState);
    }

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

    public void LoadFromConfig(LaunchInputsConfig state)
    {
        SourcePortPath = null;
        _iwads.Clear();
        _mods.Clear();
        _iwadPathSet.Clear();
        _modPathSet.Clear();

        if (!string.IsNullOrWhiteSpace(state.SourcePortPath))
        {
            SourcePortPath = PathNormalizer.NormalizeAbsolutePath(state.SourcePortPath);
        }

        foreach (var iwadPath in state.Iwads ?? [])
        {
            if (string.IsNullOrWhiteSpace(iwadPath))
            {
                continue;
            }

            AddUniquePath(iwadPath, _iwads, _iwadPathSet);
        }

        foreach (var modPath in state.Mods ?? [])
        {
            if (string.IsNullOrWhiteSpace(modPath))
            {
                continue;
            }

            AddUniquePath(modPath, _mods, _modPathSet);
        }
    }

    public bool RemoveMissingPaths()
    {
        var changed = false;

        if (!string.IsNullOrWhiteSpace(SourcePortPath) && !File.Exists(SourcePortPath))
        {
            SourcePortPath = null;
            changed = true;
        }

        changed |= RemoveMissingEntries(_iwads, _iwadPathSet);
        changed |= RemoveMissingEntries(_mods, _modPathSet);

        return changed;
    }

    public LaunchInputsConfig CreateSnapshot()
    {
        return new LaunchInputsConfig
        {
            SourcePortPath = SourcePortPath,
            Iwads = [.. _iwads],
            Mods = [.. _mods]
        };
    }

    public bool ReorderModsBySelectionSequence(IEnumerable<string> selectedModPaths)
    {
        var prioritizedSelections = new List<string>();
        var seenSelections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var selectedModPath in selectedModPaths)
        {
            if (string.IsNullOrWhiteSpace(selectedModPath))
            {
                continue;
            }

            var normalizedPath = PathNormalizer.NormalizeAbsolutePath(selectedModPath);
            if (!_modPathSet.Contains(normalizedPath))
            {
                continue;
            }

            if (seenSelections.Add(normalizedPath))
            {
                prioritizedSelections.Add(normalizedPath);
            }
        }

        var reorderedMods = new List<string>(_mods.Count);
        reorderedMods.AddRange(prioritizedSelections);

        foreach (var modPath in _mods)
        {
            if (!seenSelections.Contains(modPath))
            {
                reorderedMods.Add(modPath);
            }
        }

        if (_mods.SequenceEqual(reorderedMods, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        _mods.Clear();
        _mods.AddRange(reorderedMods);
        return true;
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

    private static bool RemoveMissingEntries(List<string> orderedPaths, HashSet<string> dedupeSet)
    {
        var changed = false;

        for (var i = orderedPaths.Count - 1; i >= 0; i--)
        {
            var path = orderedPaths[i];
            if (File.Exists(path))
            {
                continue;
            }

            orderedPaths.RemoveAt(i);
            dedupeSet.Remove(path);
            changed = true;
        }

        return changed;
    }
}
