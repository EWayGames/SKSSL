using Microsoft.Xna.Framework;

namespace SKSSL.Space;

/// A two-dimensional space that can be interacted with. This is not supposed to be for game menus.
/// <seealso cref="RenderableSpace"/>
public abstract class ExampleSpace2D : RenderableSpace
{
    public override void Initialize(GraphicsDeviceManager game)
        => throw new NotImplementedException("Implement this yourself!");

    public override void Draw(GameTime gameTime)
        => throw new NotImplementedException("Implement this yourself!");

    public override void Update(GameTime gameTime)
        => throw new NotImplementedException("Implement this yourself!");

    public override void Destroy()
        => throw new NotImplementedException("Implement this yourself!");
}