using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SKSSL.DOCUMENTATION;
using SKSSL.Scenes;

namespace SKSSL.Space;

/// <summary>
/// A "physical" virtual space or area that is rendered for gameplay. Constitutes, typically, the entire field that
/// which the user will play in. Implement this class however you see fit.
/// Instantiate your renderable space within your <see cref="BaseWorld"/> class.
/// </summary>
/// <seealso cref="ExampleSpace2D"/>
/// <seealso cref="ExampleSpace3D"/>
public abstract class RenderableSpace
{
    internal GraphicsDeviceManager _graphics = null!;
    private SpriteBatch _renderableSpriteBatch = null!;

    /// Initializes Renderable Space with provided graphics device.
    public virtual void Initialize(GraphicsDeviceManager graphics)
    {
        _graphics = graphics;
        _renderableSpriteBatch = new SpriteBatch(_graphics.GraphicsDevice);
    }

    /// Overwritten Draw() call for Space Contents.
    public abstract void Draw(GameTime gameTime);

    /// Overwritten Update() call for Space objects.
    public abstract void Update(GameTime gameTime);

    /// Overwritten Destroy() call for data-clearing.
    public abstract void Destroy();
}

/// Blank world space to act as a default. Should NOT be used, but instead replaced.
[Obsolete("Create a custom RenderableSpace instead.")]
public class BlankWorldSpace : RenderableSpace
{
    public override void Draw(GameTime gameTime) => _graphics.GraphicsDevice.Clear(Color.Purple);

    public override void Update(GameTime gameTime)
    {
    }

    public override void Destroy()
    {
    }
}