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
    RenderableSpace? WorldSpace { get; }
    void Initialize(GraphicsDeviceManager graphics);
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime);
    void Destroy();
}

/// <summary>
/// Non-generic base class with common infrastructure
/// </summary>
public abstract class BaseWorld : IWorld
{
    protected bool IsInitialized { get; private set; }

    public RenderableSpace? WorldSpace { get; protected set; }

    // Most worlds use ECS — this is opt-out instead of opt-in
    protected virtual bool UsesECS => false;

    public ECSController? ECS { get; private set; }

    protected BaseWorld()
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        if (UsesECS)
            ECS = new ECSController(this);
    }

    public virtual void Initialize(GraphicsDeviceManager graphics)
    {
        if (IsInitialized) return;
        IsInitialized = true;

        ECS?.Initialize();
        WorldSpace?.Initialize(graphics);
    }

    public virtual void Update(GameTime gameTime)
    {
        ECS?.Update(gameTime);
        WorldSpace?.Update(gameTime);
    }

    public virtual void Draw(GameTime gameTime)
    {
        ECS?.Draw(gameTime);
        WorldSpace?.Draw(gameTime);
    }

    public virtual void Destroy()
    {
        ECS?.Destroy();
        WorldSpace?.Destroy();
        WorldSpace = null;
        ECS = null;
        IsInitialized = false;
    }
}

/// <summary>
/// Generic typed version — inherit from this for concrete worlds
/// </summary>
public abstract class BaseWorld<TSpace> : BaseWorld
    where TSpace : RenderableSpace, new()
{
    // Strongly-typed access for derived classes
    public new TSpace? WorldSpace
    {
        get => base.WorldSpace as TSpace;
        protected set => base.WorldSpace = value;
    }

    protected BaseWorld()
    {
        // Guaranteed non-null after construction
        WorldSpace = new TSpace();
    }

    // ReSharper disable RedundantOverriddenMember

    #region Virtual Basic Methods

    public override void Initialize(GraphicsDeviceManager graphics) => base.Initialize(graphics);
    public override void Update(GameTime gameTime) => base.Update(gameTime);

    public override void Draw(GameTime gameTime) => base.Draw(gameTime);

    public override void Destroy() => base.Destroy();

    #endregion
}