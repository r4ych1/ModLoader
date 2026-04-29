using System;
using System.IO;
using System.Text.Json;

namespace ModLoader.Core;

public sealed class JsonLaunchInputsPersistence : ILaunchInputsPersistence
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _configPath;

    public JsonLaunchInputsPersistence(string configPath)
    {
        _configPath = configPath;
    }

    public LaunchInputsLoadResult Load()
    {
        if (!File.Exists(_configPath))
        {
            return new LaunchInputsLoadResult
            {
                State = LaunchInputsConfig.Empty
            };
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            var loadedConfig = JsonSerializer.Deserialize<LaunchInputsConfig>(json, SerializerOptions);
            if (loadedConfig is null)
            {
                return RecoverFromBrokenConfig();
            }

            return new LaunchInputsLoadResult
            {
                State = loadedConfig
            };
        }
        catch (JsonException)
        {
            return RecoverFromBrokenConfig();
        }
        catch (NotSupportedException)
        {
            return RecoverFromBrokenConfig();
        }
    }

    public void Save(LaunchInputsConfig config)
    {
        var directoryPath = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var stableConfig = new LaunchInputsConfig
        {
            SourcePortPath = config.SourcePortPath,
            Iwads = [.. (config.Iwads ?? [])],
            Mods = [.. (config.Mods ?? [])],
            SelectedIwadPath = config.SelectedIwadPath,
            SelectedModPaths = [.. (config.SelectedModPaths ?? [])]
        };

        var json = JsonSerializer.Serialize(stableConfig, SerializerOptions);
        File.WriteAllText(_configPath, json);
    }

    private LaunchInputsLoadResult RecoverFromBrokenConfig()
    {
        var backupPath = CreateUniqueBackupPath();
        File.Move(_configPath, backupPath);
        Save(LaunchInputsConfig.Empty);

        return new LaunchInputsLoadResult
        {
            State = LaunchInputsConfig.Empty,
            HadLoadWarning = true,
            WarningMessage = "Config file was invalid and was reset to an empty state.",
            HadRemediationAction = true
        };
    }

    private string CreateUniqueBackupPath()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var baseBackupPath = $"{_configPath}.broken.{timestamp}";
        if (!File.Exists(baseBackupPath))
        {
            return baseBackupPath;
        }

        var suffix = 1;
        while (true)
        {
            var candidate = $"{baseBackupPath}-{suffix}";
            if (!File.Exists(candidate))
            {
                return candidate;
            }

            suffix++;
        }
    }
}
