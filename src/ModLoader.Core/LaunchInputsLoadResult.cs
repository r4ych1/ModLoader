namespace ModLoader.Core;

public sealed class LaunchInputsLoadResult
{
    public required LaunchInputsConfig State { get; init; }

    public bool HadLoadWarning { get; init; }

    public string? WarningMessage { get; init; }

    public bool HadRemediationAction { get; init; }
}
