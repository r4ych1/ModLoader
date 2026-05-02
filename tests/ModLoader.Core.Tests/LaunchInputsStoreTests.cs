using ModLoader.Core;

namespace ModLoader.Core.Tests;

public sealed class LaunchInputsStoreTests
{
    [Fact]
    public void ProcessSourcePortDrop_AcceptsOnlyExe_AndAppendsUniqueSourcePortsInOrder()
    {
        using var temp = new TempDirectory();
        var txt = temp.CreateFile("readme.txt");
        var exe1 = temp.CreateFile("gzdoom.exe");
        var wad = temp.CreateFile("doom.wad");
        var exe2 = temp.CreateFile("vkdoom.exe");
        var exe1DifferentCase = exe1.ToUpperInvariant();

        var store = new LaunchInputsStore();
        store.ProcessSourcePortDrop([txt, exe1, wad, exe2, exe1DifferentCase]);

        Assert.Equal(
            [Path.GetFullPath(exe1), Path.GetFullPath(exe2)],
            store.SourcePorts.ToArray());
    }

    [Fact]
    public void ProcessSourcePortDrop_SameFileNameInDifferentDirectories_AddsBothFullPaths()
    {
        using var temp = new TempDirectory();
        var dirA = Directory.CreateDirectory(Path.Combine(temp.Path, "A"));
        var dirB = Directory.CreateDirectory(Path.Combine(temp.Path, "B"));
        var exeA = Path.Combine(dirA.FullName, "gzdoom.exe");
        var exeB = Path.Combine(dirB.FullName, "gzdoom.exe");
        File.WriteAllText(exeA, string.Empty);
        File.WriteAllText(exeB, string.Empty);
        var invalid = Path.Combine(dirB.FullName, "gzdoom.txt");
        File.WriteAllText(invalid, string.Empty);

        var store = new LaunchInputsStore();
        store.ProcessSourcePortDrop([exeA, invalid, exeB]);

        Assert.Equal(
            [Path.GetFullPath(exeA), Path.GetFullPath(exeB)],
            store.SourcePorts.ToArray());
    }

    [Fact]
    public void ProcessIwadDrop_AcceptsAllowlistedExtensions_AndDedupesByNormalizedPath()
    {
        using var temp = new TempDirectory();
        var iwad1 = temp.CreateFile("doom.wad");
        var iwad1DifferentCase = iwad1.ToUpperInvariant();
        var iwad2 = temp.CreateFile("doom2.ipk7");
        var invalid = temp.CreateFile("notes.txt");

        var store = new LaunchInputsStore();
        store.ProcessIwadDrop([iwad1, iwad1DifferentCase, invalid, iwad2]);

        Assert.Equal(
            [Path.GetFullPath(iwad1), Path.GetFullPath(iwad2)],
            store.Iwads.ToArray());
    }

    [Fact]
    public void ProcessIwadDrop_DirectoryInput_OnlyScansTopLevelFiles()
    {
        using var temp = new TempDirectory();
        var topLevelValid = temp.CreateFile("top.iwad");
        temp.CreateFile("top.txt");
        var nestedDir = Directory.CreateDirectory(Path.Combine(temp.Path, "nested"));
        File.WriteAllText(Path.Combine(nestedDir.FullName, "nested.pk3"), string.Empty);

        var store = new LaunchInputsStore();
        store.ProcessIwadDrop([temp.Path]);

        Assert.Single(store.Iwads);
        Assert.Equal(Path.GetFullPath(topLevelValid), store.Iwads[0]);
        Assert.DoesNotContain(Path.GetFullPath(Path.Combine(nestedDir.FullName, "nested.pk3")), store.Iwads, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProcessModDrop_AcceptsAllowlistedExtensionsIncludingZip_AndDedupesByNormalizedPath()
    {
        using var temp = new TempDirectory();
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod1CaseVariant = mod1.ToUpperInvariant();
        var mod2 = temp.CreateFile("mod-b.pkz");
        var mod3 = temp.CreateFile("mod-c.zip");
        var invalid = temp.CreateFile("mod-d.txt");

        var store = new LaunchInputsStore();
        store.ProcessModDrop([mod1, mod1CaseVariant, invalid, mod2, mod3]);

        Assert.Equal(
            [Path.GetFullPath(mod1), Path.GetFullPath(mod2), Path.GetFullPath(mod3)],
            store.Mods.ToArray());
    }

    [Fact]
    public void ProcessModDrop_DirectoryInput_DoesNotAddDirectoryPath_AndScansTopLevelOnly()
    {
        using var temp = new TempDirectory();
        temp.CreateFile("alpha.pk3");
        temp.CreateFile("beta.pwad");
        temp.CreateFile("gamma.zip");
        temp.CreateFile("ignore.txt");

        var nestedDir = Directory.CreateDirectory(Path.Combine(temp.Path, "nested"));
        File.WriteAllText(Path.Combine(nestedDir.FullName, "nested.pk3"), string.Empty);

        var expectedOrder = Directory.EnumerateFiles(temp.Path, "*", SearchOption.TopDirectoryOnly)
            .Where(IsValidMod)
            .Select(Path.GetFullPath)
            .ToArray();

        var store = new LaunchInputsStore();
        store.ProcessModDrop([temp.Path]);

        Assert.Equal(expectedOrder, store.Mods.ToArray());
        Assert.DoesNotContain(Path.GetFullPath(temp.Path), store.Mods, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(Path.GetFullPath(Path.Combine(nestedDir.FullName, "nested.pk3")), store.Mods, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClearIwads_EmptiesOnlyIwadList()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad1 = temp.CreateFile("doom1.wad");
        var iwad2 = temp.CreateFile("doom2.iwad");
        var mod = temp.CreateFile("mod-a.pk3");

        var store = new LaunchInputsStore(new LaunchInputsConfig
        {
            SourcePorts = [source],
            Iwads = [iwad1, iwad2],
            Mods = [mod]
        });

        store.ClearIwads();

        Assert.Equal([Path.GetFullPath(source)], store.SourcePorts.ToArray());
        Assert.Empty(store.Iwads);
        Assert.Equal([Path.GetFullPath(mod)], store.Mods.ToArray());
    }

    [Fact]
    public void ClearMods_EmptiesOnlyModList()
    {
        using var temp = new TempDirectory();
        var source = temp.CreateFile("gzdoom.exe");
        var iwad = temp.CreateFile("doom2.wad");
        var mod1 = temp.CreateFile("mod-a.pk3");
        var mod2 = temp.CreateFile("mod-b.pkz");

        var store = new LaunchInputsStore(new LaunchInputsConfig
        {
            SourcePorts = [source],
            Iwads = [iwad],
            Mods = [mod1, mod2]
        });

        store.ClearMods();

        Assert.Equal([Path.GetFullPath(source)], store.SourcePorts.ToArray());
        Assert.Equal([Path.GetFullPath(iwad)], store.Iwads.ToArray());
        Assert.Empty(store.Mods);
    }

    private static bool IsValidMod(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".wad", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".pwad", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".pk3", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".pk7", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".ipk3", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".ipk7", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".pkz", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromConfig_AndRemoveMissingPaths_RemovesMissingEntriesAndPreservesOrder()
    {
        using var temp = new TempDirectory();
        var existingSource = temp.CreateFile("gzdoom.exe");
        var missingSource = Path.Combine(temp.Path, "missing.exe");
        var existingIwad = temp.CreateFile("doom.wad");
        var missingIwad = Path.Combine(temp.Path, "missing.wad");
        var existingMod = temp.CreateFile("mod-a.pk3");
        var missingMod = Path.Combine(temp.Path, "missing.pk3");

        var store = new LaunchInputsStore(new LaunchInputsConfig
        {
            SourcePorts = [existingSource, missingSource],
            Iwads = [existingIwad, missingIwad],
            Mods = [existingMod, missingMod]
        });

        var changed = store.RemoveMissingPaths();

        Assert.True(changed);
        Assert.Equal([Path.GetFullPath(existingSource)], store.SourcePorts.ToArray());
        Assert.Equal([Path.GetFullPath(existingIwad)], store.Iwads.ToArray());
        Assert.Equal([Path.GetFullPath(existingMod)], store.Mods.ToArray());
    }

    [Fact]
    public void ReorderModsBySelectionSequence_SelectedModsMoveToTopInSelectionOrder()
    {
        using var temp = new TempDirectory();
        var modA = temp.CreateFile("a.pk3");
        var modB = temp.CreateFile("b.pk3");
        var modC = temp.CreateFile("c.pk3");
        var modD = temp.CreateFile("d.pk3");

        var store = new LaunchInputsStore(new LaunchInputsConfig
        {
            Mods = [modA, modB, modC, modD]
        });

        var changed = store.ReorderModsBySelectionSequence([modC, modA]);

        Assert.True(changed);
        Assert.Equal(
            [Path.GetFullPath(modC), Path.GetFullPath(modA), Path.GetFullPath(modB), Path.GetFullPath(modD)],
            store.Mods.ToArray());
    }

    [Fact]
    public void ReorderModsBySelectionSequence_IgnoresMissingOrDuplicateSelections()
    {
        using var temp = new TempDirectory();
        var modA = temp.CreateFile("a.pk3");
        var modB = temp.CreateFile("b.pk3");
        var modC = temp.CreateFile("c.pk3");
        var missing = Path.Combine(temp.Path, "missing.pk3");

        var store = new LaunchInputsStore(new LaunchInputsConfig
        {
            Mods = [modA, modB, modC]
        });

        var changed = store.ReorderModsBySelectionSequence([missing, modB, modB.ToUpperInvariant()]);

        Assert.True(changed);
        Assert.Equal(
            [Path.GetFullPath(modB), Path.GetFullPath(modA), Path.GetFullPath(modC)],
            store.Mods.ToArray());
    }
}
