using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ModLoader.Core;

namespace ModLoader.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "modloader.config.json");
        var persistence = new JsonLaunchInputsPersistence(configPath);
        _viewModel = new MainWindowViewModel(persistence);

        InitializeComponent();
        DataContext = _viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
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

    private void OnNewProfileClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.CreateNewProfile();
    }

    private void OnDeleteProfileClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string profileId)
        {
            _viewModel.RequestDeleteProfile(profileId);
        }
    }

    private void OnConfirmDeleteProfileClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.ConfirmDeleteProfile();
    }

    private void OnCancelDeleteProfileClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.CancelDeleteConfirmation();
    }

    private void OnClearAllSourcePortsClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.ClearAllSourcePorts();
    }

    private void OnRemoveSourcePortClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string path)
        {
            _viewModel.RemoveSourcePort(path);
        }
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

    private void OnClearAllIwadsClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.ClearAllIwads();
    }

    private void OnClearAllModsClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.ClearAllMods();
    }

    private void OnProfileRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount > 1 || IsFromInteractiveChild(e.Source))
        {
            return;
        }

        if (sender is Border border && border.Tag is string profileId)
        {
            _viewModel.ToggleProfileSelection(profileId);
            e.Handled = true;
        }
    }

    private void OnProfileRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (IsFromInteractiveChild(e.Source))
        {
            return;
        }

        if (sender is Border border && border.Tag is string profileId)
        {
            _viewModel.BeginRenameProfile(profileId);
            e.Handled = true;
        }
    }

    private void OnProfileRenameKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.Tag is not string profileId)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            _viewModel.CommitRename(profileId);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _viewModel.CancelRename();
            e.Handled = true;
        }
    }

    private void OnProfileRenameLostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.Tag is string profileId)
        {
            _viewModel.CommitRename(profileId);
        }
    }

    private void OnProfileRenameTextBoxLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        textBox.Focus();
        textBox.SelectAll();
    }

    private void OnSourcePortRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsFromInteractiveChild(e.Source))
        {
            return;
        }

        if (sender is Border border && border.Tag is string path)
        {
            _viewModel.ToggleSourcePortSelection(path);
            e.Handled = true;
        }
    }

    private void OnIwadRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsFromInteractiveChild(e.Source))
        {
            return;
        }

        if (sender is Border border && border.Tag is string path)
        {
            _viewModel.ToggleIwadSelection(path);
            e.Handled = true;
        }
    }

    private void OnModRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsFromInteractiveChild(e.Source))
        {
            return;
        }

        if (sender is Border border && border.Tag is string path)
        {
            _viewModel.ToggleModSelection(path);
            e.Handled = true;
        }
    }

    private void OnLaunchClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.LaunchSourcePort();
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

    private static bool IsFromInteractiveChild(object? source)
    {
        if (source is Button or TextBox)
        {
            return true;
        }

        if (source is not Avalonia.Visual visual)
        {
            return false;
        }

        foreach (var ancestor in visual.GetVisualAncestors())
        {
            if (ancestor is Button or TextBox)
            {
                return true;
            }
        }

        return false;
    }
}
