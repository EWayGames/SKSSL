using System.Diagnostics.CodeAnalysis;
// ReSharper disable UnusedType.Global

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace SKSSL;

public static class GamePathing
{
    private static readonly Dictionary<string, string> GAME_PATHS = new();
    public static readonly string GAME_ENVIRONMENT = AppContext.BaseDirectory;
    public static readonly string PROJECT_DIRECTORY = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", ".."));
    public static readonly string FOLDER_GAME = Path.Combine(GAME_ENVIRONMENT, "game");
    public static readonly string FOLDER_MODS = Path.Combine(GAME_ENVIRONMENT, "mods");
    public static readonly string FOLDER_LOCALE = Path.Combine("localization");

    /// <returns>Dedicated path to game files.</returns>
    public static string GPath(params string[] path)
    {
        var dynamicPath = GetPath("FOLDER_GAME");
        return string.IsNullOrEmpty(dynamicPath)
            ? Path.Combine(new[] { FOLDER_GAME }.Concat(path).ToArray())
            : dynamicPath;
    }

    /// <returns>A game-path explicitly for the "mods" folder.</returns>
    /// <seealso cref="GPath"/>
    public static string MPath(params string[] path)
    {
        var dynamicPath = GetPath("FOLDER_MODS");
        return string.IsNullOrEmpty(dynamicPath)
            ? Path.Combine(new[] { FOLDER_MODS }.Concat(path).ToArray())
            : dynamicPath;
    }

    public static string Proj(params string[] path)
    {
        var dynamicPath = GetPath("PROJECT_DIRECTORY");
        return string.IsNullOrEmpty(dynamicPath)
            ? Path.Combine(new[] { PROJECT_DIRECTORY }.Concat(path).ToArray())
            : dynamicPath;
    }
    
    private static string? GetPath(string id)
    {
        GAME_PATHS.TryGetValue(id, out var result);
        return result;
    }

    /// <summary>
    /// Initializes the game's two primary directories.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static void Initialize(params (string id, string path)[] paths)
    {
        foreach ((string id, string path) path in paths) GAME_PATHS[path.id] = path.path;
    }
}