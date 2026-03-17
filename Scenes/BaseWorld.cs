using Microsoft.Xna.Framework;
using SKSSL.ECS;
using SKSSL.Space;

// ReSharper disable PublicConstructorInAbstractClass

namespace SKSSL.Scenes;

/// <summary>
/// Common contract for all worlds (used by SceneManager, ECSController, etc.)
/// </summary>
public interface IWorld
{
    RenderableSpace Space { get; }
    void Initialize(GraphicsDeviceManager graphics);
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime);
    void Destroy();
}

/// <summary>
/// Overridable inherited dictation of how a World, its <see cref="RenderableSpace"/>, and its systems.
/// <see cref="UsesECS"/> toggled override will permit automatic updating of underlying systems.
/// </summary>
public abstract class BaseWorld : IWorld
{
#pragma warning disable CS0618 // Type or member is obsolete
    public RenderableSpace Space { get; private set; } = new BlankWorldSpace();
    protected bool IsInitialized { get; private set; }

    /// Most worlds use ECS — this depends on overall dictation. If ECS is enabled,
    /// then a world can be forcefully disconnected per its definition. 
    /// <value>SSLGame.<see cref="SSLGame.UseECS"/></value>
    protected virtual bool UsesECS => SSLGame.UseECS;

    /// ECS controller unique to this world instance. Left null of no ECS controller.
    public ECSController? ECS { get; private set; } = null;

    /// Assign Rendered space field with provided space definition.
    public void SetSpace(RenderableSpace worldSpace)
    {
        Space.Destroy();
        Space = worldSpace;
    }

    /// Calls ECS Init() (if enabled)
    protected BaseWorld()
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        // Enable ECS if toggled-on.
        if (!UsesECS) return;
        ECS = new ECSController(this);
        ECS.Initialize();
    }

    
    /// Calls Spacial Initializations as base class method.
    public virtual void Initialize(GraphicsDeviceManager? graphics)
    {
        // Avoid re-initializing what already has been initialized.
        if (IsInitialized) return;
        IsInitialized = true;

        // Initialize WorldSpace assuming graphics provided + exception.
        if (graphics != null)
            Space.Initialize(graphics);
        else if (Space == null && graphics != null)
            throw new NullReferenceException("Attempted to initialize WorldSpace with null GraphicsDeviceManager");
    }

    public virtual void Update(GameTime gameTime)
    {
        ECS?.Update(gameTime);
        Space.Update(gameTime);
    }

    public virtual void Draw(GameTime gameTime)
    {
        ECS?.Draw(gameTime);
        Space.Draw(gameTime);
    }

    public virtual void Destroy()
    {
        ECS?.Destroy();
        Space.Destroy();
        ECS = null;
        IsInitialized = false;
    }
#pragma warning restore CS0618 // Type or member is obsolete
}