using ModLoader.App;
using ModLoader.Core;

namespace ModLoader.Core.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void CanLaunch_RequiresSourcePortAndSelectedIwad()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence();
        var launcher = new RecordingLauncher();
        var viewModel = new MainWindowViewModel(persistence, launcher);

        Assert.False(viewModel.CanLaunch);

        viewModel.ProcessSourcePortDrop([source]);
        Assert.False(viewModel.CanLaunch);

        viewModel.ProcessIwadDrop([iwad]);
        Assert.False(viewModel.CanLaunch);

        viewModel.ToggleIwadSelection(iwad);
        Assert.True(viewModel.CanLaunch);

        viewModel.ClearSourcePort();
        Assert.False(viewModel.CanLaunch);
    }

    [Fact]
    public void LaunchSourcePort_WhenReady_UsesFullPathArgumentsFromSelectionOrder()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pk3");

        var persistence = new RecordingPersistence();
        var launcher = new RecordingLauncher();
        var viewModel = new MainWindowViewModel(persistence, launcher);

        viewModel.ProcessSourcePortDrop([source]);
        viewModel.ProcessIwadDrop([iwad]);
        viewModel.ProcessModDrop([mod1, mod2]);

        viewModel.ToggleIwadSelection(iwad);
        viewModel.ToggleModSelection(mod2);
        viewModel.ToggleModSelection(mod1);

        viewModel.LaunchSourcePort();

        Assert.Equal(1, launcher.LaunchCallCount);
        Assert.Equal(Path.GetFullPath(source), launcher.LastExecutablePath);
        Assert.Equal(
            [
                "-iwad",
                Path.GetFullPath(iwad),
                "-file",
                Path.GetFullPath(mod2),
                Path.GetFullPath(mod1)
            ],
            launcher.LastArguments);

        Assert.Equal([Path.GetFullPath(mod2), Path.GetFullPath(mod1)], viewModel.SelectedModPaths);
        Assert.Equal(Path.GetFullPath(iwad), viewModel.SelectedIwadPath);
    }

    [Fact]
    public void LaunchSourcePort_WhenNoSelectedMods_UsesOnlyIwadSegment()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence();
        var launcher = new RecordingLauncher();
        var viewModel = new MainWindowViewModel(persistence, launcher);

        viewModel.ProcessSourcePortDrop([source]);
        viewModel.ProcessIwadDrop([iwad]);
        viewModel.ToggleIwadSelection(iwad);
        viewModel.LaunchSourcePort();

        Assert.Equal(1, launcher.LaunchCallCount);
        Assert.Equal(
            ["-iwad", Path.GetFullPath(iwad)],
            launcher.LastArguments);
    }

    [Fact]
    public void LaunchSourcePort_WhenNotReady_DoesNotInvokeLauncher()
    {
        var persistence = new RecordingPersistence();
        var launcher = new RecordingLauncher();
        var viewModel = new MainWindowViewModel(persistence, launcher);

        viewModel.LaunchSourcePort();

        Assert.Equal(0, launcher.LaunchCallCount);
    }

    [Fact]
    public void LaunchSourcePort_WhenLauncherThrows_SetsNonBlockingWarningMessage()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence();
        var launcher = new RecordingLauncher
        {
            ExceptionToThrow = new InvalidOperationException("boom")
        };
        var viewModel = new MainWindowViewModel(persistence, launcher);

        viewModel.ProcessSourcePortDrop([source]);
        viewModel.ProcessIwadDrop([iwad]);
        viewModel.ToggleIwadSelection(iwad);

        viewModel.LaunchSourcePort();

        Assert.Equal(1, launcher.LaunchCallCount);
        Assert.True(viewModel.HasStartupWarning);
        Assert.Equal("Launch failed: boom", viewModel.StartupWarningMessage);
    }

    [Fact]
    public void CommandPreviewArguments_NoSelections_IsEmpty()
    {
        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);

        Assert.Equal(string.Empty, viewModel.CommandPreviewArguments);
    }

    [Fact]
    public void CommandPreviewArguments_IwadOnly_EmitsIwadSegmentOnly()
    {
        using var temp = new TempDirectory();
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessIwadDrop([iwad]);
        viewModel.ToggleIwadSelection(iwad);

        Assert.Equal("-iwad doom2.wad", viewModel.CommandPreviewArguments);
    }

    [Fact]
    public void CommandPreviewArguments_ModOnly_EmitsFileSegmentOnly_AndQuotesSpaces()
    {
        using var temp = new TempDirectory();
        var modWithSpace = temp.CreateFile("my mod.pk3");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessModDrop([modWithSpace]);
        viewModel.ToggleModSelection(modWithSpace);

        Assert.Equal("-file \"my mod.pk3\"", viewModel.CommandPreviewArguments);
    }

    [Fact]
    public void CommandPreviewArguments_IwadAndMods_UsesDeterministicSegmentOrder()
    {
        using var temp = new TempDirectory();
        var iwad = temp.CreateFile("doom2.wad");
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pk3");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessIwadDrop([iwad]);
        viewModel.ProcessModDrop([mod1, mod2]);

        viewModel.ToggleIwadSelection(iwad);
        viewModel.ToggleModSelection(mod2);
        viewModel.ToggleModSelection(mod1);

        Assert.Equal(
            "-iwad doom2.wad -file mod-b.pk3 mod-a.pk3",
            viewModel.CommandPreviewArguments);
    }

    [Fact]
    public void ClearSourcePort_ClearsOnlySourcePort()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("smoothdoom.pk3");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessSourcePortDrop([source]);
        viewModel.ProcessIwadDrop([iwad]);
        viewModel.ProcessModDrop([mod]);

        viewModel.ClearSourcePort();

        Assert.Null(viewModel.SourcePortPath);
        Assert.Single(viewModel.Iwads);
        Assert.Single(viewModel.Mods);
    }

    [Fact]
    public void RemoveEntries_RemovesOnlyTargetedEntry()
    {
        using var temp = new TempDirectory();
        var iwad1 = temp.CreateFile("doom1.wad");
        var iwad2 = temp.CreateFile("doom2.iwad");
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pkz");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessIwadDrop([iwad1, iwad2]);
        viewModel.ProcessModDrop([mod1, mod2]);

        viewModel.RemoveIwad(Path.GetFullPath(iwad1));
        viewModel.RemoveMod(Path.GetFullPath(mod2));

        Assert.Equal([Path.GetFullPath(iwad2)], viewModel.Iwads.ToArray());
        Assert.Equal([Path.GetFullPath(mod1)], viewModel.Mods.ToArray());
    }

    [Fact]
    public void ToggleIwadSelection_SingleSelectWithReselectToDeselect()
    {
        using var temp = new TempDirectory();
        var iwad1 = temp.CreateFile("doom1.wad");
        var iwad2 = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessIwadDrop([iwad1, iwad2]);

        viewModel.ToggleIwadSelection(iwad1);
        Assert.Equal(Path.GetFullPath(iwad1), viewModel.SelectedIwadPath);
        Assert.True(viewModel.IwadRows.First(row => row.Path == Path.GetFullPath(iwad1)).IsSelected);
        Assert.Equal(Path.GetFullPath(iwad1), persistence.SavedStates.Last().SelectedIwadPath);

        viewModel.ToggleIwadSelection(iwad2);
        Assert.Equal(Path.GetFullPath(iwad2), viewModel.SelectedIwadPath);
        Assert.True(viewModel.IwadRows.First(row => row.Path == Path.GetFullPath(iwad2)).IsSelected);
        Assert.False(viewModel.IwadRows.First(row => row.Path == Path.GetFullPath(iwad1)).IsSelected);
        Assert.Equal(Path.GetFullPath(iwad2), persistence.SavedStates.Last().SelectedIwadPath);

        viewModel.ToggleIwadSelection(iwad2);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.DoesNotContain(viewModel.IwadRows, row => row.IsSelected);
        Assert.Null(persistence.SavedStates.Last().SelectedIwadPath);
    }

    [Fact]
    public void ToggleModSelection_MultiSelectAndReselectToDeselectSingleRow()
    {
        using var temp = new TempDirectory();
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pk3");
        var mod3 = temp.CreateFile("mod-c.pk3");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessModDrop([mod1, mod2, mod3]);

        viewModel.ToggleModSelection(mod1);
        viewModel.ToggleModSelection(mod2);

        Assert.Equal(2, viewModel.SelectedModPaths.Count);
        Assert.Contains(Path.GetFullPath(mod1), viewModel.SelectedModPaths, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(Path.GetFullPath(mod2), viewModel.SelectedModPaths, StringComparer.OrdinalIgnoreCase);
        Assert.True(viewModel.ModRows.First(row => row.Path == Path.GetFullPath(mod1)).IsSelected);
        Assert.True(viewModel.ModRows.First(row => row.Path == Path.GetFullPath(mod2)).IsSelected);
        Assert.Equal(
            [Path.GetFullPath(mod1), Path.GetFullPath(mod2)],
            persistence.SavedStates.Last().SelectedModPaths);

        viewModel.ToggleModSelection(mod1);

        Assert.Single(viewModel.SelectedModPaths);
        Assert.DoesNotContain(Path.GetFullPath(mod1), viewModel.SelectedModPaths, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(Path.GetFullPath(mod2), viewModel.SelectedModPaths, StringComparer.OrdinalIgnoreCase);
        Assert.False(viewModel.ModRows.First(row => row.Path == Path.GetFullPath(mod1)).IsSelected);
        Assert.True(viewModel.ModRows.First(row => row.Path == Path.GetFullPath(mod2)).IsSelected);
        Assert.Equal([Path.GetFullPath(mod2)], persistence.SavedStates.Last().SelectedModPaths);
    }

    [Fact]
    public void ToggleModSelection_ReordersModRowsToSelectionSequence()
    {
        using var temp = new TempDirectory();
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pk3");
        var mod3 = temp.CreateFile("mod-c.pk3");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessModDrop([mod1, mod2, mod3]);

        viewModel.ToggleModSelection(mod3);
        Assert.Equal(
            [Path.GetFullPath(mod3), Path.GetFullPath(mod1), Path.GetFullPath(mod2)],
            viewModel.Mods.ToArray());

        viewModel.ToggleModSelection(mod1);
        Assert.Equal(
            [Path.GetFullPath(mod3), Path.GetFullPath(mod1), Path.GetFullPath(mod2)],
            viewModel.Mods.ToArray());

        viewModel.ToggleModSelection(mod3);
        Assert.Equal(
            [Path.GetFullPath(mod1), Path.GetFullPath(mod3), Path.GetFullPath(mod2)],
            viewModel.Mods.ToArray());
    }

    [Fact]
    public void ToggleModSelection_PersistsReorderedModsImmediately()
    {
        using var temp = new TempDirectory();
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pk3");
        var mod3 = temp.CreateFile("mod-c.pk3");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessModDrop([mod1, mod2, mod3]);

        viewModel.ToggleModSelection(mod2);
        viewModel.ToggleModSelection(mod3);

        Assert.Equal(
            [Path.GetFullPath(mod2), Path.GetFullPath(mod3), Path.GetFullPath(mod1)],
            persistence.SavedStates.Last().Mods);
    }

    [Fact]
    public void RemoveSelectedRows_UpdatesSelectionsWithoutChangingUnrelatedSelections()
    {
        using var temp = new TempDirectory();
        var iwad1 = temp.CreateFile("doom1.wad");
        var iwad2 = temp.CreateFile("doom2.wad");
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pk3");
        var mod3 = temp.CreateFile("mod-c.pk3");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessIwadDrop([iwad1, iwad2]);
        viewModel.ProcessModDrop([mod1, mod2, mod3]);

        viewModel.ToggleIwadSelection(iwad1);
        viewModel.ToggleModSelection(mod1);
        viewModel.ToggleModSelection(mod2);

        viewModel.RemoveIwad(iwad1);
        viewModel.RemoveMod(mod1);

        Assert.Null(viewModel.SelectedIwadPath);
        Assert.DoesNotContain(Path.GetFullPath(mod1), viewModel.SelectedModPaths, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(Path.GetFullPath(mod2), viewModel.SelectedModPaths, StringComparer.OrdinalIgnoreCase);

        viewModel.RemoveMod(mod3);
        Assert.Contains(Path.GetFullPath(mod2), viewModel.SelectedModPaths, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_LoadsState_ShowsStartupWarning_AndSanitizesMissingPaths()
    {
        using var temp = new TempDirectory();
        var existingIwad = temp.CreateFile("doom1.wad");
        var missingIwad = Path.Combine(temp.Path, "missing.wad");
        var existingMod = temp.CreateFile("mod1.pk3");
        var missingMod = Path.Combine(temp.Path, "missing.pk3");
        var missingSource = Path.Combine(temp.Path, "missing.exe");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePortPath = missingSource,
                    Iwads = [existingIwad, missingIwad],
                    Mods = [existingMod, missingMod],
                    SelectedIwadPath = missingIwad,
                    SelectedModPaths = [missingMod, existingMod]
                },
                HadLoadWarning = true,
                WarningMessage = "Config was invalid and was reset."
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        Assert.True(viewModel.HasStartupWarning);
        Assert.Equal("Config was invalid and was reset.", viewModel.StartupWarningMessage);
        Assert.Null(viewModel.SourcePortPath);
        Assert.Equal([Path.GetFullPath(existingIwad)], viewModel.Iwads.ToArray());
        Assert.Equal([Path.GetFullPath(existingMod)], viewModel.Mods.ToArray());
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(existingMod)], viewModel.SelectedModPaths);
        Assert.Equal(1, persistence.SaveCallCount);
        Assert.Null(persistence.SavedStates.Last().SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(existingMod)], persistence.SavedStates.Last().SelectedModPaths);
    }

    [Fact]
    public void Constructor_LoadsSelectionState_AndReordersModsToSelectionSequence()
    {
        using var temp = new TempDirectory();
        var iwad1 = temp.CreateFile("doom1.wad");
        var iwad2 = temp.CreateFile("doom2.wad");
        var mod1 = temp.CreateFile("mod1.pk3");
        var mod2 = temp.CreateFile("mod2.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    Iwads = [iwad1, iwad2],
                    Mods = [mod1, mod2],
                    SelectedIwadPath = iwad2,
                    SelectedModPaths = [mod2, mod1]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        Assert.Equal(Path.GetFullPath(iwad2), viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(mod2), Path.GetFullPath(mod1)], viewModel.SelectedModPaths);
        Assert.Equal([Path.GetFullPath(mod2), Path.GetFullPath(mod1)], viewModel.Mods.ToArray());
        Assert.True(viewModel.IwadRows.First(row => row.Path == Path.GetFullPath(iwad2)).IsSelected);
        Assert.True(viewModel.ModRows.First(row => row.Path == Path.GetFullPath(mod2)).IsSelected);
        Assert.True(viewModel.ModRows.First(row => row.Path == Path.GetFullPath(mod1)).IsSelected);
        Assert.Equal(1, persistence.SaveCallCount);
        Assert.Equal([Path.GetFullPath(mod2), Path.GetFullPath(mod1)], persistence.SavedStates.Last().Mods);
    }

    [Fact]
    public void Constructor_DoesNotPersistWhenPersistedModOrderAlreadyMatchesSelectionSequence()
    {
        using var temp = new TempDirectory();
        var mod1 = temp.CreateFile("mod1.pk3");
        var mod2 = temp.CreateFile("mod2.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    Mods = [mod2, mod1],
                    SelectedModPaths = [mod2, mod1]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        Assert.Equal([Path.GetFullPath(mod2), Path.GetFullPath(mod1)], viewModel.Mods.ToArray());
        Assert.Equal(0, persistence.SaveCallCount);
    }

    [Fact]
    public void Mutations_PersistImmediately()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom.wad");
        var mod = temp.CreateFile("mod.pk3");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);

        viewModel.ProcessSourcePortDrop([source]);
        viewModel.ProcessIwadDrop([iwad]);
        viewModel.ProcessModDrop([mod]);
        viewModel.RemoveIwad(iwad);
        viewModel.RemoveMod(mod);
        viewModel.ClearSourcePort();

        Assert.Equal(6, persistence.SaveCallCount);
        var lastSaved = persistence.SavedStates.Last();
        Assert.Null(lastSaved.SourcePortPath);
        Assert.Empty(lastSaved.Iwads);
        Assert.Empty(lastSaved.Mods);
        Assert.Null(lastSaved.SelectedIwadPath);
        Assert.Empty(lastSaved.SelectedModPaths);
    }
}

internal sealed class RecordingPersistence : ILaunchInputsPersistence
{
    public LaunchInputsLoadResult LoadResult { get; set; } = new()
    {
        State = LaunchInputsConfig.Empty
    };

    public int SaveCallCount { get; private set; }

    public List<LaunchInputsConfig> SavedStates { get; } = [];

    public LaunchInputsLoadResult Load()
    {
        return LoadResult;
    }

    public void Save(LaunchInputsConfig config)
    {
        SaveCallCount++;
        SavedStates.Add(new LaunchInputsConfig
        {
            SourcePortPath = config.SourcePortPath,
            Iwads = [.. config.Iwads],
            Mods = [.. config.Mods],
            SelectedIwadPath = config.SelectedIwadPath,
            SelectedModPaths = [.. config.SelectedModPaths]
        });
    }
}

internal sealed class RecordingLauncher : ISourcePortLauncher
{
    public Exception? ExceptionToThrow { get; set; }

    public int LaunchCallCount { get; private set; }

    public string? LastExecutablePath { get; private set; }

    public IReadOnlyList<string> LastArguments { get; private set; } = [];

    public void Launch(string executablePath, IReadOnlyList<string> arguments)
    {
        LaunchCallCount++;

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        LastExecutablePath = executablePath;
        LastArguments = [.. arguments];
    }
}
