using System.Reflection;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// ReSharper disable UnusedType.Global

namespace SKSSL.YAML;

/// <summary>
/// Solely handles the deserialization and loading of Y[A]ML file data. Processing is up to whatever calls load.
/// According to the code, this expects 100% peak perfect YAML matchups with the provided type. Modding will be great,
/// except when there is a change to the structure.
/// </summary>
public static class YamlLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private const string YamlFileExtension = "*.yml*";

    #region Saving

    public static void SerializeAndSave(string path, object obj)
    {
        var data = Serialize(obj);
        Save(path, data);
    }
    
    public static string Serialize(object obj)
    {
        using var writer = new StringWriter();
        Serializer.Serialize(writer, obj);
        string yaml = writer.ToString();
        return yaml;
    }

    /// <summary>
    /// Deletes file at provided path and writes contents.
    /// </summary>
    public static void Save(string path, string contents)
    {
        if (File.Exists(path))
            File.Delete(path);
        File.WriteAllText(path, contents);
    }

    #endregion
    
    #region Loading

    /// <summary>
    /// Loads YAML files from a folder or a single file and returns a list of deserialized objects.
    /// This requires the user to know precisely what type is in which folder.
    /// <code>
    /// #(In YAML)
    /// - entry
    /// - entry
    /// - ...
    /// </code>
    /// </summary>
    public static IEnumerable<T> Load<T>(string folderOrFile, Action<T>? postProcess = null)
    {
        var files = Directory.Exists(folderOrFile)
            ? Directory.GetFiles(folderOrFile, YamlFileExtension, SearchOption.AllDirectories)
            : [folderOrFile];

        foreach (var file in files)
        {
            using var reader = new StreamReader(file);
            var items = Deserializer.Deserialize<List<T>>(reader);
            foreach (T item in items)
            {
                postProcess?.Invoke(item);
                yield return item;
            }
        }
    }

    /// <summary>
    /// Loads a directory, searches every file and every entry in said files for a provided type— and if the
    /// first entry contains it —attempts to parse the entire file. Honestly, this isn't very useful, and got
    /// instantly replaced by the <see cref="YamlBulkLoader"/>
    /// <seealso cref="YamlBulkLoader"/>
    /// </summary>
    /// <param name="directory"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IEnumerable<T> LoadFolderPicky<T>(string directory)
    {
        // Loop over every YAML file.
        var files = Directory.GetFiles(directory, searchPattern: YamlFileExtension, SearchOption.AllDirectories);
        var list = new List<T>();

        foreach (var file in files)
        {
            string expectedTypeName = typeof(T).Name;

            // Regex breakdown:
            // .*?                  -> any characters (non-greedy) before "type"
            // type\s*:\s*          -> the keyword "type" (case-insensitive), colon, optional whitespace
            // (Base)?              -> optional "Base" prefix
            // ([A-Za-z0-9_]+)      -> capture group 2: the core type name (alphanumeric + _)
            // (Yaml)?              -> optional "Yaml" suffix
            // .*                   -> any characters after (we don't care)
            var regex = new Regex(
                @".*?type\s*:\s*(Base)?([A-Za-z0-9_]+)(Yaml)?.*",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            bool typeFound = false;
            using var reader = new StreamReader(file);
            while (reader.ReadLine() is { } line)
            {
                Match hasType = regex.Match(line);
                if (!hasType.Success)
                    continue;
                string extractedTypeName = hasType.Groups[1].Value;

                if (!string.Equals(extractedTypeName, expectedTypeName, StringComparison.OrdinalIgnoreCase)) continue;
                typeFound = true;
                break;
            }

            if (!typeFound)
                continue;

            var yaml = Load<T>(file);
            foreach (T entry in yaml) list.Add(entry);
        }

        return list;
    }

    /// <summary>
    /// Loads YAML into a dictionary keyed by a provided ID selector.
    /// <code>
    /// #(In YAML)
    /// list_name:
    ///   - entry
    ///   - entry
    ///   - ...
    /// </code>
    /// </summary>
    public static Dictionary<TKey, TValue> LoadDictionary<TKey, TValue>(
        string folderOrFile,
        Func<TValue, TKey> keySelector,
        Action<TValue>? postProcess = null) where TKey : notnull
    {
        var dict = new Dictionary<TKey, TValue>();
        foreach (TValue item in Load(folderOrFile, postProcess))
        {
            TKey key = keySelector(item);
            dict[key] = item; // overwrite duplicates silently
        }

        return dict;
    }

    /// <summary>
    /// Handles the dynamic loading of Type1 and Type2 types, which the latter may reference back to Type1.
    /// Both instances must begin with the "type" keyword, annotated by "typeAnnoX".
    /// <code>
    /// #(In YAML)
    /// - group:
    ///   id: group_name
    ///   entries:
    ///       - entry
    ///       - entry
    /// # Mixed but Related Entries
    /// - entry:
    ///   group: group_name
    /// </code>
    /// </summary>
    /// <param name="handleFunction"/>
    /// <param name="yamlText">Filepath to the text being parsed.</param>
    /// <param name="typeAnno1">Type annotation for group entry. (Ex: racial_group)</param>
    /// <param name="typeAnno2">Type annotation for subversive entry. (Ex: race)</param>
    /// <param name="typeAnno2Plural">Plural of type2 annotation subversive entry. (Ex: raceS)</param>
    /// <typeparam name="Type1">Contains a list of Type2.</typeparam>
    /// <typeparam name="Type2">Has a pointer to Type1, but is isolated in its own instances.</typeparam>
    public static void LoadMixedContainers<Type1, Type2>(
        string yamlText, string typeAnno1, string typeAnno2, string typeAnno2Plural,
        Action<Type2, Type1?> handleFunction)
        where Type1 : class
        where Type2 : class
    {
        var entries = Deserializer.Deserialize<List<Dictionary<string, object>>>(yamlText);
        foreach (var entry in entries)
        {
            if (!entry.TryGetValue("type", out var typeObj))
                continue;

            var type = typeObj.ToString();

            switch (type)
            {
                // (Example: if racial group)
                case var _ when type == typeAnno1:
                {
                    // Serialize dictionary to YAML string first
                    string yamlFragment = Serializer.Serialize(entry);
                    var group = Deserializer.Deserialize<Type1>(yamlFragment);

                    // Get contained instances
                    PropertyInfo? prop = typeof(Type1).GetProperty(typeAnno2Plural,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                    // Handling property without concern for case.
                    if (prop != null && prop.GetValue(group) is IEnumerable<Type2> type2InstancesProp)
                        foreach (Type2 type2Instance in type2InstancesProp)
                            handleFunction(type2Instance, group);

                    break;
                }
                // (Example: if race entry)
                case var _ when type == typeAnno2:
                {
                    string yamlFragment = Serializer.Serialize(entry);
                    var type2Instance = Deserializer.Deserialize<Type2>(yamlFragment);

                    // Run function but exclaim that the thing meant to contain it, is null.
                    handleFunction(type2Instance, null);
                    break;
                }
            }
        }
    }

    #endregion
}