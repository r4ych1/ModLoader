using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ModLoader.Core;

namespace ModLoader.App;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly LaunchInputsStore _store = new();
    private string? _sourcePortPath;
    private string? _selectedIwadPath;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string? SourcePortPath
    {
        get => _sourcePortPath;
        private set
        {
            if (_sourcePortPath == value)
            {
                return;
            }

            _sourcePortPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSourcePort));
        }
    }

    public bool HasSourcePort => !string.IsNullOrWhiteSpace(SourcePortPath);

    public ObservableCollection<string> Iwads { get; } = [];

    public ObservableCollection<string> Mods { get; } = [];

    public ObservableCollection<SelectablePathRow> IwadRows { get; } = [];

    public ObservableCollection<SelectablePathRow> ModRows { get; } = [];

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
        }
    }

    public ObservableCollection<string> SelectedModPaths { get; } = [];

    public void ProcessSourcePortDrop(IEnumerable<string> droppedPaths)
    {
        _store.ProcessSourcePortDrop(droppedPaths);
        RefreshFromStore();
    }

    public void ProcessIwadDrop(IEnumerable<string> droppedPaths)
    {
        _store.ProcessIwadDrop(droppedPaths);
        RefreshFromStore();
    }

    public void ProcessModDrop(IEnumerable<string> droppedPaths)
    {
        _store.ProcessModDrop(droppedPaths);
        RefreshFromStore();
    }

    public void ClearSourcePort()
    {
        _store.ClearSourcePort();
        RefreshFromStore();
    }

    public void RemoveIwad(string path)
    {
        _store.RemoveIwad(path);
        RefreshFromStore();
    }

    public void RemoveMod(string path)
    {
        _store.RemoveMod(path);
        RefreshFromStore();
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

        RefreshRows();
    }

    private void RefreshFromStore()
    {
        SourcePortPath = _store.SourcePortPath;
        CopyCollection(_store.Iwads, Iwads);
        CopyCollection(_store.Mods, Mods);
        NormalizeSelections();
        RefreshRows();
    }

    private static void CopyCollection(IReadOnlyList<string> source, ObservableCollection<string> destination)
    {
        destination.Clear();
        foreach (var path in source)
        {
            destination.Add(path);
        }
    }

    private void NormalizeSelections()
    {
        if (!ContainsPath(Iwads, SelectedIwadPath))
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
    }

    private void RefreshRows()
    {
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
