using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SKSSL.Space;

namespace SKSSL.DOCUMENTATION;

/// <summary>
/// Exemplary class for implementing a 3D space.
/// Controls are usually handled in the Update() method.
/// </summary>
/// <seealso cref="RenderableSpace"/>
public abstract class ExampleSpace3D : RenderableSpace
{
    /// Rendering effect / shader for this space.
    internal BasicEffect _effect;
    
    /// Buffer of vertices for rendering.
    internal VertexBuffer _vertexBuffer;

    internal SK3DCamera Camera { get; set; } = new();

    public override void Initialize(GraphicsDeviceManager graphics)
    {
        Camera = new SK3DCamera();
        Camera.Default(base._graphics);
        Camera.ResetPosition();
        
        // Shader / Rendering
        _effect = new BasicEffect(base._graphics.GraphicsDevice);
        _effect.EnableDefaultLighting();
        _effect.LightingEnabled = true;
        _effect.AmbientLightColor = new Vector3(0.3f);
    }

    public override void Draw(GameTime gameTime) => throw new NotImplementedException("Implement this yourself!");

    public override void Update(GameTime gameTime) => throw new NotImplementedException("Implement this yourself!");

    public override void Destroy() => throw new NotImplementedException("Implement this yourself!");
}