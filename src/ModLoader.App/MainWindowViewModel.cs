using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ModLoader.Core;

namespace ModLoader.App;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly ISourcePortLauncher _launcher;
    private readonly ILaunchInputsPersistence _persistence;
    private readonly LaunchInputsStore _store;
    private readonly List<ProfileConfig> _profiles = [];
    private bool _isIwadSectionCollapsed;
    private bool _isModSectionCollapsed;
    private bool _isSourcePortSectionCollapsed;
    private string? _messageText;
    private string? _pendingDeleteProfileId;
    private string? _selectedIwadPath;
    private string? _selectedProfileId;
    private string? _selectedSourcePortPath;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindowViewModel()
        : this(
            new JsonLaunchInputsPersistence(Path.Combine(AppContext.BaseDirectory, "modloader.config.json")),
            new ProcessSourcePortLauncher())
    {
    }

    public MainWindowViewModel(ILaunchInputsPersistence persistence)
        : this(persistence, new ProcessSourcePortLauncher())
    {
    }

    public MainWindowViewModel(ILaunchInputsPersistence persistence, ISourcePortLauncher launcher)
    {
        _persistence = persistence;
        _launcher = launcher;

        var loadResult = _persistence.Load();
        _store = new LaunchInputsStore(loadResult.State);
        LoadProfilesFromConfig(loadResult.State);
        IsSourcePortSectionCollapsed = loadResult.State.IsSourcePortSectionCollapsed;
        IsIwadSectionCollapsed = loadResult.State.IsIwadSectionCollapsed;
        IsModSectionCollapsed = loadResult.State.IsModSectionCollapsed;

        var storeSanitized = _store.RemoveMissingPaths();
        var selectedProfileSanitized = InitializeSelectedProfile(loadResult.State.SelectedProfileId);

        if (!string.IsNullOrWhiteSpace(loadResult.WarningMessage))
        {
            SetInformationalMessage(loadResult.WarningMessage);
        }

        RefreshFromStore();

        if (storeSanitized || selectedProfileSanitized)
        {
            PersistState();
        }
    }

    public ObservableCollection<ProfileListItem> ProfileRows { get; } = [];

    public ObservableCollection<string> SourcePorts { get; } = [];

    public ObservableCollection<string> Iwads { get; } = [];

    public ObservableCollection<string> Mods { get; } = [];

    public ObservableCollection<SelectablePathRow> SourcePortRows { get; } = [];

    public ObservableCollection<SelectablePathRow> IwadRows { get; } = [];

    public ObservableCollection<SelectablePathRow> ModRows { get; } = [];

    public ObservableCollection<string> SelectedModPaths { get; } = [];

    public bool HasProfiles => ProfileRows.Count > 0;

    public bool HasSourcePort => !string.IsNullOrWhiteSpace(SelectedSourcePortPath);

    public bool HasSourcePorts => SourcePorts.Count > 0;

    public bool HasIwads => Iwads.Count > 0;

    public bool HasMods => Mods.Count > 0;

    public bool HasSelectedProfile => !string.IsNullOrWhiteSpace(SelectedProfileId);

    public bool CanCreateProfile => !string.IsNullOrWhiteSpace(SelectedSourcePortPath) && !string.IsNullOrWhiteSpace(SelectedIwadPath);

    public bool CanLaunch
    {
        get
        {
            var selectedProfile = GetSelectedProfile();
            if (selectedProfile is null)
            {
                return false;
            }

            return GetProfileValidity(selectedProfile).IsValid;
        }
    }

    public string? SelectedSourcePortPath
    {
        get => _selectedSourcePortPath;
        private set
        {
            if (string.Equals(_selectedSourcePortPath, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _selectedSourcePortPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSourcePort));
            OnPropertyChanged(nameof(CanCreateProfile));
            OnPropertyChanged(nameof(CommandPreviewArguments));
        }
    }

    public string? SelectedIwadPath
    {
        get => _selectedIwadPath;
        private set
        {
            if (string.Equals(_selectedIwadPath, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _selectedIwadPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreateProfile));
            OnPropertyChanged(nameof(CommandPreviewArguments));
        }
    }

    public string? SelectedProfileId
    {
        get => _selectedProfileId;
        private set
        {
            if (string.Equals(_selectedProfileId, value, StringComparison.Ordinal))
            {
                return;
            }

            _selectedProfileId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedProfile));
            OnPropertyChanged(nameof(CanLaunch));
            OnPropertyChanged(nameof(SelectedProfileName));
            OnPropertyChanged(nameof(SelectedProfileStatusText));
        }
    }

    public string? MessageText
    {
        get => _messageText;
        private set
        {
            if (_messageText == value)
            {
                return;
            }

            _messageText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasMessage));
        }
    }

    public bool HasMessage => !string.IsNullOrWhiteSpace(MessageText);

    public bool HasPendingDeleteConfirmation => !string.IsNullOrWhiteSpace(_pendingDeleteProfileId);

    public bool IsSourcePortSectionCollapsed
    {
        get => _isSourcePortSectionCollapsed;
        private set
        {
            if (_isSourcePortSectionCollapsed == value)
            {
                return;
            }

            _isSourcePortSectionCollapsed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AreSourcePortRowsVisible));
            OnPropertyChanged(nameof(SourcePortSectionToggleText));
        }
    }

    public bool AreSourcePortRowsVisible => !IsSourcePortSectionCollapsed;

    public string SourcePortSectionToggleText => IsSourcePortSectionCollapsed ? "Expand" : "Collapse";

    public bool IsIwadSectionCollapsed
    {
        get => _isIwadSectionCollapsed;
        private set
        {
            if (_isIwadSectionCollapsed == value)
            {
                return;
            }

            _isIwadSectionCollapsed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AreIwadRowsVisible));
            OnPropertyChanged(nameof(IwadSectionToggleText));
        }
    }

    public bool AreIwadRowsVisible => !IsIwadSectionCollapsed;

    public string IwadSectionToggleText => IsIwadSectionCollapsed ? "Expand" : "Collapse";

    public bool IsModSectionCollapsed
    {
        get => _isModSectionCollapsed;
        private set
        {
            if (_isModSectionCollapsed == value)
            {
                return;
            }

            _isModSectionCollapsed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AreModRowsVisible));
            OnPropertyChanged(nameof(ModSectionToggleText));
        }
    }

    public bool AreModRowsVisible => !IsModSectionCollapsed;

    public string ModSectionToggleText => IsModSectionCollapsed ? "Expand" : "Collapse";

    public string SelectedProfileName => GetSelectedProfile()?.Name ?? "No Profile Selected";

    public string SelectedProfileStatusText
    {
        get
        {
            var selectedProfile = GetSelectedProfile();
            if (selectedProfile is null)
            {
                return "Select a saved profile or create one from the current library selections.";
            }

            var validity = GetProfileValidity(selectedProfile);
            return validity.IsValid ? "Selected profile is ready to launch." : validity.Reason;
        }
    }

    public string CommandPreviewArguments => BuildCommandPreviewArguments();

    public void ProcessSourcePortDrop(IEnumerable<string> droppedPaths)
    {
        _store.ProcessSourcePortDrop(droppedPaths);
        ClearPendingDeleteConfirmation();
        RefreshFromStore();
        PersistState();
    }

    public void ProcessIwadDrop(IEnumerable<string> droppedPaths)
    {
        _store.ProcessIwadDrop(droppedPaths);
        ClearPendingDeleteConfirmation();
        RefreshFromStore();
        PersistState();
    }

    public void ProcessModDrop(IEnumerable<string> droppedPaths)
    {
        _store.ProcessModDrop(droppedPaths);
        ClearPendingDeleteConfirmation();
        RefreshFromStore();
        PersistState();
    }

    public void ClearAllSourcePorts()
    {
        _store.ClearSourcePorts();
        ClearPendingDeleteConfirmation();
        RefreshFromStore();
        PersistState();
    }

    public void ToggleSourcePortSectionCollapsed()
    {
        IsSourcePortSectionCollapsed = !IsSourcePortSectionCollapsed;
        PersistState();
    }

    public void ClearSourcePort()
    {
        ClearAllSourcePorts();
    }

    public void RemoveSourcePort(string path)
    {
        _store.RemoveSourcePort(path);
        ClearPendingDeleteConfirmation();
        RefreshFromStore();
        PersistState();
    }

    public void RemoveIwad(string path)
    {
        _store.RemoveIwad(path);
        ClearPendingDeleteConfirmation();
        RefreshFromStore();
        PersistState();
    }

    public void RemoveMod(string path)
    {
        _store.RemoveMod(path);
        ClearPendingDeleteConfirmation();
        RefreshFromStore();
        PersistState();
    }

    public void ClearAllIwads()
    {
        _store.ClearIwads();
        ClearPendingDeleteConfirmation();
        RefreshFromStore();
        PersistState();
    }

    public void ToggleIwadSectionCollapsed()
    {
        IsIwadSectionCollapsed = !IsIwadSectionCollapsed;
        PersistState();
    }

    public void ClearAllMods()
    {
        _store.ClearMods();
        ClearPendingDeleteConfirmation();
        RefreshFromStore();
        PersistState();
    }

    public void ToggleModSectionCollapsed()
    {
        IsModSectionCollapsed = !IsModSectionCollapsed;
        PersistState();
    }

    public void ToggleSourcePortSelection(string path)
    {
        var normalizedPath = PathNormalizer.NormalizeAbsolutePath(path);

        if (string.Equals(SelectedSourcePortPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
        {
            SelectedSourcePortPath = null;
        }
        else
        {
            SelectedSourcePortPath = normalizedPath;
        }

        ClearPendingDeleteConfirmation();
        SyncSelectedProfileFromSelections();
        RefreshRows();
        RefreshProfileRows();
        OnPropertyChanged(nameof(CanLaunch));
        OnPropertyChanged(nameof(SelectedProfileStatusText));
        PersistState();
    }

    public void ToggleIwadSelection(string path)
    {
        var normalizedPath = PathNormalizer.NormalizeAbsolutePath(path);

        if (string.Equals(SelectedIwadPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
        {
            SelectedIwadPath = null;
        }
        else
        {
            SelectedIwadPath = normalizedPath;
        }

        ClearPendingDeleteConfirmation();
        SyncSelectedProfileFromSelections();
        RefreshRows();
        RefreshProfileRows();
        OnPropertyChanged(nameof(CanLaunch));
        OnPropertyChanged(nameof(SelectedProfileStatusText));
        PersistState();
    }

    public void ToggleModSelection(string path)
    {
        var normalizedPath = PathNormalizer.NormalizeAbsolutePath(path);
        var existingIndex = FindPathIndex(SelectedModPaths, normalizedPath);

        if (existingIndex >= 0)
        {
            SelectedModPaths.RemoveAt(existingIndex);
        }
        else
        {
            SelectedModPaths.Add(normalizedPath);
        }

        var modOrderSynchronized = ApplySelectionSynchronizedModOrdering();
        if (modOrderSynchronized)
        {
            CopyCollection(_store.Mods, Mods);
        }

        ClearPendingDeleteConfirmation();
        SyncSelectedProfileFromSelections();
        RefreshRows();
        RefreshProfileRows();
        OnPropertyChanged(nameof(CommandPreviewArguments));
        OnPropertyChanged(nameof(CanLaunch));
        OnPropertyChanged(nameof(SelectedProfileStatusText));
        PersistState();
    }

    public void ToggleProfileSelection(string profileId)
    {
        CancelRename();

        if (string.Equals(SelectedProfileId, profileId, StringComparison.Ordinal))
        {
            SelectedProfileId = null;
            ClearCurrentSelections();
        }
        else if (TrySelectProfile(profileId))
        {
            HydrateSelectionsFromSelectedProfile();
        }

        ClearPendingDeleteConfirmation();
        RefreshRows();
        RefreshProfileRows();
        OnPropertyChanged(nameof(CanLaunch));
        OnPropertyChanged(nameof(SelectedProfileStatusText));
        PersistState();
    }

    public void CreateNewProfile()
    {
        if (!CanCreateProfile || string.IsNullOrWhiteSpace(SelectedSourcePortPath) || string.IsNullOrWhiteSpace(SelectedIwadPath))
        {
            return;
        }

        CancelRename();
        ClearPendingDeleteConfirmation();

        var profile = new ProfileConfig
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = GenerateDefaultProfileName(),
            SourcePortPath = SelectedSourcePortPath,
            IwadPath = SelectedIwadPath,
            SelectedModPaths = [.. SelectedModPaths]
        };

        _profiles.Add(profile);
        SelectedProfileId = profile.Id;
        RefreshProfileRows();
        OnPropertyChanged(nameof(HasProfiles));
        OnPropertyChanged(nameof(CanLaunch));
        OnPropertyChanged(nameof(SelectedProfileName));
        OnPropertyChanged(nameof(SelectedProfileStatusText));
        PersistState();
    }

    public void BeginRenameProfile(string profileId)
    {
        CancelRename();
        ClearPendingDeleteConfirmation();

        if (!TrySelectProfile(profileId))
        {
            return;
        }

        HydrateSelectionsFromSelectedProfile();
        RefreshRows();
        RefreshProfileRows();

        var row = FindProfileRow(profileId);
        if (row is null)
        {
            return;
        }

        row.IsRenaming = true;
        row.RenameText = row.Name;
        ClearInformationalMessage();
        OnPropertyChanged(nameof(CanLaunch));
        OnPropertyChanged(nameof(SelectedProfileStatusText));
        PersistState();
    }

    public void UpdateRenameText(string profileId, string? text)
    {
        var row = FindProfileRow(profileId);
        if (row is null || !row.IsRenaming)
        {
            return;
        }

        row.RenameText = text ?? string.Empty;
    }

    public void CommitRename(string profileId)
    {
        var row = FindProfileRow(profileId);
        if (row is null || !row.IsRenaming)
        {
            return;
        }

        var proposedName = row.RenameText.Trim();
        if (string.IsNullOrWhiteSpace(proposedName))
        {
            SetInformationalMessage("Profile name is required.");
            return;
        }

        if (_profiles.Any(
                profile => !string.Equals(profile.Id, profileId, StringComparison.Ordinal)
                    && string.Equals(profile.Name, proposedName, StringComparison.OrdinalIgnoreCase)))
        {
            SetInformationalMessage("Profile name must be unique.");
            return;
        }

        var profileIndex = FindProfileIndex(profileId);
        if (profileIndex < 0)
        {
            return;
        }

        var existingProfile = _profiles[profileIndex];
        _profiles[profileIndex] = new ProfileConfig
        {
            Id = existingProfile.Id,
            Name = proposedName,
            SourcePortPath = existingProfile.SourcePortPath,
            IwadPath = existingProfile.IwadPath,
            SelectedModPaths = [.. existingProfile.SelectedModPaths]
        };

        row.IsRenaming = false;
        row.RenameText = proposedName;
        ClearInformationalMessage();
        RefreshProfileRows();
        OnPropertyChanged(nameof(SelectedProfileName));
        OnPropertyChanged(nameof(SelectedProfileStatusText));
        PersistState();
    }

    public void CancelRename()
    {
        foreach (var row in ProfileRows)
        {
            if (!row.IsRenaming)
            {
                continue;
            }

            row.IsRenaming = false;
            row.RenameText = row.Name;
        }
    }

    public void RequestDeleteProfile(string profileId)
    {
        CancelRename();

        var profile = _profiles.FirstOrDefault(candidate => string.Equals(candidate.Id, profileId, StringComparison.Ordinal));
        if (profile is null)
        {
            return;
        }

        _pendingDeleteProfileId = profileId;
        SetInformationalMessage($"Delete profile \"{profile.Name}\"?");
        OnPropertyChanged(nameof(HasPendingDeleteConfirmation));
    }

    public void ConfirmDeleteProfile()
    {
        if (string.IsNullOrWhiteSpace(_pendingDeleteProfileId))
        {
            return;
        }

        var deleteProfileId = _pendingDeleteProfileId;
        var removed = _profiles.RemoveAll(profile => string.Equals(profile.Id, deleteProfileId, StringComparison.Ordinal)) > 0;
        ClearPendingDeleteConfirmation();

        if (!removed)
        {
            return;
        }

        if (string.Equals(SelectedProfileId, deleteProfileId, StringComparison.Ordinal))
        {
            SelectedProfileId = null;
            ClearCurrentSelections();
        }

        RefreshRows();
        RefreshProfileRows();
        OnPropertyChanged(nameof(HasProfiles));
        OnPropertyChanged(nameof(CanLaunch));
        OnPropertyChanged(nameof(SelectedProfileName));
        OnPropertyChanged(nameof(SelectedProfileStatusText));
        PersistState();
    }

    public void CancelDeleteConfirmation()
    {
        ClearPendingDeleteConfirmation();
    }

    public void LaunchSourcePort()
    {
        var selectedProfile = GetSelectedProfile();
        if (selectedProfile is null)
        {
            return;
        }

        var validity = GetProfileValidity(selectedProfile);
        if (!validity.IsValid || string.IsNullOrWhiteSpace(selectedProfile.SourcePortPath) || string.IsNullOrWhiteSpace(selectedProfile.IwadPath))
        {
            return;
        }

        try
        {
            _launcher.Launch(selectedProfile.SourcePortPath, BuildLaunchArguments(selectedProfile));
        }
        catch (Exception ex)
        {
            SetInformationalMessage($"Launch failed: {ex.Message}");
        }
    }

    private void RefreshFromStore()
    {
        CopyCollection(_store.SourcePorts, SourcePorts);
        CopyCollection(_store.Iwads, Iwads);
        CopyCollection(_store.Mods, Mods);

        OnPropertyChanged(nameof(HasSourcePorts));
        OnPropertyChanged(nameof(HasIwads));
        OnPropertyChanged(nameof(HasMods));

        if (HasSelectedProfile)
        {
            HydrateSelectionsFromSelectedProfile();
        }
        else
        {
            NormalizeDetachedSelections();
        }

        RefreshRows();
        RefreshProfileRows();
        OnPropertyChanged(nameof(CanCreateProfile));
        OnPropertyChanged(nameof(CanLaunch));
        OnPropertyChanged(nameof(CommandPreviewArguments));
        OnPropertyChanged(nameof(SelectedProfileName));
        OnPropertyChanged(nameof(SelectedProfileStatusText));
    }

    private void RefreshRows()
    {
        CopyRows(
            SourcePorts,
            SourcePortRows,
            path => string.Equals(path, SelectedSourcePortPath, StringComparison.OrdinalIgnoreCase));

        CopyRows(
            Iwads,
            IwadRows,
            path => string.Equals(path, SelectedIwadPath, StringComparison.OrdinalIgnoreCase));

        CopyRows(
            Mods,
            ModRows,
            path => FindPathIndex(SelectedModPaths, path) >= 0);
    }

    private void RefreshProfileRows()
    {
        var existingById = ProfileRows.ToDictionary(row => row.Id, StringComparer.Ordinal);
        ProfileRows.Clear();

        foreach (var profile in _profiles)
        {
            if (!existingById.TryGetValue(profile.Id, out var row))
            {
                row = new ProfileListItem(profile.Id, profile.Name);
            }

            var validity = GetProfileValidity(profile);

            row.Name = profile.Name;
            row.IsSelected = string.Equals(profile.Id, SelectedProfileId, StringComparison.Ordinal);
            row.IsInvalid = !validity.IsValid;
            row.InvalidReason = validity.Reason;

            if (!row.IsRenaming)
            {
                row.RenameText = row.Name;
            }

            ProfileRows.Add(row);
        }

        OnPropertyChanged(nameof(HasProfiles));
    }

    private static void CopyCollection(IReadOnlyList<string> source, ObservableCollection<string> destination)
    {
        destination.Clear();
        foreach (var path in source)
        {
            destination.Add(path);
        }
    }

    private static void CopyRows(
        IReadOnlyList<string> paths,
        ObservableCollection<SelectablePathRow> destination,
        Func<string, bool> isSelected)
    {
        destination.Clear();
        foreach (var path in paths)
        {
            destination.Add(new SelectablePathRow(path, isSelected(path)));
        }
    }

    private void LoadProfilesFromConfig(LaunchInputsConfig state)
    {
        _profiles.Clear();

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var profile in state.Profiles ?? [])
        {
            if (string.IsNullOrWhiteSpace(profile.Id) || string.IsNullOrWhiteSpace(profile.Name))
            {
                continue;
            }

            if (!seenIds.Add(profile.Id))
            {
                continue;
            }

            var normalizedModPaths = new List<string>();
            var seenModPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var modPath in profile.SelectedModPaths ?? [])
            {
                if (string.IsNullOrWhiteSpace(modPath))
                {
                    continue;
                }

                var normalizedModPath = PathNormalizer.NormalizeAbsolutePath(modPath);
                if (seenModPaths.Add(normalizedModPath))
                {
                    normalizedModPaths.Add(normalizedModPath);
                }
            }

            _profiles.Add(new ProfileConfig
            {
                Id = profile.Id,
                Name = profile.Name,
                SourcePortPath = NormalizeNullablePath(profile.SourcePortPath),
                IwadPath = NormalizeNullablePath(profile.IwadPath),
                SelectedModPaths = normalizedModPaths
            });
        }
    }

    private bool InitializeSelectedProfile(string? selectedProfileId)
    {
        if (string.IsNullOrWhiteSpace(selectedProfileId))
        {
            SelectedProfileId = null;
            return false;
        }

        var matchingProfile = _profiles.FirstOrDefault(profile => string.Equals(profile.Id, selectedProfileId, StringComparison.Ordinal));
        if (matchingProfile is null)
        {
            SelectedProfileId = null;
            return true;
        }

        SelectedProfileId = matchingProfile.Id;
        return false;
    }

    private bool TrySelectProfile(string profileId)
    {
        var profile = _profiles.FirstOrDefault(candidate => string.Equals(candidate.Id, profileId, StringComparison.Ordinal));
        if (profile is null)
        {
            return false;
        }

        SelectedProfileId = profile.Id;
        return true;
    }

    private ProfileConfig? GetSelectedProfile()
    {
        if (string.IsNullOrWhiteSpace(SelectedProfileId))
        {
            return null;
        }

        return _profiles.FirstOrDefault(profile => string.Equals(profile.Id, SelectedProfileId, StringComparison.Ordinal));
    }

    private void HydrateSelectionsFromSelectedProfile()
    {
        var selectedProfile = GetSelectedProfile();
        if (selectedProfile is null)
        {
            ClearCurrentSelections();
            return;
        }

        SelectedSourcePortPath = ResolveSelectablePath(selectedProfile.SourcePortPath, SourcePorts);
        SelectedIwadPath = ResolveSelectablePath(selectedProfile.IwadPath, Iwads);

        SelectedModPaths.Clear();
        foreach (var modPath in selectedProfile.SelectedModPaths)
        {
            var resolvedPath = ResolveSelectablePath(modPath, Mods);
            if (resolvedPath is null || FindPathIndex(SelectedModPaths, resolvedPath) >= 0)
            {
                continue;
            }

            SelectedModPaths.Add(resolvedPath);
        }

        OnPropertyChanged(nameof(CommandPreviewArguments));
        OnPropertyChanged(nameof(CanCreateProfile));
    }

    private void NormalizeDetachedSelections()
    {
        if (!ContainsPath(SourcePorts, SelectedSourcePortPath) && !string.IsNullOrWhiteSpace(SelectedSourcePortPath))
        {
            SelectedSourcePortPath = null;
        }

        if (!ContainsPath(Iwads, SelectedIwadPath) && !string.IsNullOrWhiteSpace(SelectedIwadPath))
        {
            SelectedIwadPath = null;
        }

        var selectedModSnapshot = SelectedModPaths.ToArray();
        foreach (var selectedModPath in selectedModSnapshot)
        {
            if (!ContainsPath(Mods, selectedModPath))
            {
                var selectedIndex = FindPathIndex(SelectedModPaths, selectedModPath);
                if (selectedIndex >= 0)
                {
                    SelectedModPaths.RemoveAt(selectedIndex);
                }
            }
        }

        OnPropertyChanged(nameof(CommandPreviewArguments));
        OnPropertyChanged(nameof(CanCreateProfile));
    }

    private void ClearCurrentSelections()
    {
        SelectedSourcePortPath = null;
        SelectedIwadPath = null;
        SelectedModPaths.Clear();
        OnPropertyChanged(nameof(CommandPreviewArguments));
        OnPropertyChanged(nameof(CanCreateProfile));
    }

    private void SyncSelectedProfileFromSelections()
    {
        var profile = GetSelectedProfile();
        if (profile is null)
        {
            return;
        }

        var profileIndex = FindProfileIndex(profile.Id);
        if (profileIndex < 0)
        {
            return;
        }

        _profiles[profileIndex] = new ProfileConfig
        {
            Id = profile.Id,
            Name = profile.Name,
            SourcePortPath = SelectedSourcePortPath,
            IwadPath = SelectedIwadPath,
            SelectedModPaths = [.. SelectedModPaths]
        };
    }

    private int FindProfileIndex(string profileId)
    {
        for (var i = 0; i < _profiles.Count; i++)
        {
            if (string.Equals(_profiles[i].Id, profileId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private ProfileListItem? FindProfileRow(string profileId)
    {
        return ProfileRows.FirstOrDefault(row => string.Equals(row.Id, profileId, StringComparison.Ordinal));
    }

    private ProfileValidity GetProfileValidity(ProfileConfig profile)
    {
        var reasons = new List<string>();

        if (string.IsNullOrWhiteSpace(profile.SourcePortPath))
        {
            reasons.Add("Source Port is required.");
        }
        else
        {
            if (!File.Exists(profile.SourcePortPath))
            {
                reasons.Add($"Source Port file is missing: {Path.GetFileName(profile.SourcePortPath)}");
            }
            else if (!ContainsPath(SourcePorts, profile.SourcePortPath))
            {
                reasons.Add($"Source Port is no longer in the library: {Path.GetFileName(profile.SourcePortPath)}");
            }
        }

        if (string.IsNullOrWhiteSpace(profile.IwadPath))
        {
            reasons.Add("IWAD is required.");
        }
        else
        {
            if (!File.Exists(profile.IwadPath))
            {
                reasons.Add($"IWAD file is missing: {Path.GetFileName(profile.IwadPath)}");
            }
            else if (!ContainsPath(Iwads, profile.IwadPath))
            {
                reasons.Add($"IWAD is no longer in the library: {Path.GetFileName(profile.IwadPath)}");
            }
        }

        foreach (var modPath in profile.SelectedModPaths)
        {
            if (!File.Exists(modPath))
            {
                reasons.Add($"Mod file is missing: {Path.GetFileName(modPath)}");
                continue;
            }

            if (!ContainsPath(Mods, modPath))
            {
                reasons.Add($"Mod is no longer in the library: {Path.GetFileName(modPath)}");
            }
        }

        return reasons.Count == 0
            ? new ProfileValidity(true, "Selected profile is ready to launch.")
            : new ProfileValidity(false, string.Join(" ", reasons));
    }

    private bool ApplySelectionSynchronizedModOrdering()
    {
        return _store.ReorderModsBySelectionSequence(SelectedModPaths);
    }

    private static bool ContainsPath(IEnumerable<string> candidates, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        foreach (var candidate in candidates)
        {
            if (string.Equals(candidate, path, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static int FindPathIndex(IReadOnlyList<string> candidates, string path)
    {
        for (var i = 0; i < candidates.Count; i++)
        {
            if (string.Equals(candidates[i], path, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static string? ResolveSelectablePath(string? path, IEnumerable<string> candidates)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var candidate in candidates)
        {
            if (string.Equals(candidate, path, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? NormalizeNullablePath(string? path)
    {
        return string.IsNullOrWhiteSpace(path) ? null : PathNormalizer.NormalizeAbsolutePath(path);
    }

    private string GenerateDefaultProfileName()
    {
        var usedNames = new HashSet<string>(_profiles.Select(profile => profile.Name), StringComparer.OrdinalIgnoreCase);
        var suffix = 1;

        while (true)
        {
            var candidateName = $"Profile {suffix}";
            if (!usedNames.Contains(candidateName))
            {
                return candidateName;
            }

            suffix++;
        }
    }

    private string BuildCommandPreviewArguments()
    {
        var arguments = new List<string>();

        if (!string.IsNullOrWhiteSpace(SelectedSourcePortPath))
        {
            arguments.Add(FormatPreviewFileToken(SelectedSourcePortPath));
        }

        if (!string.IsNullOrWhiteSpace(SelectedIwadPath))
        {
            arguments.Add("-iwad");
            arguments.Add(FormatPreviewFileToken(SelectedIwadPath));
        }

        if (SelectedModPaths.Count > 0)
        {
            arguments.Add("-file");
            foreach (var selectedModPath in SelectedModPaths)
            {
                arguments.Add(FormatPreviewFileToken(selectedModPath));
            }
        }

        return string.Join(" ", arguments);
    }

    private static List<string> BuildLaunchArguments(ProfileConfig profile)
    {
        var arguments = new List<string>
        {
            "-iwad",
            profile.IwadPath!
        };

        if (profile.SelectedModPaths.Count > 0)
        {
            arguments.Add("-file");
            foreach (var selectedModPath in profile.SelectedModPaths)
            {
                arguments.Add(selectedModPath);
            }
        }

        return arguments;
    }

    private static string FormatPreviewFileToken(string path)
    {
        var fileName = Path.GetFileName(path);
        var displayToken = string.IsNullOrWhiteSpace(fileName) ? path : fileName;

        return displayToken.Contains(' ', StringComparison.Ordinal)
            ? $"\"{displayToken}\""
            : displayToken;
    }

    private void PersistState()
    {
        var snapshot = _store.CreateSnapshot();
        var persistedConfig = new LaunchInputsConfig
        {
            SourcePorts = [.. snapshot.SourcePorts],
            Profiles = [.. _profiles.Select(CloneProfile)],
            SelectedProfileId = SelectedProfileId,
            IsSourcePortSectionCollapsed = IsSourcePortSectionCollapsed,
            SelectedSourcePortPath = null,
            Iwads = [.. snapshot.Iwads],
            IsIwadSectionCollapsed = IsIwadSectionCollapsed,
            Mods = [.. snapshot.Mods],
            IsModSectionCollapsed = IsModSectionCollapsed,
            SelectedIwadPath = null,
            SelectedModPaths = []
        };

        _persistence.Save(persistedConfig);
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

    private void SetInformationalMessage(string? message)
    {
        MessageText = message;
    }

    private void ClearInformationalMessage()
    {
        if (HasPendingDeleteConfirmation)
        {
            return;
        }

        MessageText = null;
    }

    private void ClearPendingDeleteConfirmation()
    {
        var hadPendingDelete = HasPendingDeleteConfirmation;
        _pendingDeleteProfileId = null;
        MessageText = null;

        if (hadPendingDelete)
        {
            OnPropertyChanged(nameof(HasPendingDeleteConfirmation));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class SelectablePathRow
{
    public SelectablePathRow(string path, bool isSelected)
    {
        Path = path;
        IsSelected = isSelected;
    }

    public string Path { get; }

    public bool IsSelected { get; }
}

public sealed class ProfileListItem : INotifyPropertyChanged
{
    private bool _isInvalid;
    private bool _isRenaming;
    private bool _isSelected;
    private string _invalidReason;
    private string _name;
    private string _renameText;

    public ProfileListItem(string id, string name)
    {
        Id = id;
        _name = name;
        _renameText = name;
        _invalidReason = string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public bool IsInvalid
    {
        get => _isInvalid;
        set
        {
            if (_isInvalid == value)
            {
                return;
            }

            _isInvalid = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasInvalidReason));
        }
    }

    public string InvalidReason
    {
        get => _invalidReason;
        set
        {
            if (_invalidReason == value)
            {
                return;
            }

            _invalidReason = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasInvalidReason));
        }
    }

    public bool IsRenaming
    {
        get => _isRenaming;
        set
        {
            if (_isRenaming == value)
            {
                return;
            }

            _isRenaming = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDisplayVisible));
            OnPropertyChanged(nameof(IsRenameVisible));
        }
    }

    public string RenameText
    {
        get => _renameText;
        set
        {
            if (_renameText == value)
            {
                return;
            }

            _renameText = value;
            OnPropertyChanged();
        }
    }

    public bool IsDisplayVisible => !IsRenaming;

    public bool IsRenameVisible => IsRenaming;

    public bool HasInvalidReason => IsInvalid && !string.IsNullOrWhiteSpace(InvalidReason);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal readonly record struct ProfileValidity(bool IsValid, string Reason);
