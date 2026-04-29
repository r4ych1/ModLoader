using ModLoader.App;

namespace ModLoader.Core.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void ClearSourcePort_ClearsOnlySourcePort()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod = temp.CreateFile("smoothdoom.pk3");

        var viewModel = new MainWindowViewModel();
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

        var viewModel = new MainWindowViewModel();
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

        var viewModel = new MainWindowViewModel();
        viewModel.ProcessIwadDrop([iwad1, iwad2]);

        viewModel.ToggleIwadSelection(iwad1);
        Assert.Equal(Path.GetFullPath(iwad1), viewModel.SelectedIwadPath);
        Assert.True(viewModel.IwadRows.First(row => row.Path == Path.GetFullPath(iwad1)).IsSelected);

        viewModel.ToggleIwadSelection(iwad2);
        Assert.Equal(Path.GetFullPath(iwad2), viewModel.SelectedIwadPath);
        Assert.True(viewModel.IwadRows.First(row => row.Path == Path.GetFullPath(iwad2)).IsSelected);
        Assert.False(viewModel.IwadRows.First(row => row.Path == Path.GetFullPath(iwad1)).IsSelected);

        viewModel.ToggleIwadSelection(iwad2);
        Assert.Null(viewModel.SelectedIwadPath);
        Assert.DoesNotContain(viewModel.IwadRows, row => row.IsSelected);
    }

    [Fact]
    public void ToggleModSelection_MultiSelectAndReselectToDeselectSingleRow()
    {
        using var temp = new TempDirectory();
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pk3");
        var mod3 = temp.CreateFile("mod-c.pk3");

        var viewModel = new MainWindowViewModel();
        viewModel.ProcessModDrop([mod1, mod2, mod3]);

        viewModel.ToggleModSelection(mod1);
        viewModel.ToggleModSelection(mod2);

        Assert.Equal(2, viewModel.SelectedModPaths.Count);
        Assert.Contains(Path.GetFullPath(mod1), viewModel.SelectedModPaths, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(Path.GetFullPath(mod2), viewModel.SelectedModPaths, StringComparer.OrdinalIgnoreCase);
        Assert.True(viewModel.ModRows.First(row => row.Path == Path.GetFullPath(mod1)).IsSelected);
        Assert.True(viewModel.ModRows.First(row => row.Path == Path.GetFullPath(mod2)).IsSelected);

        viewModel.ToggleModSelection(mod1);

        Assert.Single(viewModel.SelectedModPaths);
        Assert.DoesNotContain(Path.GetFullPath(mod1), viewModel.SelectedModPaths, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(Path.GetFullPath(mod2), viewModel.SelectedModPaths, StringComparer.OrdinalIgnoreCase);
        Assert.False(viewModel.ModRows.First(row => row.Path == Path.GetFullPath(mod1)).IsSelected);
        Assert.True(viewModel.ModRows.First(row => row.Path == Path.GetFullPath(mod2)).IsSelected);
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

        var viewModel = new MainWindowViewModel();
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
}
