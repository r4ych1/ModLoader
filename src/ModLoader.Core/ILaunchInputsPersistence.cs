namespace ModLoader.Core;

public interface ILaunchInputsPersistence
{
    LaunchInputsLoadResult Load();

    void Save(LaunchInputsConfig config);
}
