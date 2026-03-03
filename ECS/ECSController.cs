using Microsoft.Xna.Framework;
using SKSSL.Scenes;
using static SKSSL.DustLogger;

// ReSharper disable UnusedMember.Local

namespace SKSSL.ECS;

/// Primary Entity-Component system controller present in any given world. Contains static <see cref="EntityContext"/> 
public class ECSController
{
    public bool Initialized { get; private set; } = false;

    public readonly ComponentRegistry ComponentRegistry;
    public readonly EntityManager EntityManager;
    private readonly SystemManager _systemManager;
    private readonly IWorld _world;

    public ECSController(IWorld world)
    {
        _world = world;
        _systemManager = new SystemManager();
        ComponentRegistry = new ComponentRegistry();
        EntityManager = new EntityManager(ref ComponentRegistry, _world, true);
    }

    /// <summary>
    /// Required method to initialize all ECS systems.
    /// </summary>
    public void Initialize()
    {
        if (Initialized)
        {
            Log("ECSController already initialized!");
            return;
        }

        Initialized = true;

        _systemManager.RegisterAll();

        ComponentRegistry.InitializeComponents();

    }

    /// Calls system manager update calls.
    public void Update(GameTime gameTime) => _systemManager.Update(gameTime);

    /// Calls system manager draw calls.
    public void Draw(GameTime gameTime) => _systemManager.Draw(gameTime);

    /// Ensures that this world instance is safely deleted before being replaced.
    public void Destroy() => EntityManager.MassacreAllEntities();
}