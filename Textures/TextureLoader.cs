using System.Collections.Concurrent;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SKSSL.Utilities;
using static SKSSL.DustLogger;
using static SKSSL.Textures.TextureLoader.MaterialRegistry;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace SKSSL.Textures;

/// <summary>
/// Supported texture-types in the system. Defaults to <see cref="DIFFUSE"/>.
/// </summary>
/// <remarks>
/// This will not inherently do anything besides permit additional map types. Rendering must be implemented separately.
/// </remarks>
public enum TextureType : byte
{
    /// Plain color information.
    DIFFUSE = 0,

    /// Normal-data.
    NORMAL = 1,

    /// Height data.
    DISPLACEMENT = 2,

    /// Glow data.
    EMISSIVE = 3,

    // Unused as of 20260106
    //GLOSSY,
}

// Default implementation
public class BlankTextureLoader : TextureLoader
{
    protected override void CustomInitializeRegistries() =>
        throw new NotImplementedException(
            "Developer(s) MUST implement custom Registries Initialization, as registries may vary between projects.");

    protected override void CustomOptionalLoad(string input)
    {
    }
}

/// <summary>
/// Generic texture loader for all game asset categories (objects, items, UI, etc.).
/// Supports multi-texture maps (diffuse + normal + etc.) and automatic error texture fallback.
/// <br/><br/>
/// <see cref="CustomInitializeRegistries"/> MUST be filled-out per-implementation based on the
/// developer requirements / layout of the project.
/// </summary>
public abstract partial class TextureLoader
{
    // Default implementation
    private static TextureLoader _instance = new BlankTextureLoader();

    // Allow override (e.g., for mods or tests)
    public static TextureLoader Instance
    {
        get => _instance;
        set => _instance = value ?? throw new ArgumentNullException(nameof(value));
    }

    private static GraphicsDevice _graphicsDevice { get; set; } = null!;

    /// <summary>
    /// Reference to Monogame's content manager for "base game" content.
    /// </summary>
    private static ContentManager _vanillaContent = null!;

    /// <summary>
    /// Mod Root folders. Operated upon with priority to recent "lower" mods over "older" ones. 
    /// </summary>
    private static IEnumerable<string> _modFolders = null!;

    private static readonly Dictionary<string, Texture2D> _cache = new();

    #region Initialization

    private static bool IsInitialized { get; set; } = false;

    /// <summary>
    /// Allows the developer to pre-initialize a custom loader for the game, assuming it is on the surface-level of
    /// game initialization and before base.Initialize() is called in the game's Initialize() method.
    /// <see cref="TextureLoader"/> instance may be provided to override the <see cref="BlankTextureLoader"/>.
    /// </summary>
    /// <param name="alternativeLoader"></param>
    public static void PreInitializeLoader(TextureLoader? alternativeLoader = null)
    {
        _instance = alternativeLoader ?? new BlankTextureLoader();
    }

    /// <summary>
    /// Initializes texture loaded. An alternative version of the loaded with a custom implement for
    /// <br/><br/>
    /// It is IMPERATIVE that this be loaded before the base.Initialize() of the game's Initialize() method.
    /// </summary>
    /// <param name="vanillaContent">Monogame content manager for "Vanilla' game content.</param>
    /// <param name="graphicsDevice">Game's graphic device for rendering.</param>
    /// <param name="modFolders">All the root-level mod directory paths.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void Initialize(ContentManager vanillaContent, GraphicsDevice graphicsDevice, List<string> modFolders)
    {
        // If the texture loader has already been initialized by a "surface-level" class override,
        //  then that override is the one that shall be used and whatever is needed has already been initialized.
        if (IsInitialized)
            return;

        // Load Custom Registries.
        _instance.CustomInitializeRegistries();

        _modFolders = modFolders;
        _vanillaContent = vanillaContent ?? throw new ArgumentNullException(nameof(vanillaContent));
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        IsInitialized = true;
    }

    #endregion

    // The "static" method — but delegates to instance

    #region Get Raw Images

    /// <summary>
    /// Loads a provided asset name as a <see cref="Texture2D"/>.
    /// Assumes the mod folders within the TextureLoader are all texture folders for each mod.
    /// Use <see cref="GetTexture"/> for external use.
    /// </summary>
    /// <param name="assetName">Name of the provided asset without extension. (e.g. "Textures/PlayerSprite")</param>
    /// <returns>Texture asset or Default Error Texture, instead.</returns>
    private static Texture2D Load(string assetName)
    {
        // Check cache first.
        if (_cache.TryGetValue(assetName, out Texture2D? cached))
            return cached;

        Texture2D? texture;

        // TODO: Add <mod_name>:<asset_name> support.

        // Check mods for raw override
        // This makes sure that mod assets are loaded -before- vanilla assets.
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_modFolders is not null && _modFolders.Any())
        {
            foreach (var modFolder in _modFolders.Reverse()) // Reverse() last-mod-wins priority
            {
                string modPath = Path.Combine(modFolder, assetName + ".png");
                if (!File.Exists(modPath))
                    continue; // Short-circuit.
                using FileStream stream = File.OpenRead(modPath);
                texture = Texture2D.FromStream(_graphicsDevice, stream);
                _cache[assetName] = texture;
                return texture;
            }
        }

        // Try vanilla pipeline load (falls back if no .xnb exists)
        try
        {
            texture = _vanillaContent.Load<Texture2D>(assetName);
            _cache[assetName] = texture;
            return texture;
        }
        catch (ContentLoadException)
        {
        } // Expected if no vanilla asset

        Log($"Image texture not found: {assetName}. Defaulting to error texture instead.",
            LOG.FILE_WARNING);
        return HardcodedTextures.GetErrorTexture();
    }

    #endregion

    /// <summary>
    /// Custom method for initializing dedicated registries. Absolutely required per-project.
    /// </summary>
    protected abstract void CustomInitializeRegistries();

    /// <summary>
    /// Custom method for loading. This is additional optional logic that the developer may choose to implement.
    /// Though all instantiated inheritors of <see cref="TextureLoader"/> require this, the developer is NOT
    /// required to add any code.
    /// </summary>
    protected abstract void CustomOptionalLoad(string input);

    // Generic storage: category → texture name → texture object
    private static readonly
        ConcurrentDictionary<string, Dictionary<string, Texture2D>> _simpleTextures = new();

    private static readonly Dictionary<string, TextureCategoryConfig> _categories = new();

    /// <summary>
    /// Register a new texture category (e.g., objects, items).
    /// </summary>
    public static void RegisterCategory(string categoryName, TextureCategoryConfig config)
    {
        _categories[categoryName] = config;

        // Material mapping is now handled in the Material Registry.
        if (!config.IsMultiTextureMap)
            _simpleTextures[categoryName] = new Dictionary<string, Texture2D>();
    }

    /// <summary>
    /// Get read-only dictionary for a category.
    /// </summary>
    public static IReadOnlyDictionary<string, TTexture> GetCategory<TTexture>(string categoryName)
    {
        if (_simpleTextures.TryGetValue(categoryName, out var dict))
        {
            return (IReadOnlyDictionary<string, TTexture>)dict;
        }

        return new Dictionary<string, TTexture>().AsReadOnly();
    }

    /// <summary>
    /// Ambiguous current directory, which may be a game or mod directory.
    /// </summary>
    private static string _currentDirectory = string.Empty;

    /// <summary>
    /// Load all registered texture categories.
    /// </summary>
    public static void LoadAll(string currentDirectory)
    {
        _currentDirectory = currentDirectory;
        _instance.CustomOptionalLoad(currentDirectory);
        foreach ((string categoryName, TextureCategoryConfig config) in _categories)
            LoadCategory(categoryName, config);
    }

    private static void LoadCategory(string categoryName, TextureCategoryConfig config)
    {
        // Database for specific category, such as "Items" or "Entities", etc.
        if (config.IsMultiTextureMap)
            LoadMaterialTextureCategory(config);
        else
            LoadSingleTextureCategory(categoryName, config);
    }

    private static void LoadSingleTextureCategory(string categoryName, TextureCategoryConfig config)
    {
        string dir = _currentDirectory;
        if (config.AssetPathKey != null)
            dir = Path.Combine(_currentDirectory, config.AssetPathKey);

        var files = GameLoader.GetGameFiles(dir);

        Dictionary<string, Texture2D> flatTextures = new();

        foreach (var file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            string key = config.KeyTransform?.Invoke(fileName, file) ?? fileName.ToLower();

            // Error Reporting & Texture is automatically handled in the Load() call.
            Texture2D texture = Load(fileName);

            flatTextures[key] = texture;
        }

        // Using KeyValuePair directly for single-entries. Treating it as a standard dictionary in this respect.
        _simpleTextures[categoryName] = flatTextures;
    }

    /// <summary>
    /// Handles materials entirely differently using a material registry.
    /// </summary>
    /// <param name="config"></param>
    private static void LoadMaterialTextureCategory(TextureCategoryConfig config)
    {
        string dir = _currentDirectory;
        if (config.AssetPathKey != null)
            dir = Path.Combine(_currentDirectory, config.AssetPathKey);

        string[] directories = Directory.GetDirectories(dir);
        var materialGroups = new Dictionary<string, SKMaterial>(); // baseName → material

        foreach (var folder in directories)
        {
            // E.g. "gneiss"
            string folderPrefix = Path.GetFileName(folder).ToLower();
            var files = GameLoader.GetGameFiles(null, folder);

            foreach (var file in files)
            {
                // "object_normal" from "object_normal.png"
                string fileName = Path.GetFileNameWithoutExtension(file);

                // "test_object" from "test_object_normal"
                string baseName = fileName.RemoveUnderscoreEndingTag();

                // "normal" from "test_object_normal"
                // This will normally break for entries that have no subtype, as those are diffuse textures.
                var subTypeName = fileName.GetUnderscoreEndingTag();

                // If there is no sub-type (it must be a diffuse map, OR an unsupported texture)
                //  So, the assumption is that clearly this must be a new diffuse entry! :D
                //  Hacky, yes. However this supports "..._diffuse.png" as much as it supports "....png" 
                if (!Enum.TryParse(subTypeName, true, out TextureType subType))
                {
                    Log($"Unknown sub-texture type for {fileName}. Defaulting to Diffuse.", 3);
                    subType = TextureType.DIFFUSE;
                }

                // Could be diffuse, normal, displacement, or anything.
                Texture2D texture = Load(fileName);

                // If material groups doesn't contain a material with the base name.
                //  Effectively creates a new material group using the base name as a diffuse.
                //  Items with multiple '_' underscores are fine, as all it cares about is the final one.
                if (!materialGroups.TryGetValue(baseName, out SKMaterial currentMap))
                {
                    // Changes current key texture entry main key, such as "folder_test_object" without suffix.
                    //  Because it aligns to the folder, every key will (should) be unique.
                    string currentKey = config.KeyTransform?.Invoke(folderPrefix, baseName) ??
                                        $"{folderPrefix}_{baseName}";

                    // Override current map.
                    currentMap = new SKMaterial();
                    materialGroups[currentKey] = currentMap;
                }

                // Switch between supported subtypes.
                switch (subType)
                {
                    // Finalize previous map
                    case TextureType.DIFFUSE:
                        currentMap.Diffuse = texture;
                        break;
                    case TextureType.NORMAL:
                        currentMap.Normal = texture;
                        break;
                    case TextureType.DISPLACEMENT:
                        currentMap.Displacement = texture;
                        break;
                    case TextureType.EMISSIVE:
                        currentMap.Emissive = texture;
                        break;
                }
            }
        }

        // Register all materials.
        // I am aware this is a call to yet another static class. I am not going to add a wrapper or two and make an
        //  entire registry a dictionary entry. Simple textures are bad as it is.
        foreach (var group in materialGroups)
            RegisterMaterial(group.Key, group.Value);
    }

    /// <summary>
    /// Slow calls to get material from Material Registry. Not recommended for common or repetitive use. 
    /// </summary>
    public static SKMaterial GetMaterialWithKey(string key) => GetMaterial(GetId(key));

    /// <summary>
    /// Safe accessor with error fallback and logging.
    /// </summary>
    public static Texture2D GetTexture(string category, string key)
    {
        // Attempting to retrieve a Texture2D.
        if (_simpleTextures.TryGetValue(category, out var dict))
            if (dict.TryGetValue(key, out Texture2D? value))
                return value;

        Log($"Provided Texture Key invalid for category-key pair: [{category}][{key}] — using error texture.",
            LOG.FILE_WARNING);
        return HardcodedTextures.GetErrorTexture();
    }

    private const int DEFAULT_WIDTH = 128;
    private const int DEFAULT_HEIGHT = 128;

    /// <summary>
    /// Programmer-assigned textures for use elsewhere.
    /// </summary>
    private static class HardcodedTextures
    {
        private static Texture2D? DefaultError;

        /// <returns>Cached Default Error Texture, or creates a new one if one is not present. Defaults to 128x128.</returns>
        public static Texture2D GetErrorTexture(int width = DEFAULT_WIDTH, int height = DEFAULT_HEIGHT)
        {
            if (DefaultError != null)
                return DefaultError;

            var tex = new Texture2D(_graphicsDevice, width, height);

            var pixels = new Color[128 * 128];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                bool checker = (x / 32 + y / 32) % 2 == 0;
                pixels[y * 128 + x] = checker ? new Color(1f, 0f, 1f, 1f) : Color.Black; // Magenta / Black
            }

            tex.SetData(pixels);
            DefaultError = tex;
            return tex;
        }
    }
}

/// <summary>
/// Configurable handling for texture registration behaviour.
/// </summary>
public class TextureCategoryConfig
{
    /// Asset path dictating overall category.
    /// <remarks>Make sure that this is assigned as lowercase, or whatever case needed to match folder structure</remarks>
    /// <example>e.g., "I.e. "objects", "items" , etc."</example>
    public string? AssetPathKey { get; init; }

    /// Does this texture category store complex texture maps?
    /// <value>Stores simple key-value pairs when false, and a <see cref="SKMaterial"/> dictionary when true.</value>
    public bool IsMultiTextureMap { get; init; }

    /// In-line function call to transform string tuple (key, value), returning and assigning a resulting string value. 
    public Func<string, string, string>? KeyTransform { get; init; }
}