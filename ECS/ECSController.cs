using Microsoft.Xna.Framework;
using SKSSL.Scenes;
using static SKSSL.DustLogger;

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
    public bool Initialized { get; private set; } = false;

    private ComponentRegistry? _componentRegistry;
    private EntityManager? _entityManager;
    private SystemManager? _systemManager;
    private readonly IWorld _world;

    public ECSController(IWorld world)
    {
        _world = world;
    }

    private static ECSController? GetCurrentWorld()
    {
        return SSLGame.SceneManager.CurrentWorld is BaseWorld baseWorld ? baseWorld.ECS : null;
    }

    private List<SKEntity> ActiveEntities
        => _entityManager?.AllEntities.ToList() ?? throw new InvalidOperationException(
            "Entity manager is null. Did you forget to Initialize the ECS?");

    /// <summary>
    /// 
    /// </summary>
    /// <param name="referenceId">Reference ID of entity to spawn.</param>
    /// <returns>Instant of that entity.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static SKEntity? SpawnEntity(string referenceId)
    {
        ECSController? controller = GetCurrentWorld();
        if (controller == null)
        {
            Log(
                $"Called {nameof(ECSController)}.{nameof(SpawnEntity)} to spawn {referenceId}, but the Current World's " +
                $"ECS controller was not found! There is either no current world, or the current world does not have ECS enabled!");
            return null;
        }

        if (controller._entityManager == null)
        {
            Log($"Called {nameof(ECSController)}.{nameof(SpawnEntity)} to spawn {referenceId}, but the controller's " +
                $"entity manager was found!");
            return null;
        }

        SKEntity? spawnedEntity = null;
        try
        {
            spawnedEntity = controller._entityManager.Spawn(referenceId);
        }
        catch (Exception e)
        {
            Log($"Call to {nameof(SpawnEntity)} failed to spawn entity reference {referenceId}: {e.Message}");
        }

        return spawnedEntity;
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

        _systemManager = new SystemManager();
        _systemManager.RegisterAll();

        _componentRegistry = new ComponentRegistry();
        _componentRegistry.InitializeComponents();

        _entityManager = new EntityManager(ref _componentRegistry, _world, true);
    }

    /// Calls system manager update calls.
    public void Update(GameTime gameTime) => _systemManager?.Update(gameTime);

    /// Calls system manager draw calls.
    public void Draw(GameTime gameTime) => _systemManager?.Draw(gameTime);

    /// Ensures that this world instance is safely deleted before being replaced.
    public void Destroy() => _entityManager?.MassacreAllEntities();
}