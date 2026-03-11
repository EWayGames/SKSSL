using System.Diagnostics;
using System.Reflection;
using SKSSL.Extensions;
using SKSSL.Scenes;
using SKSSL.Utilities;
using static SKSSL.DustLogger;

namespace SKSSL.ECS;

/// <summary>
/// Instantiated Manager for all <see cref="SKEntity"/> instances contained within it. Fundamental ECS
/// infrastructure that currently softly requires a world to be contained-in.
/// </summary>
public partial class EntityManager
{
    private static readonly IDIterator _nextId = new();
    private readonly List<SKEntity> _allEntities = [];
    private readonly ComponentRegistry _componentRegistry;
    private readonly IWorld _world;

    /// <inheritdoc cref="EntityManager"/>
    public EntityManager(ref ComponentRegistry componentRegistry, IWorld world, bool isInitialized = false)
    {
        _componentRegistry = componentRegistry;
        _world = world;
    }

    /// Get all Active entities present in the game.
    /// <seealso cref="Definitions"/>
    internal IReadOnlyList<SKEntity> AllEntities => _allEntities;

    // ReSharper disable once UnusedMember.Global
    /// All inactive Entity Definitions, which ubiquitously inherit <see cref="AEntityCommon"/>.
    public static IReadOnlyDictionary<string, AEntityCommon> Definitions => EntityRegistry.Definitions;

    #region Entity Management

    /// <summary>
    /// Remove all entities contained in Entity Manager.
    /// </summary>
    public void MassacreAllEntities()
    {
        // TODO: MIGHT require additional unloading? The list just clears references for the GC. Components these
        //  entities had aren't clear, and created IDs aren't reset back to start from 0.
        _allEntities.Clear();
    }

    /// <param name="handle">Reference ID of entity definition.</param>
    /// <returns>Null or first entity found within active <see cref="AllEntities"/> list.</returns>
    /// <remarks>Acts like <see cref="GetEntity(int)"/>, but uses a string reference handle instead.</remarks>
    public SKEntity? GetEntity(string handle) => _allEntities.FirstOrDefault(e => e?.Handle == handle, null);

    /// <param name="id">Numeric ID of requested entity.</param>
    /// <returns>Null or instance of entity with provided ID.</returns>
    /// <remarks>Requires the user to know the ID of the entity.</remarks>
    public SKEntity? GetEntity(int id)
    {
        if (_allEntities.Any(e => e.Id == id))
            return _allEntities[id];
        Log($"Attempted to retrieve nonexistent entity with ID {id}");
        return null;
    }

    /// <summary>
    /// Get-Method for all Entities of desired type. Does not handle component contents.
    /// </summary>
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
    /// Create entity using existing raw <see cref="SKEntity"/> definition. Assumes definition is valid.
    /// </summary>
    /// <param name="definition">Existing entity definition contained in the manager</param>
    /// <returns></returns>
    private SKEntity CreateEntity(SKEntity definition)
    {
        Debug.Assert(definition != null, nameof(definition) + " != null");

        // Create a copy of this entity.
        if (definition.CloneEntityAs<SKEntity>() is not SKEntity entity)
            throw new Exception("Attempted to create entity from definition, but the definition was nul!!");

        // Should be safe to create ID by now.
        int id = _nextId.Iterate();

        entity.SetRuntimeId(id);

        return FinalizeEntity(ref entity, definition.DefaultComponents);
    }

    /// <summary>
    /// Creates a new entity and returns its handle.
    /// Optionally fills metadata from a template or explicit values.
    /// </summary>
    private SKEntity CreateEntity(EntityTemplate template)
    {
        int id = _nextId.Iterate();

        // Use the template's desired entity type
        //  This is essentially a dynamic constructor to account for varying component definitions and templates.
        SKEntity entity = (SKEntity)Activator.CreateInstance(
                              template.EntityType,
                              BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                              null,
                              [id, ComponentRegistry.Count, template], // Relies on precise constructor.
                              null)!
                          ?? throw new InvalidOperationException(
                              $"Failed to create entity \"{template.Handle}\" in {nameof(CreateEntity)}");

        return FinalizeEntity(ref entity, template.DefaultComponents);
    }

    /// <summary>
    /// Generalization of final steps in completing entity creation.
    /// <br/>-➡ Assign World
    /// <br/>-➡ Copy Default Components
    /// </summary>
    private SKEntity FinalizeEntity(ref SKEntity entity, IReadOnlyDictionary<Type, object> defaultComponents)
    {
        // Assign world to entity. Will cause some funk if the world is null.
        entity.World = _world;

        foreach ((Type type, object _) in defaultComponents)
            entity.AddComponent(type);

        return entity;
    }

    /// <summary>
    /// Acquires an entity template using a provided reference id, and creates an entity instance using it.
    /// </summary>
    /// <param name="handle">Reference id to template stored in registry.</param>
    /// <returns>Spawned entity for later use.</returns>
    internal SKEntity Spawn(string handle)
    {
        if (!EntityRegistry.TryGetDefinition(handle, out AEntityCommon? definition) || definition is null)
            throw new Exception($"Failed to create entity copy using handle {handle}");
        // TODO: Nullability fallbacks may be needed from here and "up the chain" of calls.

        // Create entity regardless of how it's stored.
        SKEntity entity = definition.GetType() == typeof(SKEntity)
            ? CreateEntity((definition as SKEntity)!)
            : CreateEntity((definition as EntityTemplate)!);

        // Initialize the entity.
        entity.Initialize();

        _allEntities.Add(entity);
        return entity;
    }

    #endregion

}