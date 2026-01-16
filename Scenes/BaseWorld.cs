using Microsoft.Xna.Framework;
using SKSSL.ECS;
using SKSSL.Space;

// ReSharper disable PublicConstructorInAbstractClass

namespace SKSSL.Scenes;

public abstract class BaseWorld
{
    public ECSController ECS { get; }
    
    public abstract IRenderableSpace WorldSpace { get; }

    public virtual bool HasECS => false;    
    
    public BaseWorld()
    {
        ECS = new ECSController(this);
    }

    /// <summary>
    /// Called by <see cref="BaseScene"/> initialization.
    /// <seealso cref="SceneManager"/>
    /// </summary>
    public virtual void Initialize()
    {
        // If programmer forces ECS to be enabled, toggle it in initialize.
        if (HasECS)
            ECS.Initialize();
        WorldSpace.Initialize();
    }

    public void Update(GameTime gameTime)
    {
        ECS.Update(gameTime);
        WorldSpace.Update();
    }

    public void Draw(GameTime gameTime)
    {
        ECS.Draw(gameTime);
        WorldSpace.Draw();
    }

    /// <summary>
    /// Ensures that this world instance is safely deleted before being replaced.
    /// </summary>
    public void Destroy()
    {
        ECS.Destroy();
    }
}