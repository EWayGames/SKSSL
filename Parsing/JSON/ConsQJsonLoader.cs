using System.Text.Json;

namespace SKSSL.Parsing.JSON;

/// <summary>
/// Lifted from "Consequaintances", this is an incredibly simple loader for Json files
/// using system <see cref="System.Text.Json"/>.
/// </summary>
public class ConsQJsonLoader
{
    /// Save object as JSON.
    public static void Save<T>(string path, T obj)
    {
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    /// Load multiple files containing identical types in a directory.
    public static List<T> LoadDirectory<T>(string directory)
    {
        var files = Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories);
        List<T> result = [];
        foreach (var file in files)
        {
            var obj = Load<T>(file);
            result.AddRange(obj);
        }
        return result;
    }
    
    /// 
    public static T Load<T>(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json)!;
    }
}