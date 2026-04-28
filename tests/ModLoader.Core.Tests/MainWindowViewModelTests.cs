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
}
