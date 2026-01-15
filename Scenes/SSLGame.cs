using Gum.DataTypes;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using SKSSL.Localization;
using SKSSL.Textures;
using SKSSL.Utilities;

namespace SKSSL.Scenes;

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
    public readonly SceneManager SceneManager;

    internal readonly GraphicsDeviceManager _graphicsManager;
    internal readonly SpriteBatch _spriteBatch;

    private static GumService Gum => GumService.Default;
    private readonly InteractiveGue currentScreenGue = new();
    
    public AbstractGameData GameData { get; set; }

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
    /// <param name="gameDataClass">Provided non-static game data.</param>
    /// <param name="title">Title of the game window.</param>
    /// <param name="gumFile">Gum Interface File</param>
    protected SSLGame(AbstractGameData gameDataClass, string title, string gumFile = "")
    {
        GameData = gameDataClass;
        
        Title = title;
        SceneManager = new SceneManager(this);
        _graphicsManager = HandleGraphicsDesignManager(new GraphicsDeviceManager(this));
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += HandleClientSizeChanged;
        currentScreenGue.UpdateLayout(); // UI Behaviour when dragged

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        Content.RootDirectory = "Content";

        if (string.IsNullOrEmpty(gumFile))
            DustLogger.Log($"Provided gum project file is empty! {title}, {nameof(SSLGame)}", 3);
        else
            GumFile = gumFile;
    }

    // WARN: I have no idea how to do networking. This needs work. Set False as Default, for now.
    public bool IsNetworkSupported { get; set; } = false;
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
        SceneManager.Initialize(_graphicsManager, _spriteBatch, gumSave); // Initialize Scene Manager

        // Initialize all static paths, which the developer must have defined!
        StaticGameLoader.Initialize(StaticPaths);

        #region Modding

        // Get the mods in the game. This is a trick that will come in handy later~
        var mods = StaticGameLoader.GetAllModDirectories();
        var validModTextures = StaticGameLoader.GetDirectoriesWithSubPathAndAppend(mods, "textures");

        // Must be after Hard-coded assets, or there will be problems.
        TextureLoader.Initialize(
            Content,
            GraphicsDevice,
            // Get game mod directory, get all folders within it, and feed as list. These are mods!
            validModTextures);

        #endregion

        // Continue
        base.Initialize();
    }

    protected override void LoadContent()
    {
        // Load Game Data
        GameData.Load(
        [ // Load Game and Mods in one breath.
            StaticGameLoader.GPath(StaticGameLoader.DEFAULT_FOLDER_GAME),
            StaticGameLoader.MPath(StaticGameLoader.DEFAULT_FOLDER_GAME)
        ]);
        base.LoadContent();
        
        // After game data and additional base content is loaded, then initiate post-load.
        GameData.PostLoad();
    }

    public void Quit() => throw new NotImplementedException();

    public void ResetGame() => throw new NotImplementedException();

    protected override void Draw(GameTime gameTime)
    {
        SceneManager.Draw(gameTime);
        Gum.Draw();
        base.Draw(gameTime);
    }

    protected override void Update(GameTime gameTime)
    {
        SceneManager.Update(gameTime);
        Gum.Update(gameTime);
        base.Update(gameTime);
    }
}