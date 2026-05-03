using System.Linq;
using ModLoader.App;
using ModLoader.Core;

namespace ModLoader.Core.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void CreateNewProfile_UsesFirstAvailableDefaultNameAndSelectsNewProfile()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles =
                    [
                        CreateProfile("p1", "Profile 1", source, iwad),
                        CreateProfile("p3", "Profile 3", source, iwad)
                    ],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.CreateNewProfile();

        Assert.Equal("Profile 2", viewModel.SelectedProfileName);
        Assert.Equal(3, viewModel.ProfileRows.Count);
        Assert.Contains(viewModel.ProfileRows, row => row.Name == "Profile 2");
        Assert.Equal(3, persistence.SavedStates.Last().Profiles.Count);
        Assert.Equal("Profile 2", persistence.SavedStates.Last().Profiles.Last().Name);
        Assert.Equal(persistence.SavedStates.Last().Profiles.Last().Id, persistence.SavedStates.Last().SelectedProfileId);
    }

    [Fact]
    public void ToggleProfileSelection_SelectsAndUnselectsHydratingAndClearingSelections()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Mods = [mod],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad, mod)]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.ToggleProfileSelection("p1");

        Assert.Equal("Profile 1", viewModel.SelectedProfileName);
        Assert.Equal(Path.GetFullPath(source), viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(iwad), viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(mod)], viewModel.SelectedModPaths);
        Assert.True(viewModel.CanLaunch);

        viewModel.ToggleProfileSelection("p1");

        Assert.Equal("No Profile Selected", viewModel.SelectedProfileName);
        Assert.Null(viewModel.SelectedSourcePortPath);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Empty(viewModel.SelectedModPaths);
        Assert.False(viewModel.CanLaunch);
        Assert.Null(persistence.SavedStates.Last().SelectedProfileId);
        Assert.Null(persistence.SavedStates.Last().SelectedSourcePortPath);
        Assert.Null(persistence.SavedStates.Last().SelectedIwadPath);
        Assert.Empty(persistence.SavedStates.Last().SelectedModPaths);
    }

    [Fact]
    public void CanCreateProfile_RequiresCurrentSelectedSourcePortAndIwad()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);

        Assert.False(viewModel.CanCreateProfile);

        viewModel.ProcessSourcePortDrop([source]);
        viewModel.ProcessIwadDrop([iwad]);
        Assert.False(viewModel.CanCreateProfile);

        viewModel.ToggleSourcePortSelection(source);
        Assert.False(viewModel.CanCreateProfile);

        viewModel.ToggleIwadSelection(iwad);
        Assert.True(viewModel.CanCreateProfile);
    }

    [Fact]
    public void ToggleSectionCollapse_PersistsStateAndUpdatesVisibility()
    {
        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);

        Assert.True(viewModel.AreSourcePortRowsVisible);
        Assert.Equal("Collapse", viewModel.SourcePortSectionToggleText);
        Assert.True(viewModel.AreIwadRowsVisible);
        Assert.Equal("Collapse", viewModel.IwadSectionToggleText);
        Assert.True(viewModel.AreModRowsVisible);
        Assert.Equal("Collapse", viewModel.ModSectionToggleText);

        viewModel.ToggleSourcePortSectionCollapsed();
        viewModel.ToggleIwadSectionCollapsed();
        viewModel.ToggleModSectionCollapsed();

        Assert.True(viewModel.IsSourcePortSectionCollapsed);
        Assert.False(viewModel.AreSourcePortRowsVisible);
        Assert.Equal("Expand", viewModel.SourcePortSectionToggleText);
        Assert.True(viewModel.IsIwadSectionCollapsed);
        Assert.False(viewModel.AreIwadRowsVisible);
        Assert.Equal("Expand", viewModel.IwadSectionToggleText);
        Assert.True(viewModel.IsModSectionCollapsed);
        Assert.False(viewModel.AreModRowsVisible);
        Assert.Equal("Expand", viewModel.ModSectionToggleText);

        Assert.True(persistence.SavedStates.Last().IsSourcePortSectionCollapsed);
        Assert.True(persistence.SavedStates.Last().IsIwadSectionCollapsed);
        Assert.True(persistence.SavedStates.Last().IsModSectionCollapsed);
    }

    [Fact]
    public void Constructor_WithPersistedCollapseState_RestoresSectionVisibility()
    {
        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    IsSourcePortSectionCollapsed = true,
                    IsIwadSectionCollapsed = false,
                    IsModSectionCollapsed = true
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        Assert.True(viewModel.IsSourcePortSectionCollapsed);
        Assert.False(viewModel.AreSourcePortRowsVisible);
        Assert.Equal("Expand", viewModel.SourcePortSectionToggleText);
        Assert.False(viewModel.IsIwadSectionCollapsed);
        Assert.True(viewModel.AreIwadRowsVisible);
        Assert.Equal("Collapse", viewModel.IwadSectionToggleText);
        Assert.True(viewModel.IsModSectionCollapsed);
        Assert.False(viewModel.AreModRowsVisible);
        Assert.Equal("Expand", viewModel.ModSectionToggleText);
    }

    [Fact]
    public void ToggleSelections_WhenProfileSelected_AutoSavesProfileAndCanBecomeInvalid()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.ToggleIwadSelection(iwad);

        Assert.False(viewModel.CanLaunch);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Null(persistence.SavedStates.Last().Profiles.Single().IwadPath);
        Assert.True(viewModel.ProfileRows.Single().IsInvalid);
    }

    [Fact]
    public void DetachedSelections_DoNotPersistAsCanonicalSelectionState()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence();
        var viewModel = new MainWindowViewModel(persistence);

        viewModel.ProcessSourcePortDrop([source]);
        viewModel.ProcessIwadDrop([iwad]);
        viewModel.ProcessModDrop([mod]);
        viewModel.ToggleSourcePortSelection(source);
        viewModel.ToggleIwadSelection(iwad);
        viewModel.ToggleModSelection(mod);

        Assert.Equal("gzdoom.exe -iwad doom2.wad -file mod-a.pk3", viewModel.CommandPreviewArguments);
        Assert.Null(persistence.SavedStates.Last().SelectedProfileId);
        Assert.Null(persistence.SavedStates.Last().SelectedSourcePortPath);
        Assert.Null(persistence.SavedStates.Last().SelectedIwadPath);
        Assert.Empty(persistence.SavedStates.Last().SelectedModPaths);
    }

    [Fact]
    public void DeleteSelectedProfile_ClearsSelectionAndCurrentSelections()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Mods = [mod],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad, mod)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.RequestDeleteProfile("p1");
        Assert.True(viewModel.HasPendingDeleteConfirmation);

        viewModel.ConfirmDeleteProfile();

        Assert.False(viewModel.HasProfiles);
        Assert.Equal("No Profile Selected", viewModel.SelectedProfileName);
        Assert.Null(viewModel.SelectedSourcePortPath);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Empty(viewModel.SelectedModPaths);
        Assert.False(viewModel.CanLaunch);
        Assert.Empty(persistence.SavedStates.Last().Profiles);
        Assert.Null(persistence.SavedStates.Last().SelectedProfileId);
    }

    [Fact]
    public void RemoveLibraryItemUsedBySelectedProfile_KeepsProfileSavedButInvalid()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.RemoveSourcePort(source);

        var profileRow = viewModel.ProfileRows.Single();
        Assert.True(profileRow.IsInvalid);
        Assert.Contains("Source Port", profileRow.InvalidReason);
        Assert.False(viewModel.CanLaunch);
        Assert.Null(viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(source), persistence.SavedStates.Last().Profiles.Single().SourcePortPath);
    }

    [Fact]
    public void BeginRenameProfile_CommitRename_PersistsNewUniqueName()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles = [CreateProfile("p1", "Profile 1", source, iwad)]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.BeginRenameProfile("p1");
        var row = viewModel.ProfileRows.Single();
        row.RenameText = "Ultra-Violence";

        viewModel.CommitRename("p1");

        Assert.False(row.IsRenaming);
        Assert.Equal("Ultra-Violence", row.Name);
        Assert.Equal("Ultra-Violence", persistence.SavedStates.Last().Profiles.Single().Name);
        Assert.False(viewModel.HasMessage);
    }

    [Fact]
    public void BeginRenameProfile_SelectsProfileAndHydratesSelections()
    {
        using var temp = new TempDirectory();
        var source1 = temp.CreateFile("gzdoom.exe");
        var source2 = temp.CreateFile("vkdoom.exe");
        var iwad1 = temp.CreateFile("doom.wad");
        var iwad2 = temp.CreateFile("doom2.wad");
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source1, source2],
                    Iwads = [iwad1, iwad2],
                    Mods = [mod1, mod2],
                    Profiles =
                    [
                        CreateProfile("p1", "Profile 1", source1, iwad1, mod1),
                        CreateProfile("p2", "Profile 2", source2, iwad2, mod2)
                    ]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.BeginRenameProfile("p2");

        var row = viewModel.ProfileRows.Single(candidate => candidate.Id == "p2");
        Assert.True(row.IsRenaming);
        Assert.Equal("Profile 2", viewModel.SelectedProfileName);
        Assert.Equal(Path.GetFullPath(source2), viewModel.SelectedSourcePortPath);
        Assert.Equal(Path.GetFullPath(iwad2), viewModel.SelectedIwadPath);
        Assert.Equal([Path.GetFullPath(mod2)], viewModel.SelectedModPaths);
        Assert.Equal("p2", persistence.SavedStates.Last().SelectedProfileId);
    }

    [Fact]
    public void CommitRename_DuplicateName_ShowsValidationAndKeepsRenameOpen()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    Iwads = [iwad],
                    Profiles =
                    [
                        CreateProfile("p1", "Profile 1", source, iwad),
                        CreateProfile("p2", "Profile 2", source, iwad)
                    ]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        viewModel.BeginRenameProfile("p1");
        var row = viewModel.ProfileRows.First(candidate => candidate.Id == "p1");
        row.RenameText = "profile 2";

        viewModel.CommitRename("p1");

        Assert.True(row.IsRenaming);
        Assert.Equal("Profile 1", row.Name);
        Assert.Equal("Profile name must be unique.", viewModel.MessageText);
    }

    [Fact]
    public void LaunchSourcePort_WhenSelectedProfileValid_UsesProfileArguments()
    {
        using var temp = new TempDirectory();
        var source1 = temp.CreateFile("gzdoom.exe");
        var source2 = temp.CreateFile("vkdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source1, source2],
                    Iwads = [iwad],
                    Mods = [mod1, mod2],
                    Profiles = [CreateProfile("p1", "Profile 1", source2, iwad, mod2, mod1)],
                    SelectedProfileId = "p1"
                }
            }
        };

        var launcher = new RecordingLauncher();
        var viewModel = new MainWindowViewModel(persistence, launcher);

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
    }

    [Fact]
    public void Constructor_WithLegacySelectionStateAndNoProfiles_StartsDetachedAndDisabled()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("mod-a.pk3");

        var persistence = new RecordingPersistence
        {
            LoadResult = new LaunchInputsLoadResult
            {
                State = new LaunchInputsConfig
                {
                    SourcePorts = [source],
                    SelectedSourcePortPath = source,
                    Iwads = [iwad],
                    Mods = [mod],
                    SelectedIwadPath = iwad,
                    SelectedModPaths = [mod]
                }
            }
        };

        var viewModel = new MainWindowViewModel(persistence);

        Assert.False(viewModel.HasProfiles);
        Assert.False(viewModel.HasSelectedProfile);
        Assert.Null(viewModel.SelectedSourcePortPath);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.Empty(viewModel.SelectedModPaths);
        Assert.False(viewModel.CanLaunch);
        Assert.Equal(string.Empty, viewModel.CommandPreviewArguments);
    }

    private static ProfileConfig CreateProfile(string id, string name, string? sourcePortPath, string? iwadPath, params string[] selectedModPaths)
    {
        return new ProfileConfig
        {
            Id = id,
            Name = name,
            SourcePortPath = sourcePortPath,
            IwadPath = iwadPath,
            SelectedModPaths = [.. selectedModPaths]
        };
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
            Profiles = [.. config.Profiles.Select(CloneProfile)],
            SelectedProfileId = config.SelectedProfileId,
            IsSourcePortSectionCollapsed = config.IsSourcePortSectionCollapsed,
            SelectedSourcePortPath = config.SelectedSourcePortPath,
            Iwads = [.. config.Iwads],
            IsIwadSectionCollapsed = config.IsIwadSectionCollapsed,
            Mods = [.. config.Mods],
            IsModSectionCollapsed = config.IsModSectionCollapsed,
            SelectedIwadPath = config.SelectedIwadPath,
            SelectedModPaths = [.. config.SelectedModPaths]
        });
    }

    private static ProfileConfig CloneProfile(ProfileConfig profile)
    {
        return new ProfileConfig
        {
            Id = profile.Id,
            Name = profile.Name,
            SourcePortPath = profile.SourcePortPath,
            IwadPath = profile.IwadPath,
            SelectedModPaths = [.. profile.SelectedModPaths]
        };
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
