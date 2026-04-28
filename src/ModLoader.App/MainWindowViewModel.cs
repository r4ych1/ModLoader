using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ModLoader.Core;

namespace ModLoader.App;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly LaunchInputsStore _store = new();
    private string? _sourcePortPath;

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

    private void RefreshFromStore()
    {
        SourcePortPath = _store.SourcePortPath;
        CopyCollection(_store.Iwads, Iwads);
        CopyCollection(_store.Mods, Mods);
    }

    private static void CopyCollection(IReadOnlyList<string> source, ObservableCollection<string> destination)
    {
        destination.Clear();
        foreach (var path in source)
        {
            destination.Add(path);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
