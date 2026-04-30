using System.Collections.Generic;
using System.Diagnostics;

namespace ModLoader.App;

public interface ISourcePortLauncher
{
    void Launch(string executablePath, IReadOnlyList<string> arguments);
}

public sealed class ProcessSourcePortLauncher : ISourcePortLauncher
{
    public void Launch(string executablePath, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var process = Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException("Process start returned no process instance.");
        }
    }
}
