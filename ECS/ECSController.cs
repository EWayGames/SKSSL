using Microsoft.Xna.Framework;
using SKSSL.Scenes;

// ReSharper disable UnusedMember.Local

namespace SKSSL.ECS;

public readonly struct EntityContext
{
    public readonly EntityManager EntityManager;
    public readonly ComponentRegistry Components;

    public EntityContext(EntityManager entityManager, ComponentRegistry componentRegistry)
    {
        EntityManager = entityManager;
        Components = componentRegistry;
    }
}


/// Primary Entity-Component system controller present in any given world. Contains static <see cref="EntityContext"/> 
public class ECSController
{
    // WARN: This somewhat goes *against* the principle of a divided ECS. It's understandable that its controller needs
    //  to be accessed quickly and conveniently outside of a world context, but still.
    /// General context that which all entities are acting.
    public static EntityContext? EntityContext;
    public static EntityManager EntityManager => EntityContext!.Value.EntityManager;
    
    public bool Initialized { get; private set; } = false;

    private ComponentRegistry? _componentRegistry;
    private EntityManager? _entityManager;
    private SystemManager? _systemManager;
    private readonly IWorld _world;

    public ECSController(IWorld world)
    {
        _world = world;
    }

    private List<SKEntity> ActiveEntities
        => _entityManager?.AllEntities.ToList() ?? throw new InvalidOperationException(
            "Entity manager is null. Did you forget to Initialize the ECS?");

    private SKEntity SpawnEntity(string referenceId)
        => _entityManager?.Spawn(referenceId) ?? throw new InvalidOperationException(
            "Entity manager is null. Did you forget to Initialize the ECS?");

    /// <summary>
    /// Required method to initialize all ECS systems.
    /// </summary>
    public void Initialize()
    {
        if (Initialized)
        {
            DustLogger.Log("ECSController already initialized!");
            return;
        }

        Initialized = true;

        _systemManager = new SystemManager();
        _systemManager.RegisterAll();
        
        _componentRegistry = new ComponentRegistry();
        _componentRegistry.InitializeComponents();
        
        _entityManager = new EntityManager(ref _componentRegistry, _world);
        
        // Assign entity context for reflective purposes.
        EntityContext = new EntityContext(_entityManager, _componentRegistry);
    }

    /// Calls system manager update calls.
    public void Update(GameTime gameTime) => _systemManager?.Update(gameTime);

    /// Calls system manager draw calls.
    public void Draw(GameTime gameTime) => _systemManager?.Draw(gameTime);

    /// Ensures that this world instance is safely deleted before being replaced.
    public void Destroy() => _entityManager?.MassacreAllEntities();
}