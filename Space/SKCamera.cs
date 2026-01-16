using Microsoft.Xna.Framework;

namespace SKSSL.Space;

public class SK3DCamera
{
    /// Camera's target. (The thing it's pointed at.)
    private Vector3 target;

    /// Camera's position.
    private Vector3 position;

    /// Camera's projection matrix.
    private Matrix projection;

    /// Camera's view matrix.
    private Matrix view;

    /// World matrix.
    private Matrix world;

    /// Is this camera focused around a single point?
    private bool orbit = false;

    public void ResetPosition()
    {
        target = Vector3.Zero;
        position = Vector3.Zero;
    }

    public void Default(GraphicsDeviceManager graphics)
    {
        projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45f),
            graphics.GraphicsDevice.Viewport.AspectRatio, 1f, 1000f);
        view = Matrix.CreateLookAt(position, target, new Vector3(0f, 1f, 0f)); // Y up
        world = Matrix.CreateWorld(target, Vector3.Forward, Vector3.Up);
    }
}