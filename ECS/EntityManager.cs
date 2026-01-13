using System.Reflection;
using SKSSL.Extensions;
using SKSSL.Scenes;
using SKSSL.YAML;

namespace SKSSL.ECS;

/// <summary>
/// Instantiated Manager for all <see cref="SKEntity"/> instances contained within it. Fundamental ECS
/// infrastructure that currently softly requires a <see cref="BaseWorld"/> to be contained-in.
/// </summary>
public class EntityManager
{
    private static int _nextId = 0;
    private readonly List<SKEntity> _allEntities = [];

    /// <summary>
    /// Remove all entities contained within this entity Manager.
    /// </summary>
    public void WipeAllEntities()
    {
        // TODO: MIGHT require additional unloading? The list just clears references for the GC. Components these
        //  entities had aren't clear, and created IDs aren't reset back to start from 0.
        _allEntities.Clear();
    }

    /// <summary>
    /// All entities that are active and exist somewhere.
    /// </summary>
    public IReadOnlyList<SKEntity> AllEntities => _allEntities;

    /// <param name="handle">Reference ID of entity definition.</param>
    /// <returns>Null or first entity found within <see cref="AllEntities"/> list.</returns>
    /// <remarks>Acts like <see cref="GetEntity(int)"/>, but uses a string reference handle instead.</remarks>
    public SKEntity? GetEntity(string handle)
        => _allEntities.FirstOrDefault(e => e?.ReferenceId == handle, null);

    /// <param name="id">Numeric ID of requested entity.</param>
    /// <returns>Null or instance of entity with provided ID.</returns>
    /// <remarks>Requires the user to know the ID of the entity.</remarks>
    public SKEntity? GetEntity(int id)
    {
        if (_allEntities.Any(e => e.Id == id))
            return _allEntities[id];
        DustLogger.Log($"Attempted to retrieve nonexistent entity with ID {id}");
        return null;
    }

    /// <summary>
    /// Get-Method for all Entities of desired type. Does not handle component contents.
    /// </summary>
    /// <seealso cref="EntitySystemQueryExtensions"/>
    /// <typeparam name="T">
    /// Type of entities queried. <see cref="SKEntity"/> will return all entities, as
    /// all entities inherit that type.
    /// </typeparam>
    /// <returns>Readonly enumerable list of entities that inherit from type T</returns>
    public IEnumerable<SKEntity> GetEntities<T>() where T : SKEntity => AllEntities.OfType<T>();

    /// <summary>
    /// TryGet wrapper for <see cref="GetEntity(string)"/>
    /// </summary>
    public bool TryGetEntity(string handle, out SKEntity? entity)
    {
        entity = GetEntity(handle);
        return entity != null;
    }

    /// <summary>
    /// Creates a new entity and returns its handle.
    /// Optionally fills metadata from a template or explicit values.
    /// </summary>
    private static SKEntity CreateEntity(EntityTemplate template, BaseWorld? world = null)
    {
        int id = _nextId++;

        // Use the template's desired entity type
        //  This is essentially a dynamic constructor to account for varying component definitions and templates.
        var entity = (SKEntity)Activator.CreateInstance(
                         template.EntityType,
                         BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                         null,
                         [id, ComponentRegistry.Count, template],
                         null)!
                     ?? throw new InvalidOperationException(
                         $"Failed to create entity \"{template.ReferenceId}\" in {nameof(CreateEntity)}");

        // Make a copy of the entity and force the reference ID. Funky, but it works.
        entity.World = world;

        foreach ((Type type, object _) in template.DefaultComponents)
            entity.AddComponent(type);

        return entity;
    }

    /// <summary>
    /// Acquires an entity template using a provided reference id, and creates an entity instance using it.
    /// </summary>
    /// <param name="handle">Reference id to template stored in registry.</param>
    /// <param name="world">Optional world parameter to define what world this entity is present in.</param>
    /// <returns>Spawned entity for later use.</returns>
    public SKEntity Spawn(string handle, BaseWorld? world = null)
    {
        EntityTemplate template = EntityRegistry.GetTemplate(handle);
        SKEntity entity = CreateEntity(template, world);
        _allEntities.Add(entity);
        return entity;
    }
}