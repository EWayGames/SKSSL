using System.Diagnostics.CodeAnalysis;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using MonoGameGum;
using static SKSSL.DustLogger;

namespace SKSSL.Scenes;

public class SceneManager
{
    protected GumProjectSave? _gumProjectSave;

    private readonly SpriteBatch _gameMainSpriteBatch;
    private readonly GraphicsDeviceManager _graphicsManager;

    /// <summary>
    /// World definition that should be initialized with a custom variant.
    /// Allows developers to initialize world settings / data per-scene.
    /// <remarks>May need improvement later.</remarks>
    /// </summary>
    protected internal IWorld? CurrentWorld;

    protected BaseScene? _currentScene;

    public SceneManager(GraphicsDeviceManager graphics, SpriteBatch gameMainSpriteBatch, GumProjectSave? gumSave)
    {
        _gameMainSpriteBatch = gameMainSpriteBatch;
        _graphicsManager = graphics;
        _currentScene = null;
    }

    /// <summary>
    /// Checks if "GumService.Default.Root.Children" is not Null, and if not, clears them.
    /// </summary>
    public static void ClearScreens()
    {
        if (GumService.Default.Root.Children != null)
            GumService.Default.Root.Children.Clear();
    }

    public void Initialize(GumProjectSave? gumProjectSave)
    {
        _gumProjectSave = gumProjectSave;
    }

    // WARN: This might not actually be needed? If scene switching is as I think it is, then all of this is automated. 
    [Obsolete]
    public static void LoadScreen<T>() where T : new()
    {
        if (typeof(T).BaseType != typeof(FrameworkElement))
            return;

        ClearScreens();

        T screen = new();
        if (screen is FrameworkElement frameworkElement)
            frameworkElement.AddToRoot();
        else
            throw new InvalidOperationException("The screen didn't cast correctly on load.");
    }

    /// <summary>
    /// Switches scene to new scene based on provided scene type.
    /// </summary>
    /// <typeparam name="TScene"></typeparam>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public void SwitchScene<TScene>() where TScene : BaseScene, new()
    {
        Log($"Switching to {nameof(TScene)} Scene.");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // Timing the load.

        if (typeof(TScene).BaseType != typeof(BaseScene))
            throw new TypeLoadException("Attempted to load scene type that does not derive from BaseScene.");

        Log("...clearing screens...");
        // Clear everything before creating new scene.
        MediaPlayer.Stop(); // Stop The Music
        ClearScreens(); // Clear old screens.

        // Force empty constructor of new scene. Scenes aren't instantiated and stored elsewhere, they're created here.

        Log("...unloading previous scene...");
        _currentScene?.UnloadContent(); // UniqueUnloadContent the current scene

        Log($"...creating new {nameof(TScene)} scene...");
        var newScene = new TScene();
        _currentScene = newScene; // Switch to the new scene

        // Allow scenes to override current world.
        if (_currentScene.GameWorld != null)
        {
            CurrentWorld = _currentScene.GameWorld;
        }

        // Initialize the Scene
        Log("...initializing scene...");
        _currentScene.Initialize(_graphicsManager, _gameMainSpriteBatch, _gumProjectSave, ref CurrentWorld);

        Log("...loading additional scene content...");
        _currentScene.LoadContent(); // Load the new scene content

        stopwatch.Stop(); // Stop the timer.
        Log($"Scene load complete by {stopwatch.ElapsedMilliseconds}ms");
    }

    /// Calls Draw Methods on Current Scene, which innately has an update call on the world.
    public void Draw(GameTime gameTime) => _currentScene?.Draw(gameTime);

    public void Update(GameTime gameTime) => _currentScene?.Update(gameTime);
}