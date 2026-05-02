using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using ModLoader.Core;

namespace ModLoader.App;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly ISourcePortLauncher _launcher;
    private readonly ILaunchInputsPersistence _persistence;
    private readonly LaunchInputsStore _store;
    private string? _selectedSourcePortPath;
    private string? _selectedIwadPath;
    private string? _startupWarningMessage;

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
        InitializeSelectionsFromConfig(loadResult.State);

        var storeSanitized = _store.RemoveMissingPaths();

        StartupWarningMessage = loadResult.WarningMessage;
        var selectionSanitized = RefreshFromStore();
        var modOrderSynchronized = ApplySelectionSynchronizedModOrdering();
        if (modOrderSynchronized)
        {
            RefreshFromStore();
        }

        if (storeSanitized || selectionSanitized || modOrderSynchronized)
        {
            PersistState();
        }
    }

    public bool HasSourcePort => !string.IsNullOrWhiteSpace(SelectedSourcePortPath);

    public bool HasSourcePorts => SourcePorts.Count > 0;

    public bool CanLaunch => HasSourcePort && !string.IsNullOrWhiteSpace(SelectedIwadPath);

    public ObservableCollection<string> SourcePorts { get; } = [];

    public ObservableCollection<string> Iwads { get; } = [];

    public ObservableCollection<string> Mods { get; } = [];

    public bool HasIwads => Iwads.Count > 0;

    public bool HasMods => Mods.Count > 0;

    public ObservableCollection<SelectablePathRow> SourcePortRows { get; } = [];

    public ObservableCollection<SelectablePathRow> IwadRows { get; } = [];

    public ObservableCollection<SelectablePathRow> ModRows { get; } = [];

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
            OnPropertyChanged(nameof(CanLaunch));
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
            OnPropertyChanged(nameof(CanLaunch));
            OnPropertyChanged(nameof(CommandPreviewArguments));
        }
    }

    public ObservableCollection<string> SelectedModPaths { get; } = [];

    public string? StartupWarningMessage
    {
        get => _startupWarningMessage;
        private set
        {
            if (_startupWarningMessage == value)
            {
                return;
            }

            _startupWarningMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStartupWarning));
        }
    }

    public bool HasStartupWarning => !string.IsNullOrWhiteSpace(StartupWarningMessage);

    public string CommandPreviewArguments => BuildCommandPreviewArguments();

    public void ProcessSourcePortDrop(IEnumerable<string> droppedPaths)
    {
        _store.ProcessSourcePortDrop(droppedPaths);
        RefreshFromStore();
        PersistState();
    }

    public void ProcessIwadDrop(IEnumerable<string> droppedPaths)
    {
        _store.ProcessIwadDrop(droppedPaths);
        RefreshFromStore();
        PersistState();
    }

    public void ProcessModDrop(IEnumerable<string> droppedPaths)
    {
        _store.ProcessModDrop(droppedPaths);
        RefreshFromStore();
        PersistState();
    }

    public void ClearAllSourcePorts()
    {
        _store.ClearSourcePorts();
        RefreshFromStore();
        PersistState();
    }

    public void ClearSourcePort()
    {
        ClearAllSourcePorts();
    }

    public void RemoveSourcePort(string path)
    {
        _store.RemoveSourcePort(path);
        RefreshFromStore();
        PersistState();
    }

    public void RemoveIwad(string path)
    {
        _store.RemoveIwad(path);
        RefreshFromStore();
        PersistState();
    }

    public void RemoveMod(string path)
    {
        _store.RemoveMod(path);
        RefreshFromStore();
        PersistState();
    }

    public void ClearAllIwads()
    {
        _store.ClearIwads();
        RefreshFromStore();
        PersistState();
    }

    public void ClearAllMods()
    {
        _store.ClearMods();
        RefreshFromStore();
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

        RefreshRows();
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

        RefreshRows();
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
            RefreshFromStore();
        }
        else
        {
            RefreshRows();
            OnPropertyChanged(nameof(CommandPreviewArguments));
        }

        PersistState();
    }

    public void LaunchSourcePort()
    {
        if (!CanLaunch || string.IsNullOrWhiteSpace(SelectedSourcePortPath) || string.IsNullOrWhiteSpace(SelectedIwadPath))
        {
            return;
        }

        try
        {
            _launcher.Launch(SelectedSourcePortPath, BuildLaunchArguments());
        }
        catch (Exception ex)
        {
            StartupWarningMessage = $"Launch failed: {ex.Message}";
        }
    }

    private bool RefreshFromStore()
    {
        CopyCollection(_store.SourcePorts, SourcePorts);
        CopyCollection(_store.Iwads, Iwads);
        CopyCollection(_store.Mods, Mods);
        OnPropertyChanged(nameof(HasSourcePorts));
        OnPropertyChanged(nameof(HasIwads));
        OnPropertyChanged(nameof(HasMods));
        var selectionChanged = NormalizeSelections();
        RefreshRows();
        OnPropertyChanged(nameof(CommandPreviewArguments));
        return selectionChanged;
    }

    private static void CopyCollection(IReadOnlyList<string> source, ObservableCollection<string> destination)
    {
        destination.Clear();
        foreach (var path in source)
        {
            destination.Add(path);
        }
    }

    private bool NormalizeSelections()
    {
        var changed = false;

        if (!ContainsPath(SourcePorts, SelectedSourcePortPath) && !string.IsNullOrWhiteSpace(SelectedSourcePortPath))
        {
            SelectedSourcePortPath = null;
            changed = true;
        }

        if (!ContainsPath(Iwads, SelectedIwadPath) && !string.IsNullOrWhiteSpace(SelectedIwadPath))
        {
            SelectedIwadPath = null;
            changed = true;
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
                    changed = true;
                }
            }
        }

        return changed;
    }

    private void InitializeSelectionsFromConfig(LaunchInputsConfig state)
    {
        if (!string.IsNullOrWhiteSpace(state.SelectedSourcePortPath))
        {
            _selectedSourcePortPath = PathNormalizer.NormalizeAbsolutePath(state.SelectedSourcePortPath);
        }

        if (!string.IsNullOrWhiteSpace(state.SelectedIwadPath))
        {
            _selectedIwadPath = PathNormalizer.NormalizeAbsolutePath(state.SelectedIwadPath);
        }

        foreach (var selectedModPath in state.SelectedModPaths ?? [])
        {
            if (string.IsNullOrWhiteSpace(selectedModPath))
            {
                continue;
            }

            var normalizedPath = PathNormalizer.NormalizeAbsolutePath(selectedModPath);
            if (FindPathIndex(SelectedModPaths, normalizedPath) < 0)
            {
                SelectedModPaths.Add(normalizedPath);
            }
        }
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

    private bool ApplySelectionSynchronizedModOrdering()
    {
        return _store.ReorderModsBySelectionSequence(SelectedModPaths);
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

    private List<string> BuildLaunchArguments()
    {
        var arguments = new List<string>
        {
            "-iwad",
            SelectedIwadPath!
        };

        if (SelectedModPaths.Count > 0)
        {
            arguments.Add("-file");
            foreach (var selectedModPath in SelectedModPaths)
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void PersistState()
    {
        var snapshot = _store.CreateSnapshot();
        var persistedConfig = new LaunchInputsConfig
        {
            SourcePorts = [.. snapshot.SourcePorts],
            SelectedSourcePortPath = SelectedSourcePortPath,
            Iwads = [.. snapshot.Iwads],
            Mods = [.. snapshot.Mods],
            SelectedIwadPath = SelectedIwadPath,
            SelectedModPaths = [.. SelectedModPaths]
        };

        _persistence.Save(persistedConfig);
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
