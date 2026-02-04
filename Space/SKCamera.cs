using Microsoft.Xna.Framework;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SKSSL.Space;

/// Three-Dimensional Camera For Use in a 3D Space
public class SK3DCamera
{
    /// Camera's target. (The thing it's pointed at.)
    private Vector3 Target;

    /// Camera's position.
    public Vector3 Position { get; private set; }

    /// Camera's projection matrix.
    public Matrix Projection { get; private set; }

    /// Camera's view matrix.
    public Matrix View { get; private set; }

    /// World matrix.
    public Matrix World { get; private set; }

    /// Is this camera focused around a single point?
    public bool IsOrbit { get; private set; } = false;

    /// Resets camera Target and Position to Zero.
    public void ResetPosition()
    {
        Target = Vector3.Zero;
        Position = Vector3.Zero;
    }

    /// Resets Camera to Default Values
    public void Default(GraphicsDeviceManager graphics)
    {
        Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45f),
            graphics.GraphicsDevice.Viewport.AspectRatio, 1f, 1000f);
        View = Matrix.CreateLookAt(Position, Target, new Vector3(0f, 1f, 0f)); // Y up
        World = Matrix.CreateWorld(Target, Vector3.Forward, Vector3.Up);
    }

    /// Assigns Projection, View, and World Matrices to Camera
    public void Set(Matrix projection, Matrix view, Matrix world)
    {
        Projection = projection;
        View = view;
        World = world;
    }
}