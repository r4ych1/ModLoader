using ModLoader.App;
using ModLoader.Core;

namespace ModLoader.Core.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void CanLaunch_RequiresSelectedSourcePortAndSelectedIwad()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence();
        var launcher = new RecordingLauncher();
        var viewModel = new MainWindowViewModel(persistence, launcher);

        Assert.False(viewModel.CanLaunch);

        viewModel.ProcessSourcePortDrop([source]);
        viewModel.ProcessIwadDrop([iwad]);
        Assert.False(viewModel.CanLaunch);

        viewModel.ToggleSourcePortSelection(source);
        Assert.False(viewModel.CanLaunch);

        viewModel.ToggleIwadSelection(iwad);
        Assert.True(viewModel.CanLaunch);

        viewModel.ToggleSourcePortSelection(source);
        Assert.False(viewModel.CanLaunch);
    }

    [Fact]
    public void LaunchSourcePort_WhenReady_UsesSelectedSourcePortAndFullPathArgumentsFromSelectionOrder()
    {
        using var temp = new TempDirectory();
        var source1 = temp.CreateFile("gzdoom.exe");
        var source2 = temp.CreateFile("vkdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pk3");

        var persistence = new RecordingPersistence();
        var launcher = new RecordingLauncher();
        var viewModel = new MainWindowViewModel(persistence, launcher);

        viewModel.ProcessSourcePortDrop([source1, source2]);
        viewModel.ProcessIwadDrop([iwad]);
        viewModel.ProcessModDrop([mod1, mod2]);

        viewModel.ToggleSourcePortSelection(source2);
        viewModel.ToggleIwadSelection(iwad);
        viewModel.ToggleModSelection(mod2);
        viewModel.ToggleModSelection(mod1);

        viewModel.LaunchSourcePort();

        Assert.Equal(1, launcher.LaunchCallCount);
        Assert.Equal(Path.GetFullPath(source2), launcher.LastExecutablePath);
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
        Assert.Equal(Path.GetFullPath(source2), viewModel.SelectedSourcePortPath);
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
        viewModel.ToggleSourcePortSelection(source);
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
        viewModel.ToggleSourcePortSelection(source);
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
    public void CommandPreviewArguments_SourcePortOnly_EmitsExecutableTokenOnly()
    {
        using var temp = new TempDirectory();
        var sourcePort = temp.CreateFile("gzdoom.exe");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessSourcePortDrop([sourcePort]);
        viewModel.ToggleSourcePortSelection(sourcePort);

        Assert.Equal("gzdoom.exe", viewModel.CommandPreviewArguments);
    }

    [Fact]
    public void CommandPreviewArguments_SourcePortOnly_QuotesSourcePortFilenameWhenItContainsSpaces()
    {
        using var temp = new TempDirectory();
        var sourcePortWithSpace = temp.CreateFile("my gzdoom.exe");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessSourcePortDrop([sourcePortWithSpace]);
        viewModel.ToggleSourcePortSelection(sourcePortWithSpace);

        Assert.Equal("\"my gzdoom.exe\"", viewModel.CommandPreviewArguments);
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
    public void CommandPreviewArguments_SourcePortIwadAndMods_UsesDeterministicSegmentOrder()
    {
        using var temp = new TempDirectory();
        var sourcePort = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pk3");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessSourcePortDrop([sourcePort]);
        viewModel.ProcessIwadDrop([iwad]);
        viewModel.ProcessModDrop([mod1, mod2]);

        viewModel.ToggleSourcePortSelection(sourcePort);
        viewModel.ToggleIwadSelection(iwad);
        viewModel.ToggleModSelection(mod2);
        viewModel.ToggleModSelection(mod1);

        Assert.Equal(
            "gzdoom.exe -iwad doom2.wad -file mod-b.pk3 mod-a.pk3",
            viewModel.CommandPreviewArguments);
    }

    [Fact]
    public void ClearAllSourcePorts_ClearsSourcePortsAndSelectionOnly()
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
        viewModel.ToggleSourcePortSelection(source);

        viewModel.ClearAllSourcePorts();

        Assert.Empty(viewModel.SourcePorts);
        Assert.Null(viewModel.SelectedSourcePortPath);
        Assert.Single(viewModel.Iwads);
        Assert.Single(viewModel.Mods);
    }

    [Fact]
    public void ClearAllIwads_RemovesAllIwads_ClearsSelectedIwad_AndPersistsImmediately()
    {
        using var temp = new TempDirectory();
        var iwad1 = temp.CreateFile("doom1.wad");
        var iwad2 = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessIwadDrop([iwad1, iwad2]);
        viewModel.ProcessModDrop([mod]);
        viewModel.ToggleIwadSelection(iwad2);
        var saveCountBeforeClear = persistence.SaveCallCount;

        viewModel.ClearAllIwads();

        Assert.Empty(viewModel.Iwads);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Single(viewModel.Mods);
        Assert.False(viewModel.HasIwads);
        Assert.True(viewModel.HasMods);
        Assert.Equal(saveCountBeforeClear + 1, persistence.SaveCallCount);
        Assert.Empty(persistence.SavedStates.Last().Iwads);
        Assert.Null(persistence.SavedStates.Last().SelectedIwadPath);
    }

    [Fact]
    public void ClearAllMods_RemovesAllMods_ClearsSelectedMods_AndPersistsImmediately()
    {
        using var temp = new TempDirectory();
        var iwad = temp.CreateFile("doom2.wad");
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pk3");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessIwadDrop([iwad]);
        viewModel.ProcessModDrop([mod1, mod2]);
        viewModel.ToggleModSelection(mod2);
        viewModel.ToggleModSelection(mod1);
        var saveCountBeforeClear = persistence.SaveCallCount;

        viewModel.ClearAllMods();

        Assert.Empty(viewModel.Mods);
        Assert.Empty(viewModel.SelectedModPaths);
        Assert.Single(viewModel.Iwads);
        Assert.True(viewModel.HasIwads);
        Assert.False(viewModel.HasMods);
        Assert.Equal(saveCountBeforeClear + 1, persistence.SaveCallCount);
        Assert.Empty(persistence.SavedStates.Last().Mods);
        Assert.Empty(persistence.SavedStates.Last().SelectedModPaths);
    }

    [Fact]
    public void HasCollections_ReflectCollectionStateTransitions()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);

        Assert.False(viewModel.HasSourcePorts);
        Assert.False(viewModel.HasIwads);
        Assert.False(viewModel.HasMods);

        viewModel.ProcessSourcePortDrop([source]);
        viewModel.ProcessIwadDrop([iwad]);
        viewModel.ProcessModDrop([mod]);
        Assert.True(viewModel.HasSourcePorts);
        Assert.True(viewModel.HasIwads);
        Assert.True(viewModel.HasMods);

        viewModel.ClearAllSourcePorts();
        viewModel.ClearAllIwads();
        viewModel.ClearAllMods();
        Assert.False(viewModel.HasSourcePorts);
        Assert.False(viewModel.HasIwads);
        Assert.False(viewModel.HasMods);
    }

    [Fact]
    public void RemoveEntries_RemovesOnlyTargetedEntry()
    {
        using var temp = new TempDirectory();
        var source1 = temp.CreateFile("gzdoom.exe");
        var source2 = temp.CreateFile("vkdoom.exe");
        var iwad1 = temp.CreateFile("doom1.wad");
        var iwad2 = temp.CreateFile("doom2.iwad");
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pkz");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessSourcePortDrop([source1, source2]);
        viewModel.ProcessIwadDrop([iwad1, iwad2]);
        viewModel.ProcessModDrop([mod1, mod2]);

        viewModel.RemoveSourcePort(Path.GetFullPath(source1));
        viewModel.RemoveIwad(Path.GetFullPath(iwad1));
        viewModel.RemoveMod(Path.GetFullPath(mod2));

        Assert.Equal([Path.GetFullPath(source2)], viewModel.SourcePorts.ToArray());
        Assert.Equal([Path.GetFullPath(iwad2)], viewModel.Iwads.ToArray());
        Assert.Equal([Path.GetFullPath(mod1)], viewModel.Mods.ToArray());
    }

    [Fact]
    public void ToggleSourcePortSelection_SingleSelectWithReselectToDeselect()
    {
        using var temp = new TempDirectory();
        var source1 = temp.CreateFile("gzdoom.exe");
        var source2 = temp.CreateFile("vkdoom.exe");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessSourcePortDrop([source1, source2]);

        viewModel.ToggleSourcePortSelection(source1);
        Assert.Equal(Path.GetFullPath(source1), viewModel.SelectedSourcePortPath);
        Assert.True(viewModel.SourcePortRows.First(row => row.Path == Path.GetFullPath(source1)).IsSelected);
        Assert.Equal(Path.GetFullPath(source1), persistence.SavedStates.Last().SelectedSourcePortPath);

        viewModel.ToggleSourcePortSelection(source2);
        Assert.Equal(Path.GetFullPath(source2), viewModel.SelectedSourcePortPath);
        Assert.True(viewModel.SourcePortRows.First(row => row.Path == Path.GetFullPath(source2)).IsSelected);
        Assert.False(viewModel.SourcePortRows.First(row => row.Path == Path.GetFullPath(source1)).IsSelected);
        Assert.Equal(Path.GetFullPath(source2), persistence.SavedStates.Last().SelectedSourcePortPath);

        viewModel.ToggleSourcePortSelection(source2);
        Assert.Null(viewModel.SelectedSourcePortPath);
        Assert.DoesNotContain(viewModel.SourcePortRows, row => row.IsSelected);
        Assert.Null(persistence.SavedStates.Last().SelectedSourcePortPath);
    }

    [Fact]
    public void ProcessSourcePortDrop_SameFileNameDifferentDirectories_PreservesDistinctRowsAndSelection()
    {
        using var temp = new TempDirectory();
        var dirA = Directory.CreateDirectory(Path.Combine(temp.Path, "A"));
        var dirB = Directory.CreateDirectory(Path.Combine(temp.Path, "B"));
        var sourceA = Path.Combine(dirA.FullName, "gzdoom.exe");
        var sourceB = Path.Combine(dirB.FullName, "gzdoom.exe");
        File.WriteAllText(sourceA, string.Empty);
        File.WriteAllText(sourceB, string.Empty);

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);

        viewModel.ProcessSourcePortDrop([sourceA, sourceB]);

        Assert.Equal(
            [Path.GetFullPath(sourceA), Path.GetFullPath(sourceB)],
            viewModel.SourcePorts.ToArray());
        Assert.Equal(2, viewModel.SourcePortRows.Count);

        viewModel.ToggleSourcePortSelection(sourceA);
        Assert.Equal(Path.GetFullPath(sourceA), viewModel.SelectedSourcePortPath);
        Assert.True(viewModel.SourcePortRows.First(row => row.Path == Path.GetFullPath(sourceA)).IsSelected);
        Assert.False(viewModel.SourcePortRows.First(row => row.Path == Path.GetFullPath(sourceB)).IsSelected);

        viewModel.ToggleSourcePortSelection(sourceB);
        Assert.Equal(Path.GetFullPath(sourceB), viewModel.SelectedSourcePortPath);
        Assert.True(viewModel.SourcePortRows.First(row => row.Path == Path.GetFullPath(sourceB)).IsSelected);
        Assert.False(viewModel.SourcePortRows.First(row => row.Path == Path.GetFullPath(sourceA)).IsSelected);
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
        var source1 = temp.CreateFile("gzdoom.exe");
        var source2 = temp.CreateFile("vkdoom.exe");
        var iwad1 = temp.CreateFile("doom1.wad");
        var iwad2 = temp.CreateFile("doom2.wad");
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pk3");
        var mod3 = temp.CreateFile("mod-c.pk3");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);
        viewModel.ProcessSourcePortDrop([source1, source2]);
        viewModel.ProcessIwadDrop([iwad1, iwad2]);
        viewModel.ProcessModDrop([mod1, mod2, mod3]);

        viewModel.ToggleSourcePortSelection(source1);
        viewModel.ToggleIwadSelection(iwad1);
        viewModel.ToggleModSelection(mod1);
        viewModel.ToggleModSelection(mod2);

        viewModel.RemoveSourcePort(source1);
        viewModel.RemoveIwad(iwad1);
        viewModel.RemoveMod(mod1);

        Assert.Null(viewModel.SelectedSourcePortPath);
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
        var existingSource = temp.CreateFile("gzdoom.exe");
        var missingSource = Path.Combine(temp.Path, "missing.exe");
        var existingIwad = temp.CreateFile("doom1.wad");
        var missingIwad = Path.Combine(temp.Path, "missing.wad");
        var existingMod = temp.CreateFile("mod1.pk3");
        var missingMod = Path.Combine(temp.Path, "missing.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [existingSource, missingSource],
                    Iwads = [existingIwad, missingIwad],
                    Mods = [existingMod, missingMod],
                    SelectedSourcePortPath = missingSource,
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
        Assert.Equal([Path.GetFullPath(existingSource)], viewModel.SourcePorts.ToArray());
        Assert.Equal([Path.GetFullPath(existingIwad)], viewModel.Iwads.ToArray());
        Assert.Equal([Path.GetFullPath(existingMod)], viewModel.Mods.ToArray());
        Assert.Null(viewModel.SelectedSourcePortPath);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(existingMod)], viewModel.SelectedModPaths);
        Assert.Equal(1, persistence.SaveCallCount);
        Assert.Null(persistence.SavedStates.Last().SelectedSourcePortPath);
        Assert.Null(persistence.SavedStates.Last().SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(existingMod)], persistence.SavedStates.Last().SelectedModPaths);
    }

    [Fact]
    public void Constructor_LoadsSelectionState_AndReordersModsToSelectionSequence()
    {
        using var temp = new TempDirectory();
        var source1 = temp.CreateFile("gzdoom.exe");
        var source2 = temp.CreateFile("vkdoom.exe");
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
                    SourcePorts = [source1, source2],
                    SelectedSourcePortPath = source2,
                    Iwads = [iwad1, iwad2],
                    Mods = [mod1, mod2],
                    SelectedIwadPath = iwad2,
                    SelectedModPaths = [mod2, mod1]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        Assert.Equal(Path.GetFullPath(source2), viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(iwad2), viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(mod2), Path.GetFullPath(mod1)], viewModel.SelectedModPaths);
        Assert.Equal([Path.GetFullPath(mod2), Path.GetFullPath(mod1)], viewModel.Mods.ToArray());
        Assert.True(viewModel.SourcePortRows.First(row => row.Path == Path.GetFullPath(source2)).IsSelected);
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
        viewModel.ClearAllSourcePorts();

        Assert.Equal(6, persistence.SaveCallCount);
        var lastSaved = persistence.SavedStates.Last();
        Assert.Empty(lastSaved.SourcePorts);
        Assert.Null(lastSaved.SelectedSourcePortPath);
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
            SourcePorts = [.. config.SourcePorts],
            SelectedSourcePortPath = config.SelectedSourcePortPath,
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
