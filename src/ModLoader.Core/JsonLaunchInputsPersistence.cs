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

            loadedConfig = ApplyLegacySourcePortCompatibility(loadedConfig, json);

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
            SourcePorts = [.. (config.SourcePorts ?? [])],
            SelectedSourcePortPath = config.SelectedSourcePortPath,
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

    private static LaunchInputsConfig ApplyLegacySourcePortCompatibility(LaunchInputsConfig loadedConfig, string json)
    {
        if (loadedConfig.SourcePorts.Count > 0 || !string.IsNullOrWhiteSpace(loadedConfig.SelectedSourcePortPath))
        {
            return loadedConfig;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.TryGetProperty("SourcePorts", out _))
            {
                return loadedConfig;
            }

            if (!root.TryGetProperty("SourcePortPath", out var legacySourcePortElement))
            {
                return loadedConfig;
            }

            var legacySourcePortPath = legacySourcePortElement.GetString();
            if (string.IsNullOrWhiteSpace(legacySourcePortPath))
            {
                return loadedConfig;
            }

            return new LaunchInputsConfig
            {
                SourcePorts = [legacySourcePortPath],
                SelectedSourcePortPath = legacySourcePortPath,
                Iwads = [.. loadedConfig.Iwads],
                Mods = [.. loadedConfig.Mods],
                SelectedIwadPath = loadedConfig.SelectedIwadPath,
                SelectedModPaths = [.. loadedConfig.SelectedModPaths]
            };
        }
        catch (JsonException)
        {
            return loadedConfig;
        }
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
