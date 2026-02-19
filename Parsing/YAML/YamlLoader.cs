using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using SKSSL.Extensions;
using VYaml.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static SKSSL.DustLogger;

namespace SKSSL.YAML;

/// <summary>
/// Load all entries in YAML files based on provided types in BULK. Caches data when
/// loading specific folders dedicated to a set of YAML files that homogeneously share a data type.
/// <example><code>
/// var types = new[] { typeof(YamlTypeA), typeof(YamlTypeB), typeof(YamlTypeC) };
/// var allData = YamlLoader.LoadAllTypes(types, path); // Supports ".../**/*.yaml"
/// var typeAs = allData[typeof(YamlTypeA)].Cast&lt;YamlTypeA&gt;();
/// var typeBs = allData[typeof(YamlTypeB)].Cast&lt;YamlTypeB&gt;();
/// // Files read only ONCE
/// </code></example>
/// <example><code>
/// var typeAs = YamlLoader.LoadAll&lt;YamlTypeA&gt;(path); // Supports ".../**/*.yaml"
/// var typeBs = YamlLoader.LoadAll&lt;YamlTypeB&gt;(path); // Uses cache
/// // Files read once per type, cached afterward
/// </code></example>
/// </summary>
public static partial class YamlLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    // Cache deserialized entries per type (optional, for repeated queries)
    private static readonly Dictionary<Type, object> _cache = new();

    private const string YamlFileExtension = "*.yml*";

    #region Serialization

    /// Serialize provided object and save to specific file path. Overrides existing file if present.
    public static void SerializeAndSave<T>(string path, T obj, bool @override = true) where T : class
    {
        var data = Serialize(obj);

        // Create directory if needed.
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // If override, write over. Otherwise, will create one if it doesn't exist.
        if (@override || !File.Exists(path))
            File.WriteAllText(path, data);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static string Serialize<T>(T obj) where T : class => YamlSerializer.SerializeToString(obj);

    #endregion

    #region Loading (Single)

    /// Loads a file containing more than one entry of type T. Entries are a consecutive list beginning with '-' each.
    public static async Task<List<T>> LoadFile<T>(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!File.Exists(path))
            throw new FileNotFoundException("File not found", path);

        try
        {
            await using FileStream stream = File.OpenRead(path);
            var sample = await YamlSerializer.DeserializeAsync<List<T>>(stream);
            return sample;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"YAML deserialization failed for {path}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Attempt to extract yaml data with limited defined types.
    /// Entries are expected to begin with "- type: example"
    /// </summary>
    public static Dictionary<Type, List<object>> LoadFileWithTags(string file, params Type[] expectedTypes)
    {
        Dictionary<Type, List<object>> results = new();
        string[] lines = File.ReadAllLines(file);
        var entries = SplitIntoYamlEntries(lines);

        foreach (var entryLines in entries)
        {
            // WARN: ExtractTypeTag() limits the parser to only one type per file.
            //  A file CANNOT have mixed types, despite that being the initial intention. This isn't super game-breaking,
            //  But it IS an issue.
            // Extract "- type:" tag
            string? typeTag = ExtractTypeTag(entryLines);
            if (typeTag == null) continue;
            // Strip any "Base...Yaml" [pre]/[suf]fixes.
            typeTag = StripBaseAndYaml(typeTag);

            // Find which known type matches the tag.
            Type? targetType = expectedTypes.FirstOrDefault
                (type => string.Equals(StripBaseAndYaml(type.Name), typeTag, StringComparison.OrdinalIgnoreCase));
            if (targetType == null) continue;

            string yamlBlock = string.Join("\n", entryLines);
            byte[] yamlBytes = Encoding.Default.GetBytes(yamlBlock);
            try
            {
                // Always deserialize as a list – handles single or multiple entries, with or without '-'
                Type typeOfList = typeof(List<>).MakeGenericType(targetType);
                if (!results.TryGetValue(targetType, out var deserializedEntries))
                {
                    deserializedEntries = [];
                    results.Add(targetType, deserializedEntries);
                }

                var output = Teast(targetType, yamlBytes);


                // Using yaml block, convert to Bytes.
                if (output == null) // Safety check.
                    throw new NullReferenceException(
                        "Output deserialized YAML data returned null from generic invocation!");

                // Make sure that the output is a list. It shall always be a list, even if theres one entry in there!
                var list = (IList)output;
                foreach (var item in list)
                    deserializedEntries.Add(item);
            }
            catch (Exception ex)
            {
                // Fallback attempt to deserialize individual item from block instead.
                try
                {
                    results[targetType].Add(Deserializer.Deserialize(yamlBlock, targetType)!);
                }
                catch (Exception innerEx)
                {
                    throw new InvalidOperationException(
                        "Failed to deserialize as either List<T> or single T.\n" +
                        "Input appears to be a YAML sequence (- item), so List<T> is usually required.\n" +
                        $"Inner error: {ex.Message}", innerEx);
                }

                Log($"Failed to deserialize {typeTag} in {file}: {ex.Message}", LOG.FILE_ERROR);
            }
        }

        return results;
    }

    private static IList Teast(Type type, byte[] yamlBytes)
    {
        var rawList = YamlSerializer.Deserialize<List<object>>(yamlBytes);
        var typedList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type))!;
        foreach (var obj in rawList)
        {
            if (obj is not Dictionary<object, object> dict) continue;

            // Create instance of targetType
            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor == null) throw new InvalidOperationException($"No parameterless constructor for {type}");

            var instance = ctor.Invoke(null);

            // Map entire yaml block as KVPs
            foreach (var yamlBlockInstance in dict)
            {
                // Get every property in target type
                foreach (PropertyInfo p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    // They should all have a yaml member attribute
                    var attr = p.GetCustomAttribute<VYaml.Annotations.YamlMemberAttribute>();
                    
                    // Ensure working on valid attribute and writable property
                    if (attr?.Name == null || !p.CanWrite)
                        continue;
                    
                    // Check for yaml attribute assigned "name", or that the property matches with the block key 
                    if (attr.Name.Equals(yamlBlockInstance.Key) || yamlBlockInstance.Key.Equals(p.Name.ToCamelCase()))
                    {
                        
                    }
                    p.SetValue(instance, Convert.ChangeType(yamlBlockInstance.Value, p.PropertyType));
                }
            }

            typedList.Add(instance);
        }


        return typedList;
    }

    #endregion

    #region Loading (Bulk)

    /// <summary>
    /// Loads YAML files from a folder, before invoking a post-process action on every entry.
    /// Presumes that all entries in a folder are of the same homogenous type.
    /// <code>
    /// #(In YAML)
    /// - entry
    /// - entry
    /// - ...
    /// </code>
    /// </summary>
    /// <returns>Enumerable amount of deserialized objects of type 'T'</returns>
    public static IEnumerable<T> LoadFolder<T>(string directory, Action<T>? postProcess = null)
    {
        if (!Directory.Exists(directory))
            yield break;

        var files = Directory.GetFiles(directory, YamlFileExtension, SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var items = LoadFile<T>(file).Result;
            foreach (T item in items)
            {
                postProcess?.Invoke(item);
                yield return item;
            }
        }
    }

    /// <summary>
    /// Loads all entries of type T from the given file patterns. Files are read once.
    /// </summary>
    public static List<T> PatternLoad<T>(params string[] patterns) where T : class
    {
        Type targetType = typeof(T);
        if (_cache.TryGetValue(targetType, out var cached))
            return (List<T>)cached;

        var files = GetFiles(patterns);
        var list = new List<T>();
        string expectedCore = StripBaseAndYaml(targetType.Name);

        foreach (var file in files)
        {
            string[] lines = File.ReadAllLines(file);
            var entries = SplitIntoYamlEntries(lines);

            foreach (var entryLines in entries)
            {
                string? typeTag = ExtractTypeTag(entryLines);
                if (typeTag == null
                    || !string.Equals(StripBaseAndYaml(typeTag), expectedCore, StringComparison.OrdinalIgnoreCase))
                    continue; // Short-circuit.

                // TODO: Convert this to VYaml parser instead of the Deserializer, here.
                string yamlBlock = string.Join("\n", entryLines);
                var obj = Deserializer.Deserialize<T>(yamlBlock);
                list.Add(obj);
            }
        }

        _cache[targetType] = list; // Cache for future calls
        return list.ToList(); // Return copy of list.
    }

    /// <summary>
    /// Gets all files in directory, searches every file, searches every in said file for provided type.
    /// If the first entry contains it, then an attempt to parse the entire file as a list is made.
    /// </summary>
    /// <param name="directory">Directory path to load.</param>
    /// <typeparam name="T">Entry type in files</typeparam>
    [Obsolete("Use an alternative Load method instead.")]
    public static IEnumerable<T> LoadDirectory<T>(string directory)
    {
        // Loop over every YAML file.
        var files = Directory.GetFiles(directory, searchPattern: YamlFileExtension, SearchOption.AllDirectories);
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

            var yaml = LoadFile<T>(file).Result;
            foreach (T entry in yaml)
                yield return entry;
        }
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
    public static Dictionary<TKey, TValue> LoadAsDictionary<TKey, TValue>(
        string folderOrFile, Func<TValue, TKey> keySelector, Action<TValue>? postProcess = null) where TKey : notnull
    {
        var dict = new Dictionary<TKey, TValue>();
        foreach (TValue item in LoadFolder(folderOrFile, postProcess))
        {
            TKey key = keySelector(item);
            dict[key] = item; // overwrite duplicates silently
        }

        return dict;
    }

    /// <summary>
    /// Searches a directory using provided type definitions and file patterns. Directory defaults to application's if
    /// not provided.
    /// </summary>
    public static Dictionary<Type, List<object>> LoadAll(
        Type[] types,
        string directory = "",
        params string[] patterns)
    {
        // Assign proper types to a new dictionary to organize all the different flavors of files.
        var results = types.ToDictionary(t => t, _ => new List<object>());

        // Get all yaml files.
        var files = GetFiles(patterns, directory);

        // Process every file with expected types.
        foreach (var file in files)
        {
            // Get file output and put to dictionary.
            var fileOutput = LoadFileWithTags(file, types);
            foreach (var data in fileOutput)
                results.Add(data.Key, data.Value);
        }

        return results;
    }

    /// <summary>
    /// Returns a distinct set of file paths matching the given patterns, optionally restricted to a base directory.
    /// </summary>
    /// <param name="patterns">File patterns (e.g., "*.cs", "src/**/*.txt", "logs/error.log")</param>
    /// <param name="directory">Optional base-directory to resolve relative patterns against. If null, uses current directory.</param>
    /// <returns>Distinct file paths (case-insensitive comparison on Windows)</returns>
    public static IEnumerable<string> GetFiles(IEnumerable<string> patterns, string directory = "")
    {
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Force base directory to current directory of application if none is provided.
        if (string.IsNullOrWhiteSpace(directory))
            directory = Directory.GetCurrentDirectory();

        foreach (var pattern in patterns)
        {
            string dir;
            string searchPattern;

            // If the pattern is an absolute path, use it directly
            if (Path.IsPathRooted(pattern))
            {
                dir = Path.GetDirectoryName(pattern) ?? directory;
                searchPattern = Path.GetFileName(pattern);
            }
            else
            {
                // Relative pattern: resolve against baseDirectory
                dir = Path.Combine(directory, Path.GetDirectoryName(pattern) ?? "");
                searchPattern = Path.GetFileName(pattern);
            }

            // Ensure the directory is normalized and exists
            if (Directory.Exists(dir))
                files.UnionWith(Directory.GetFiles(dir, searchPattern, SearchOption.AllDirectories));
        }

        return files;
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
    [Obsolete("VYaml handles mixed non-primitive types, so long as it is not recursive. Use LoadFile() if possible.")]
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

    #region YAML Data Parsing Helpers

    /// <summary>
    /// Remove "Base" from the beginning of a name, and "Yaml" from the end, if present.
    /// </summary>
    private static string StripBaseAndYaml(string name)
    {
        if (name.StartsWith("Base", StringComparison.OrdinalIgnoreCase))
            name = name[4..];
        if (name.EndsWith("Yaml", StringComparison.OrdinalIgnoreCase))
            name = name[..^4];
        return name;
    }

    private static string? ExtractTypeTag(string[] entryLines)
    {
        foreach (var line in entryLines)
        {
            Match match = RegexSpaceTypeBaseYaml().Match(line);
            return match.Success switch
            {
                false => null,
                true => match.Groups[2].Value // core name
            };
        }

        return null;
    }

    private static List<string[]> SplitIntoYamlEntries(string[] lines)
    {
        var entries = new List<string[]>();
        var current = new List<string>();

        foreach (var line in lines)
        {
            if (IsTopLevelEntryStart(line) && current.Count > 0)
            {
                entries.Add(current.ToArray());
                current.Clear();
            }

            current.Add(line);
        }

        if (current.Count > 0)
            entries.Add(current.ToArray());

        return entries;
    }

    private static bool IsTopLevelEntryStart(string line)
    {
        // The line must start with '-' at column 0 (only whitespace before is OK, but typically none)
        // Skip leading whitespace
        int i = 0;
        while (i < line.Length && char.IsWhiteSpace(line[i]))
            i++;

        // Must be exactly at the start (i == 0) and begin with '-', followed by space or end
        if (i >= line.Length || i != 0) return false; // Ensures that any indentation = not top-level

        if (line[i] != '-') return false;

        // Optional: require space after '-' (most common style)
        // Remove this check if you want to allow "-type: recipe" (no space)
        return i >= line.Length || char.IsWhiteSpace(line[i]);
    }

    #endregion

    [GeneratedRegex(@"\btype\s*:\s*(Base)?([A-Za-z0-9_]+)(Yaml)?\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex RegexSpaceTypeBaseYaml();
}