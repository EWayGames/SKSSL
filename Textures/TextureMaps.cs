using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SKSSL.Textures;

public struct TextureMaps
{
    public enum TextureType
    {
        DIFFUSE,
        NORMAL,

        // Unused
        DISPLACEMENT,
        GLOSSY,
    }
    
    public Texture2D Diffuse;
    public Texture2D Normal;

    // public Texture2D Specular;
    // public Texture2D Shaded;

    /// <summary>
    /// This constructor's explicit use is to use the content pipeline to create an error texture to replace a bad
    /// reference in any one of the prototypes.
    /// </summary>
    public TextureMaps(ContentManager contentManager)
    {
        // TODO: Make this a static reference. No need to call load on every error-ed texture. 
        var errorTexture =
            contentManager.Load<Texture2D>(Path.Combine(contentManager.RootDirectory, "textures", "error"));
        Diffuse = errorTexture;
        Normal = errorTexture;
    }
}