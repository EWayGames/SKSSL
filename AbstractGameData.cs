using SKSSL.Scenes;

namespace SKSSL;

using static DustLogger;

public abstract class AbstractGameData
{
    /// <summary>
    /// Load all game data based on multiple game paths.
    /// </summary>
    /// <param name="paths"></param>
    public void Load(string[] paths)
    {
        foreach (var path in paths)
        {
            if (!string.IsNullOrEmpty(path))
                Load(path);
            else
                Log($"Missing filepath in LoadContent() call in {nameof(AbstractGameData)}", LOG.SYSTEM_ERROR);
        }
    }

    /// <summary>
    /// Load game data from singular path.
    /// </summary>
    /// <param name="gamePath"></param>
    public abstract void Load(string gamePath);

    /// <summary>
    /// Additional optional handling once all game data has been loaded. Automatically called AFTER <see cref="SSLGame"/> LoadContent(); 
    /// </summary>
    /// <remarks>Will do nothing on its own. Developer must implement additional post-loading if desired.</remarks>
    public virtual void PostLoad()
    {
    }
}