using System.Collections.Generic;

namespace ModLoader.Core;

public sealed class ProfileConfig
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? SourcePortPath { get; init; }

    public string? IwadPath { get; init; }

    public List<string> SelectedModPaths { get; init; } = [];
}
