using Gum.DataTypes;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SKSSL.Scenes;

public abstract class BaseScene
{
    /// Dedicated scene spritebatch for screen rendering.
    protected SpriteBatch _spriteBatch = null!;
    
    /// Graphics management passed-down from the game.
    protected GraphicsDeviceManager _graphicsManager = null!;
    
    /// Save-data for gum UI project.
    internal GumProjectSave? _gumProjectSave;

    /// List of UI elements this scene possesses.
    protected readonly List<FrameworkElement> _Menus = [];

    /// <summary>
    /// World definition that should be initialized with a custom variant.
    /// Allows developers to initialize world settings / data per-scene.
    /// </summary>
    /// <remarks>
    /// Gameworld is passed-through as a reference. A world does NOT need to be updated within a scene, and shouldn't.
    /// The <see cref="SceneManager"/> calls world updates.
    /// </remarks>
    public IWorld? GameWorld;

    public void Initialize(GraphicsDeviceManager manager, SpriteBatch gameSpriteBatch, GumProjectSave? gumProjectSave, ref IWorld? world)
    {
        _spriteBatch = gameSpriteBatch;
        _graphicsManager = manager;
        _gumProjectSave = gumProjectSave;
        GameWorld = world;

        GameWorld?.Initialize(manager); // GameWorld has its own spritebatch.
    }

    /// <summary>
    /// The screens and UI elements that are being loaded in this scene.
    /// </summary>
    public abstract void LoadContent();

    public void UnloadContent()
    {
        GameWorld?.Destroy();
        SpecialUnload();
    }

    /// Special overridable unloading instructions should they be required.
    protected virtual void SpecialUnload()
    {
    }

    /// Per-scene Update instructions.
    public abstract void Update(GameTime gameTime);

    /// Per-scene Draw instructions.
    public abstract void Draw(GameTime gameTime);
}