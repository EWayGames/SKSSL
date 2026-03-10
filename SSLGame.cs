using Gum.DataTypes;
using Gum.Wireframe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using SKSSL.ECS;
using SKSSL.Localization;
using SKSSL.Scenes;
using SKSSL.Textures;
using static SKSSL.DustLogger;

// ReSharper disable VirtualMemberCallInConstructor
// ReSharper disable NotAccessedField.Local

namespace SKSSL;

/// <summary>
/// Game Instances should inherit this class to have Gum and other systems automatically initialized.
/// <code>
/// override Initialize() {
/// base.Initialize(); // Naturally!
/// GameLoader.Register(...); // &lt;- Registering Game Loaders
/// }
/// </code>
/// Registering Game factories, loaders and such, such as anything that inherits BaseRegistry or <see cref="Loc"/>,
/// is incredibly important as these are the loaders that will LOAD the game's content.
/// </summary>
public abstract class SSLGame : Game
{
    /// General context of the game dictated here.
    public static SceneManager SceneManager = null!;

    /// <remarks>
    /// In order to Spawn, Remove, or generally interact with entities in an ECS, a context is required. This context
    /// varies between scenes.
    /// //WARN: In the system's current limitations, it is difficult to interact with entities
    ///    across different scenes.
    /// </remarks>
    /// <returns>Scene Manager's Current World's Entity Context.</returns>
    public static EntityContext? ECS()
    {
        if (SceneManager.CurrentWorld is not BaseWorld world)
        {
            Log("Failed to get Entity Context from current (null) world in Scene Manager!", LOG.SYSTEM_WARNING);
            return null;
        }

        if (world.ECS is null)
        {
            Log("Failed to get Entity Context for a (null) ECS Controller!", LOG.SYSTEM_WARNING);
            return null;
        }

        var entityContext = new EntityContext(world.ECS);
        return entityContext;
    }

    internal readonly GraphicsDeviceManager _graphicsManager;
    internal readonly SpriteBatch _spriteBatch;

    private static GumService Gum => GumService.Default;
    private readonly InteractiveGue currentScreenGue = new();

    /// Registries and services belonging to the game.
    private readonly IServiceProvider GameServices;

    /// <summary>
    /// An array of Tuple paths assigned to an ID. These are loaded into the game's pather, and should
    /// NEVER change. General examples include game texture and yaml prototypes folders.
    /// </summary>
    protected abstract (string id, string path)[] StaticPaths { get; }

    /// <summary>
    /// The Project Gum UI file that will dictate how UI is loaded.
    /// <code>
    /// Example: "Gum/SolKom.gumx"
    /// </code>
    /// </summary>
    public static string GumFile = "CHANGE_ME"; // Example: 

    /// <summary>
    /// Constructor for SSLGame.
    /// </summary>
    /// <param name="title">Title of the game window.</param>
    /// <param name="gumFile">Gum Interface File</param>
    protected SSLGame(string title, string gumFile = "")
    {
        Title = title;
        Content.RootDirectory = "Content";
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += HandleClientSizeChanged;
        _graphicsManager = HandleGraphicsDesignManager(new GraphicsDeviceManager(this));
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        SceneManager = new SceneManager(_graphicsManager, _spriteBatch);
        currentScreenGue.UpdateLayout(); // UI Behaviour when dragged

        if (string.IsNullOrEmpty(gumFile))
            Log($"Provided gum project file is empty! {title}, {nameof(SSLGame)}", 3);
        else
            GumFile = gumFile;

        var services = new ServiceCollection();
        LoadServices(services);
        GameServices = services.BuildServiceProvider();
    }

    /// <summary>
    /// Loads programmer-provided game services and registries.
    /// </summary>
    /// <param name="services"></param>
    /// <code>services.AddSingleton&lt;ExampleRegistry&gt;();</code>
    protected virtual void LoadServices(ServiceCollection services)
    {
        // Add game services to override method here.
    }

    // WARN: I have no idea how to do networking. This needs work. Set False as Default, for now.
    public bool IsNetworkSupported { get; set; } = false;

    /// Title of game window.
    public string Title { get; set; }

    /// <summary>
    /// Accommodates for when the user readjusts the UI dimensions.
    /// </summary>
    private void HandleClientSizeChanged(object? _, EventArgs e)
    {
        GraphicalUiElement.CanvasWidth = _graphicsManager.GraphicsDevice.Viewport.Width;
        GraphicalUiElement.CanvasHeight = _graphicsManager.GraphicsDevice.Viewport.Height;
    }

    private static GraphicsDeviceManager HandleGraphicsDesignManager(GraphicsDeviceManager graphicsDeviceManager)
    {
        var monitorWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        var monitorHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        graphicsDeviceManager.PreferredBackBufferWidth = monitorWidth; // Set preferred width
        graphicsDeviceManager.PreferredBackBufferHeight = monitorHeight; // Set preferred height
        graphicsDeviceManager.ApplyChanges();
        return graphicsDeviceManager;
    }

    /// <summary>
    /// For custom <see cref="StaticGameLoader"/>s, you MUST initialize them before the base.Initialize() an inheritance
    /// level above this class.
    /// </summary>
    protected override void Initialize()
    {
        // Initialize Gum UI Handling (Some projects may choose not to utilize Gum)
        GumProjectSave? gumSave = null;
        if (!string.IsNullOrEmpty(GumFile)) gumSave = Gum.Initialize(this, GumFile);
        SceneManager.Initialize(gumSave); // Initialize Scene Manager

        // Initialize all static paths, which the developer must have defined!
        StaticGameLoader.Initialize(StaticPaths);

        // Below handles the Initialization (/preloading) of all game data.
        // Also includes mods. This is a trick that will come in handy later~
        // TODO: Implement load order.
        List<string> workingDirectories = [];
        var allDirectories = StaticGameLoader.GetAllGameDirectories();
        foreach (var directory in allDirectories)
        {
            string texturesFolder = Path.Combine(directory, "textures");

            // If there is no textures folder, don't load it!
            if (!Directory.Exists(texturesFolder))
                continue;

            workingDirectories.Add(texturesFolder);
        }

        // Must be after Hard-coded assets, or there will be problems.
        TextureLoader.Initialize(
            Content,
            GraphicsDevice,
            // Get directory, get all folders within it, and feed as list. These are mods!
            workingDirectories);


        // Continue
        base.Initialize();
    }

    /// <inheritdoc />
    protected override void LoadContent()
    {
        base.LoadContent();
        PostLoad();
    }

    /// <summary>
    /// Custom user-defined load operation. After most LoadContent() has been loaded, but before Update calls.
    /// </summary>
    protected virtual void PostLoad()
    {
        // After game data and additional base content is loaded, then initiate post-load.
    }

    /// Quits the game.
    public void Quit() => throw new NotImplementedException("Quit is not implemented, really. Let's crash, instead.");

    /// Resets the game.
    public void ResetGame() =>
        throw new NotImplementedException("ResetGame is not implemented, really. Let's crash, instead.");

    /// <inheritdoc />
    protected override void Draw(GameTime gameTime)
    {
        SceneManager.Draw(gameTime);
        Gum.Draw();
        base.Draw(gameTime);
    }

    /// <inheritdoc />
    protected override void Update(GameTime gameTime)
    {
        SceneManager.Update(gameTime);
        Gum.Update(gameTime);
        base.Update(gameTime);
    }
}