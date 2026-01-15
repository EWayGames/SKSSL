namespace SKSSL;

public abstract class BaseGameData
{
    /// <summary>
    /// Load all game data based on multiple game paths.
    /// </summary>
    /// <param name="paths"></param>
    public void Load(string[] paths)
    {
        foreach (var path in paths) Load(path);
    }

    /// <summary>
    /// Load game data from singular path.
    /// </summary>
    /// <param name="gamePath"></param>
    public abstract void Load(string gamePath);
}