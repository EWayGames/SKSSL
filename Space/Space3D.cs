using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SKSSL.Space;

public class Space3D : RenderableSpace
{
    /// Rendering effect / shader for this space.
    private BasicEffect _effect;
    
    /// Buffer of vertices for rendering.
    VertexBuffer _vertexBuffer;

    private SK3DCamera Camera { get; set; } = new();

    public override void Initialize(GraphicsDeviceManager game)
    {
        Camera = new SK3DCamera();
        Camera.Default(graphics);
        Camera.ResetPosition();
        
        // Shader / Rendering
        _effect = new BasicEffect(graphics.GraphicsDevice);
        _effect.EnableDefaultLighting();
        _effect.LightingEnabled = true;
        _effect.AmbientLightColor = new Vector3(0.3f);
    }

    public override void Draw()
    {
    }

    public override void Update()
    {
    }

    public override void Destroy()
    {
    }
}