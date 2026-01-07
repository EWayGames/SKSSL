using Microsoft.Xna.Framework.Graphics;

namespace SKSSL.Textures;

/// <summary>
/// Texture mapping fields mapped to various types of textures.
/// <example>Diffuse, Normal, Displacement, etc.</example>
/// </summary>
public record TextureMaps()
{
    /// <summary>
    /// Supported texture-types in the system.
    /// </summary>
    public enum TextureType : byte
    {
        /// <summary>
        /// Plain color information.
        /// </summary>
        DIFFUSE = 0,
        /// <summary>
        /// Normal-data.
        /// </summary>
        NORMAL = 1,
        
        // Unused as of 20260106
        //DISPLACEMENT,
        //GLOSSY,
    }

    public Texture2D? Diffuse { get; set; }
    public Texture2D? Normal { get; set; }
    public Texture2D? Displacement { get; set; }
    public Texture2D? Metallic { get; set; }
    public Texture2D? Roughness { get; set; }
    public Texture2D? Emissive { get; set; }

    // Positional constructor for normal use
    public TextureMaps(Texture2D? diffuse, Texture2D? normal = null) : this()
    {
        Diffuse = diffuse;
        Normal = normal;
    }
}