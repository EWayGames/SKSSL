using Microsoft.Xna.Framework;

namespace SKSSL.Space;

/// A two-dimensional space that can be interacted with. This is not supposed to be for game menus.
/// <seealso cref="RenderableSpace"/>
public abstract class ExampleSpace2D : RenderableSpace
{
    public override void Initialize(GraphicsDeviceManager game)
    {
        throw new NotImplementedException("2D Space Not Implemented");
    }

    public override void Draw()
    {
        throw new NotImplementedException();
    }

    public override void Update()
    {
        throw new NotImplementedException();
    }

    public override void Destroy()
    {
        throw new NotImplementedException();
    }
}