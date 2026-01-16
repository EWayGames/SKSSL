// IMPL: Add query extensions for EntitySystem.

using SKSSL.ECS;
using SKSSL.Scenes;
using ComponentRegistry = SKSSL.ECS.ComponentRegistry;

namespace SKSSL.Extensions;

/// <summary>
/// Query Extensions for SKSSL ECS. Queries entities based on Component contents.
/// </summary>
/// <remarks>
/// QueryE ➡ Query-returns Entities <br/>
/// QueryEC ➡ Query-returns Entities and Components<br/>
/// </remarks>
/*public partial class EntityManager
{
    /// <summary>
    /// Overload for <see cref="QueryE{T}"/> for user convenience, as the notation may be foreign for some.
    /// </summary>
    /// <inheritdoc cref="QueryE{T}"/>
    public static IEnumerable<SKEntity> Query<T>(this BaseWorld world) where T : struct, ISKComponent => QueryE<T>(world);

    /// <typeparam name="T">Type of component queried.</typeparam>
    /// <returns>Enumerable list of Entities and their Components of type T.</returns>
    public static IEnumerable<(SKEntity, T)> QueryEC<T>(this BaseWorld world) where T : struct, ISKComponent
    {
        var all = world._entityManager.AllEntities;
        foreach (SKEntity entity in all)
            if (entity.HasComponent<T>())
                yield return (entity, entity.GetComponent<T>());
    }

    /// <summary>
    /// Get all (and only) entities with a single component type.
    /// </summary>
    /// <typeparam name="T">Type of component queried.</typeparam>
    /// <returns>Enumerable list of Entities containing components of type T</returns>
    public static IEnumerable<SKEntity> QueryE<T>(this BaseWorld world) where T : struct, ISKComponent
    {
        int typeId = ComponentRegistry.GetComponentTypeId<T>();
        foreach (SKEntity entity in world._entityManager.AllEntities)
        {
            if (entity.ComponentIndices[typeId] != -1)
                yield return entity;
        }
    }

    /// <summary>
    /// Get all (and only) entities with two component types
    /// </summary>
    public static IEnumerable<SKEntity> QueryE<T1, T2>(this BaseWorld world)
        where T1 : struct, ISKComponent
        where T2 : struct, ISKComponent
    {
        int id1 = ComponentRegistry.GetComponentTypeId<T1>();
        int id2 = ComponentRegistry.GetComponentTypeId<T2>();

        foreach (SKEntity entity in world._entityManager.AllEntities)
        {
            if (entity.ComponentIndices[id1] != -1 &&
                entity.ComponentIndices[id2] != -1)
                yield return entity;
        }
    }

    /// <summary>
    /// Get all (and only) entities with three component types.
    /// </summary>
    public static IEnumerable<SKEntity> QueryE<T1, T2, T3>(this BaseWorld world)
        where T1 : struct, ISKComponent
        where T2 : struct, ISKComponent
        where T3 : struct, ISKComponent
    {
        int id1 = ComponentRegistry.GetComponentTypeId<T1>();
        int id2 = ComponentRegistry.GetComponentTypeId<T2>();
        int id3 = ComponentRegistry.GetComponentTypeId<T3>();

        foreach (SKEntity entity in world._entityManager.AllEntities)
        {
            if (entity.ComponentIndices[id1] != -1 &&
                entity.ComponentIndices[id2] != -1 &&
                entity.ComponentIndices[id3] != -1)
                yield return entity;
        }
    }
    
    /// <summary>
    /// Get all (and only) entities with four component types.
    /// </summary>
    public static IEnumerable<SKEntity> QueryE<T1, T2, T3, T4>(this BaseWorld world)
        where T1 : struct, ISKComponent
        where T2 : struct, ISKComponent
        where T3 : struct, ISKComponent
        where T4 : struct, ISKComponent
    {
        int id1 = ComponentRegistry.GetComponentTypeId<T1>();
        int id2 = ComponentRegistry.GetComponentTypeId<T2>();
        int id3 = ComponentRegistry.GetComponentTypeId<T3>();
        int id4 = ComponentRegistry.GetComponentTypeId<T4>();

        foreach (SKEntity entity in world._entityManager.AllEntities)
        {
            if (entity.ComponentIndices[id1] != -1 &&
                entity.ComponentIndices[id2] != -1 &&
                entity.ComponentIndices[id3] != -1 &&
                entity.ComponentIndices[id4] != -1)
                yield return entity;
        }
    }
}*/