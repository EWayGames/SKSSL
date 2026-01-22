using System.Text.Json;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

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

    /// <summary>
    /// Loads json file at path and returns a list of objects, even if there is only one entry.
    /// </summary>
    public static List<T> LoadAmbiguous<T>(string path)
    {
        var json = File.ReadAllText(path);
        using JsonDocument doc = JsonDocument.Parse(json);
        List<T> objects = [];
        switch (doc.RootElement.ValueKind)
        {
            case JsonValueKind.Array:
            {
                var list = Load<List<T>>(json);
                if (list != null)
                    objects = list;
                break;
            }
            case JsonValueKind.Object:
            {
                var entry = Load<T>(json);
                if (entry != null)
                    objects.Add(entry);
                break;
            }
        }

        return objects;
    }

    /// Load multiple files containing identical types in a directory.
    /// Expects all files in the directory to be the same general type.
    public static List<T> LoadDirectory<T>(string directory)
    {
        var files = Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories);
        List<T> result = [];
        foreach (var file in files)
        {
            var obj = LoadAmbiguous<T>(file);
            result.AddRange(obj);
        }
        return result;
    }

    /// Load data from individual file with expected Type T.
    /// If the file contains multiple entries and T is NOT a List of type T, it will not function.
    /// Use <see cref="LoadAmbiguous{T}"/> instead.
    public static T? Load<T>(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json);
    }
}