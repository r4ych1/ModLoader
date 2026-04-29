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
        Assert.Null(result.State.SourcePortPath);
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
            SourcePortPath = Path.Combine(temp.Path, "gzdoom.exe"),
            Iwads = [Path.Combine(temp.Path, "doom.wad"), Path.Combine(temp.Path, "doom2.wad")],
            Mods = [Path.Combine(temp.Path, "mod-a.pk3"), Path.Combine(temp.Path, "mod-b.pk3")],
            SelectedIwadPath = Path.Combine(temp.Path, "doom2.wad"),
            SelectedModPaths = [Path.Combine(temp.Path, "mod-b.pk3"), Path.Combine(temp.Path, "mod-a.pk3")]
        };

        persistence.Save(state);
        var result = persistence.Load();

        Assert.Equal(state.SourcePortPath, result.State.SourcePortPath);
        Assert.Equal(state.Iwads, result.State.Iwads);
        Assert.Equal(state.Mods, result.State.Mods);
        Assert.Equal(state.SelectedIwadPath, result.State.SelectedIwadPath);
        Assert.Equal(state.SelectedModPaths, result.State.SelectedModPaths);
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
        Assert.Null(result.State.SourcePortPath);
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
        Assert.Null(replacementState.SourcePortPath);
        Assert.Empty(replacementState.Iwads);
        Assert.Empty(replacementState.Mods);
        Assert.Null(replacementState.SelectedIwadPath);
        Assert.Empty(replacementState.SelectedModPaths);
    }
}
