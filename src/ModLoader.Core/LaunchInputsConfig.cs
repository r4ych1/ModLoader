using System.Collections.Generic;

namespace ModLoader.Core;

public sealed class LaunchInputsConfig
{
    public string? SourcePortPath { get; init; }

    public List<string> Iwads { get; init; } = [];

    public List<string> Mods { get; init; } = [];

    public string? SelectedIwadPath { get; init; }

    public List<string> SelectedModPaths { get; init; } = [];

    public static LaunchInputsConfig Empty { get; } = new();
}
