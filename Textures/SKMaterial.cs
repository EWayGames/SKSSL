using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SKSSL.Textures;

/// <summary>
/// Texture mapping fields mapped to various types of textures.
/// <example>Diffuse, Normal, Displacement, etc.</example>
/// </summary>
public struct SKMaterial
{
    // @formatter:off
    /// Albedo / color map.
    public Texture2D? Diffuse { get; set; }
    public Texture2D? Normal { get; set; } // Normal map
    public Texture2D? Displacement { get; set; } // Height map
    public Texture2D? Emissive { get; set; }
    // IMPL: occlusion, detail mask, etc. Everything below is a bit out-of-current-scope.
    //public Texture2D? Specular { get; set; }
    //public Texture2D? Metallic { get; set; }
    //public Vector4 TintColor;   // Tint color multiplier
    //public float Smoothness;    // 0-1
    //public float Metallic;      // 0-1
    // @formatter:on

    // Positional constructor for normal use
}