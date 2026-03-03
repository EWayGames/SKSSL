using static SKSSL.DustLogger;

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

    /// <summary>
    /// Blank constructor that calls method in <see cref="SSLGame"/> to statically get ECS instance.
    /// </summary>
    /// <exception cref="NullReferenceException">Thrown when ECS acquired is null.</exception>
    public EntityContext()
    {
        var ec = SSLGame.ECS();
        if (ec == null)
        {
            throw new NullReferenceException(
                "Attempted to create Entity Context in Blank Constructor from an Entity Context that doesn't exist!");
        }

        EntityManager = ec.Value.EntityManager;
        Components = new ComponentRegistry();
    }

    public EntityContext(ECSController ecs)
    {
        EntityManager = ecs.EntityManager;
        Components = ecs.ComponentRegistry;
    }

    /* Below are Proxy-Methods, that is to say functions designed to be called remotely that which call internal methods
     * inside the Entity Manager, and Component Registry.
     */

    #region Proxxy Methods

    public List<SKEntity> ActiveEntities => EntityManager.AllEntities.ToList();
    
    public SKEntity? SpawnEntity(string referenceId)
    {
        SKEntity? spawnedEntity = null;
        try
        {
            spawnedEntity = EntityManager.Spawn(referenceId);
        }
        catch (Exception e)
        {
            Log($"{nameof(EntityContext)}.{nameof(SpawnEntity)} call failed to spawn {referenceId}: {e.Message}");
        }
        
        return spawnedEntity;
    }

    #endregion
}