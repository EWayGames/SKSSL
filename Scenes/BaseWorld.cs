using Microsoft.Xna.Framework;
using SKSSL.ECS;

namespace SKSSL.Scenes;

public abstract class BaseWorld
{
    public ECSController ECS { get; }

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
    }

    public virtual void Update(GameTime gameTime)
    {
        ECS.Update(gameTime);
    }

    public virtual void Draw(GameTime gameTime)
    {
        ECS.Draw(gameTime);
    }

    /// <summary>
    /// Ensures that this world instance is safely deleted before being replaced.
    /// </summary>
    public virtual void Destroy()
    {
        ECS.Destroy();
    }
}