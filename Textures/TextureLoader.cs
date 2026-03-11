using System.Collections.Concurrent;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SKSSL.Extensions;
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

/// Default implementation
public class BlankTextureLoader : TextureLoader
{
    /// <inheritdoc />
    public BlankTextureLoader(ContentManager contentManager, GraphicsDevice graphicsDevice) : base(contentManager,
        graphicsDevice)
    {
    }

    /// <inheritdoc />
    protected override void InitializeRegistries() =>
        throw new NotImplementedException(
            "Developer(s) MUST implement custom Registries Initialization, as registries may vary between projects.");
}

/// <summary>
/// Generic texture loader for all game asset categories (objects, items, UI, etc.).
/// Supports multi-texture maps (diffuse + normal + etc.) and automatic error texture fallback.
/// <br/><br/>
/// <see cref="InitializeRegistries"/> MUST be filled-out per-implementation based on the
/// developer requirements / layout of the project.
/// Allows the developer to pre-initialize a custom loader for the game, assuming it is on the surface-level of
/// game initialization and before base.Initialize() is called in the game's Initialize() method.
/// <see cref="TextureLoader"/> instance may be provided to override the <see cref="BlankTextureLoader"/>.
/// </summary>
public abstract partial class TextureLoader
{
    /// Initially default implementation. Permits one static instance per program.
    private static TextureLoader _instance = null!;

    /// Allow override (e.g., for mods or tests)
    public static TextureLoader Instance
    {
        get => _instance;
        set => _instance = value ?? throw new ArgumentNullException(nameof(value));
    }

    private static GraphicsDevice _graphicsDevice { get; set; } = null!;

    /// <summary>
    /// Reference to Monogame's content manager for "base game" content.
    /// </summary>
    private static ContentManager _monoGameContent = null!;

    /// Generic storage: category → (texture name → texture object). These are textures actively being used in memory.
    private static readonly ConcurrentDictionary<string, Dictionary<string, Texture2D>> _textures = new();

    private static readonly Dictionary<string, TextureCategoryConfig> _categories = new();

    #region Initialization

    private static bool IsInitialized { get; set; } = false;

    /// Default static assignation of instance of a texture loader.
    public TextureLoader(ContentManager contentManager, GraphicsDevice graphicsDevice)
    {
        // Load Custom Registries.
        _instance = this;
        _monoGameContent = contentManager ?? throw new ArgumentNullException(nameof(contentManager));
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
    }

    /// <summary>
    /// Initializes texture loaded. An alternative version of the loaded with a custom implement for
    /// <br/><br/>
    /// It is IMPERATIVE that this be loaded before the base.Initialize() of the game's Initialize() method.
    /// </summary>
    /// <param name="gameContentDirectories"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void Initialize(IEnumerable<GameContentDirectory> gameContentDirectories)
    {
        // If the texture loader has already been initialized by a "surface-level" class override,
        //  then that override is the one that shall be used and whatever is needed has already been initialized.
        if (IsInitialized)
            return;
        IsInitialized = true;
        _instance.InitializeRegistries();
        CompleteTextureInit(gameContentDirectories);
    }

    /// <summary>
    /// Load all game textures into memory. This should be called by a Content Manager.
    /// </summary>
    /// <param name="gameContentDirectories"></param>
    /// <remarks>
    /// Due to how it is written, all texture categories must be registered.
    /// I.e. "items" must have a dedicated "items" folder.
    /// </remarks>
    private static void CompleteTextureInit(IEnumerable<GameContentDirectory> gameContentDirectories)
    {
        // Below handles the Initialization (/preloading) of all game data.
        // Also includes mods. This is a trick that will come in handy later~
        // TODO: Implement load order.
        List<string> textureFolders = [];
        foreach (GameContentDirectory directory in gameContentDirectories)
        {
            string? texturesFolder = directory.TexturesFolder;
            if (texturesFolder is not null)
                textureFolders.Add(texturesFolder);
        }

        // Load all folders with registered textures.
        foreach (var folder in textureFolders)
        {
            // Get all categorical texture folders.
            var subFolders = Directory.GetDirectories(folder, "", SearchOption.TopDirectoryOnly);
            ProcessTextureSubfolder(subFolders);
        }

        return;

        // Process all folders inside textures folder. 
        void ProcessTextureSubfolder(IEnumerable<string> subFolders)
        {
            // Extract every texture folder contained in subfolders, and handle differently depending on categories.
            foreach (var subFolder in subFolders)
            {
                // Ensure that this folder is a valid one.
                var name = Path.GetFileName(subFolder);

                // Obtain the first config file whose asset key matches this subfolder.
                TextureCategoryConfig? config =
                    _categories.Values.FirstOrDefault(d => subFolder.Contains(d.AssetPathKey));
                if (config is null) continue;

                // Use registered asset paths to find dedicated folder, and load it.
                if (!name.Contains(config.AssetPathKey))
                    // TODO: Add handling for "rogue" texture folders, who aren't registered.
                    continue;

                // Database for specific category, such as "Items" or "Entities", etc.
                if (config.IsMultiTextureMap)
                    LoadMaterialTextureCategory(subFolder, config);
                else
                    LoadSingleTextureCategory(subFolder, config);
            }
        }
    }

    #endregion

    // The "static" method — but delegates to instance

    #region Get Raw Images

    /// <summary>
    /// Loads a provided asset name as a <see cref="Texture2D"/>.
    /// Assumes the folders within the TextureLoader are all texture folders.
    /// </summary>
    /// <param name="key">Name of the provided asset without extension. (e.g. "Textures/PlayerSprite")</param>
    /// <param name="config">Dedicated category.</param>
    /// <param name="path">Path to asset.</param>
    /// <returns>Texture asset or Default Error Texture, instead.</returns>
    public static Texture2D Load(string key, TextureCategoryConfig? config = null, string? path = null)
    {
        // Attempting to retrieve a Texture2D.
        // Using the key and category, check if texture already exists in memory.
        // If the category provided is a valid (non-null) one, then it should be checked.
        if (config is not null)
        {
            // Textures :: (category -> [key, texture])
            if (_textures.TryGetValue(config.AssetPathKey, out var dictionary) &&
                dictionary.TryGetValue(key, out Texture2D? dictionaryTexture))
                return dictionaryTexture;
        }
        // However if the category isn't valid, then one must brute-force search through textures. Not optimal!
        else
        {
            // Get texture by brute force.
            Texture2D? bruteForcedTexture = _textures.Values
                .SelectMany(d => d)
                .Where(kvp => kvp.Key == key)
                .Select(kvp => kvp.Value)
                .FirstOrDefault();
            if (bruteForcedTexture is not null)
                return bruteForcedTexture;
        }

        // TODO: Add support for naming prototypes / textures with <asset_name>_<mod_name> support.
        //  Use GameFolderDirectory type.

        // If it gets to this point, then clearly the texture likely is not loaded inside. In such case, it must be
        //  loaded by the filepath, and stored appropriately using the key.
        // Assuming the file exists, attempting to load it directly with more brute force than ever before is the next
        //  best option.
        if (File.Exists(path))
        {
            // Read the path directly.
            using FileStream stream = File.OpenRead(path);
            Texture2D? streamTexture = Texture2D.FromStream(_graphicsDevice, stream);

            // If the category is provided, then load this texture into memory as if it were cached.
            if (config is not null && !config.IsMultiTextureMap)
                _textures[config.AssetPathKey][key] = streamTexture;
            // TODO: Add handling if category is null. Automatically add category based on folder parent.

            stream.Close(); // Stop memory leaks with this one simple trick!
            return streamTexture;
        }

        // At this point attempts to load with a provided key and category failed. Loading from a filepath also failed,
        //  and the program inches towards an Error texture. To avoid this, a last-ditch attempt to use Monogame's
        //  content builder is made. This is surrounded in a Try-Catch because the vanilla pipeline load fails if no
        //  '.xnb' file exists in the system.
        try
        {
            var streamTexture = _monoGameContent.Load<Texture2D>(key);

            if (config is not null && !config.IsMultiTextureMap)
                _textures[config.AssetPathKey][key] = streamTexture;
            // TODO: Add handling if category is null. Automatically add category based on folder parent.

            return streamTexture;
        }
        catch (ContentLoadException)
        {
        }
        catch (Exception)
        {
            // Expected if no vanilla asset  
        }

        // All hope as failed. Time to return the error texture instead.
        Log($"Failed to find valid path for texture cat.-key-pair: [{config}:{key}] — using error texture.",
            LOG.FILE_ERROR);
        return HardcodedTextures.GetErrorTexture();
    }

    #endregion

    /// <summary>
    /// Custom method for initializing dedicated registries. Overload required.
    /// </summary>
    /// <remarks>Registries are the dedicated names to the topmost folders containing textures.</remarks>
    protected abstract void InitializeRegistries();

    /// <summary>
    /// Register a new texture category (e.g., objects, items).
    /// </summary>
    public static void RegisterCategory(TextureCategoryConfig config)
    {
        _categories[config.AssetPathKey] = config;

        // Material mapping is now handled in the Material Registry.
        if (!config.IsMultiTextureMap)
            _textures[config.AssetPathKey] = new Dictionary<string, Texture2D>();
    }

    /// <summary>
    /// Get read-only dictionary for a category.
    /// </summary>
    public static IReadOnlyDictionary<string, TTexture> GetCategory<TTexture>(string categoryName)
    {
        if (_textures.TryGetValue(categoryName, out var dict))
        {
            return (IReadOnlyDictionary<string, TTexture>)dict;
        }

        return new Dictionary<string, TTexture>().AsReadOnly();
    }

    private static void LoadSingleTextureCategory(string gameFolder, TextureCategoryConfig config)
    {
        // Get all files in the textures folder and process them.
        var files = StaticGameLoader.GetGameFiles(gameFolder);
        foreach (var file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            string key = config.KeyTransform?.Invoke(fileName, file) ?? fileName.ToLower();

            // Error Reporting & Texture is automatically handled in the Load() call.
            Load(key, config, file);
        }
    }

    /// <summary>
    /// Handles materials entirely differently using a material registry.
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="config"></param>
    private static void LoadMaterialTextureCategory(string directory, TextureCategoryConfig config)
    {
        string[] directories = Directory.GetDirectories(directory);
        var materialGroups = new Dictionary<string, SKMaterial>(); // baseName → material

        // Every dedicated texture folder in a material directory dictates a prefix.
        foreach (var folder in directories)
        {
            // E.g. "gneiss"
            string folderPrefix = Path.GetFileName(folder).ToLower();

            // Get every file contained in this folder.
            var files = StaticGameLoader.GetGameFiles(folder);
            foreach (var file in files)
            {
                // "object_normal" from "object_normal.png"
                // "test_object" from "test_object_normal"
                // "normal" from "test_object_normal"
                // This will break for entries with no explicit subtype. Those default to diffuse textures.
                string fileName = Path.GetFileNameWithoutExtension(file);
                string baseName = fileName.RemoveUnderscoreEndingTag();
                var subTypeName = fileName.GetUnderscoreEndingTag();

                // If there is no subtype (it must be a diffuse map, OR an unsupported texture)
                //  So, the assumption is that clearly this must be a new diffuse entry! :D
                //  Hacky, yes. However, this supports "..._diffuse.png" as much as it supports "....png" 
                if (!Enum.TryParse(subTypeName, true, out TextureType subType))
                {
                    // No need to log default behaviour.
                    //Log($"Unknown sub-texture type for {fileName}. Defaulting to Diffuse.", 3);
                    subType = TextureType.DIFFUSE;
                }

                // Could be diffuse, normal, displacement, or anything.
                Texture2D texture = Load(fileName, config, file);

                // Changes current key texture entry main key, such as "folder_test_object" without suffix.
                //  Because it aligns to the folder, every key will (should) be unique.
                string currentKey = config.KeyTransform?.Invoke(folderPrefix, baseName) ??
                                    $"{folderPrefix}_{baseName}";

                // If material groups doesn't contain a material with the base name.
                //  Effectively creates a new material group using the base name as a diffuse.
                //  Items with multiple '_' underscores are fine, as all it cares about is the final one.
                if (!materialGroups.TryGetValue(currentKey, out SKMaterial? currentMap))
                {

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
    /// Asset path that is checked-over for loading. Also used for categorization.
    /// <remarks>Make sure that this is assigned as lowercase, or whatever case needed to match folder structure</remarks>
    /// <example>e.g., "I.e. "objects", "items" , etc."</example>
    public required string AssetPathKey { get; init; }

    /// Does this texture category store complex texture maps?
    /// <value>Stores simple key-value pairs when false, and a <see cref="SKMaterial"/> dictionary when true.</value>
    /// <remarks>Example layout:<br/>
    /// game ➡<br/>
    /// .textures ➡<br/>
    /// ..test ➡<br/>
    /// ...test.png, test_normal.png, etc.</remarks>
    public bool IsMultiTextureMap { get; init; } = false;

    /// In-line function call to transform string tuple (key, value), returning and assigning a resulting string value. 
    public Func<string, string, string>? KeyTransform { get; init; }
}