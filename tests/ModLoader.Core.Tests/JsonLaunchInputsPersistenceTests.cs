using System.Text.Json;
using ModLoader.Core;

namespace ModLoader.Core.Tests;

public sealed class JsonLaunchInputsPersistenceTests
{
    [Fact]
    public void Load_MissingConfigFile_ReturnsEmptyStateWithoutWarning()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "modloader.config.json");
        var persistence = new JsonLaunchInputsPersistence(configPath);

        var result = persistence.Load();

        Assert.False(result.HadLoadWarning);
        Assert.False(result.HadRemediationAction);
        Assert.Null(result.WarningMessage);
        Assert.Empty(result.State.SourcePorts);
        Assert.Null(result.State.SelectedSourcePortPath);
        Assert.Empty(result.State.Iwads);
        Assert.Empty(result.State.Mods);
        Assert.Null(result.State.SelectedIwadPath);
        Assert.Empty(result.State.SelectedModPaths);
    }

    [Fact]
    public void Save_ThenLoad_PreservesStateAndOrdering()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "modloader.config.json");
        var persistence = new JsonLaunchInputsPersistence(configPath);

        var state = new LaunchInputsConfig
        {
            SourcePorts = [Path.Combine(temp.Path, "gzdoom.exe"), Path.Combine(temp.Path, "vkdoom.exe")],
            SelectedSourcePortPath = Path.Combine(temp.Path, "vkdoom.exe"),
            Iwads = [Path.Combine(temp.Path, "doom.wad"), Path.Combine(temp.Path, "doom2.wad")],
            Mods = [Path.Combine(temp.Path, "mod-a.pk3"), Path.Combine(temp.Path, "mod-b.pk3")],
            SelectedIwadPath = Path.Combine(temp.Path, "doom2.wad"),
            SelectedModPaths = [Path.Combine(temp.Path, "mod-b.pk3"), Path.Combine(temp.Path, "mod-a.pk3")]
        };

        persistence.Save(state);
        var result = persistence.Load();

        Assert.Equal(state.SourcePorts, result.State.SourcePorts);
        Assert.Equal(state.SelectedSourcePortPath, result.State.SelectedSourcePortPath);
        Assert.Equal(state.Iwads, result.State.Iwads);
        Assert.Equal(state.Mods, result.State.Mods);
        Assert.Equal(state.SelectedIwadPath, result.State.SelectedIwadPath);
        Assert.Equal(state.SelectedModPaths, result.State.SelectedModPaths);
    }

    [Fact]
    public void Load_LegacySourcePortPath_MigratesToSourcePortsAndSelectedSourcePort()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "modloader.config.json");
        var legacySourcePort = Path.Combine(temp.Path, "gzdoom.exe");

        var legacyConfigJson = JsonSerializer.Serialize(new
        {
            SourcePortPath = legacySourcePort,
            Iwads = Array.Empty<string>(),
            Mods = Array.Empty<string>(),
            SelectedIwadPath = (string?)null,
            SelectedModPaths = Array.Empty<string>()
        });
        File.WriteAllText(configPath, legacyConfigJson);

        var persistence = new JsonLaunchInputsPersistence(configPath);
        var result = persistence.Load();

        Assert.Equal([legacySourcePort], result.State.SourcePorts);
        Assert.Equal(legacySourcePort, result.State.SelectedSourcePortPath);
    }

    [Fact]
    public void Load_InvalidJson_RenamesBrokenFile_AndWritesFreshEmptyConfig()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "modloader.config.json");
        File.WriteAllText(configPath, "{ this is not valid json");

        var persistence = new JsonLaunchInputsPersistence(configPath);
        var result = persistence.Load();

        Assert.True(result.HadLoadWarning);
        Assert.True(result.HadRemediationAction);
        Assert.NotNull(result.WarningMessage);
        Assert.Empty(result.State.SourcePorts);
        Assert.Null(result.State.SelectedSourcePortPath);
        Assert.Empty(result.State.Iwads);
        Assert.Empty(result.State.Mods);
        Assert.Null(result.State.SelectedIwadPath);
        Assert.Empty(result.State.SelectedModPaths);

        var backupPath = Directory.EnumerateFiles(temp.Path, "modloader.config.json.broken.*").Single();
        Assert.True(File.Exists(backupPath));
        Assert.True(File.Exists(configPath));

        var replacementJson = File.ReadAllText(configPath);
        var replacementState = JsonSerializer.Deserialize<LaunchInputsConfig>(replacementJson);
        Assert.NotNull(replacementState);
        Assert.Empty(replacementState.SourcePorts);
        Assert.Null(replacementState.SelectedSourcePortPath);
        Assert.Empty(replacementState.Iwads);
        Assert.Empty(replacementState.Mods);
        Assert.Null(replacementState.SelectedIwadPath);
        Assert.Empty(replacementState.SelectedModPaths);
    }
}
