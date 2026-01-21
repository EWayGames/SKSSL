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

    public virtual void Initialize(GraphicsDeviceManager graphics)
    {
        _graphics = graphics;
        _renderableSpriteBatch = new SpriteBatch(_graphics.GraphicsDevice);
    }
    public abstract void Draw(GameTime gameTime);

    public abstract void Update(GameTime gameTime);
    public abstract void Destroy();
}