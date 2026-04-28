using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;

namespace ModLoader.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    private void OnDropZoneDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = HasFilePayload(e) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnSourcePortDrop(object? sender, DragEventArgs e)
    {
        _viewModel.ProcessSourcePortDrop(ExtractDroppedPaths(e));
        e.Handled = true;
    }

    private void OnIwadDrop(object? sender, DragEventArgs e)
    {
        _viewModel.ProcessIwadDrop(ExtractDroppedPaths(e));
        e.Handled = true;
    }

    private void OnModDrop(object? sender, DragEventArgs e)
    {
        _viewModel.ProcessModDrop(ExtractDroppedPaths(e));
        e.Handled = true;
    }

    private void OnClearSourcePortClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.ClearSourcePort();
    }

    private void OnRemoveIwadClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string path)
        {
            _viewModel.RemoveIwad(path);
        }
    }

    private void OnRemoveModClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string path)
        {
            _viewModel.RemoveMod(path);
        }
    }

    private static bool HasFilePayload(DragEventArgs e)
    {
        return e.Data.Contains(DataFormats.Files);
    }

    private static IReadOnlyList<string> ExtractDroppedPaths(DragEventArgs e)
    {
        var droppedItems = e.Data.GetFiles();
        if (droppedItems is null)
        {
            return Array.Empty<string>();
        }

        var localPaths = new List<string>();
        foreach (var droppedItem in droppedItems)
        {
            var localPath = droppedItem.Path.LocalPath;
            if (!string.IsNullOrWhiteSpace(localPath))
            {
                localPaths.Add(localPath);
            }
        }

        return localPaths;
    }
}
