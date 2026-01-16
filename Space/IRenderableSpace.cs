using Microsoft.Xna.Framework;

namespace SKSSL.Space;

public abstract class RenderableSpace
{
    internal GraphicsDeviceManager graphics = null!;

    public virtual void Initialize(GraphicsDeviceManager game)
    {
        graphics = game;
    }
    public abstract void Draw();

    public abstract void Update();
    public abstract void Destroy();
}