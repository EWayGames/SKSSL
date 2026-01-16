using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SKSSL.Space;

public abstract class RenderableSpace
{
    internal GraphicsDeviceManager graphics = null!;
    private SpriteBatch renderableSpriteBatch = null!;

    public virtual void Initialize(GraphicsDeviceManager game)
    {
        graphics = game;
        renderableSpriteBatch = new SpriteBatch(graphics.GraphicsDevice);
    }
    public abstract void Draw();

    public abstract void Update();
    public abstract void Destroy();
}