using System.Diagnostics;
using System.Reflection;
using SKSSL.Extensions;
using SKSSL.Scenes;
using SKSSL.Utilities;
using SKSSL.YAML;
using YamlDotNet.Core;
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
    internal readonly ComponentRegistry _componentRegistry;
    private readonly IWorld _world;

    /// One-time toggle to use raw entity-class definitions OR entity templates.
    /// For complex entity types that have multiple levels of inheritance and varying data structures,
    /// Entity Templates are highly suggested.
    private readonly bool _useRawEntities;

    /// Global default toggle for development purposes.
    /// Used in multiple parts "up-the-chain"
    public const bool DefaultUseRawEntities = true;

    /// <inheritdoc cref="EntityManager"/>
    public EntityManager(
        ref ComponentRegistry componentRegistry,
        IWorld world,
        bool useRawEntities = DefaultUseRawEntities)
    {
        _componentRegistry = componentRegistry;
        _world = world;
        _useRawEntities = useRawEntities;
    }

    /// Get all Active entities present in the game.
    /// <seealso cref="Definitions"/>
    public IReadOnlyList<SKEntity> AllEntities => _allEntities;

    /// All inactive Entity Definitions, which ubiquitously inherit <see cref="AEntityCommon"/>.
    public static IReadOnlyDictionary<string, AEntityCommon> Definitions => _definitions;

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
    /// <exception cref="InvalidOperationException">Thrown when <see cref="_useRawEntities"/> is false.</exception>
    private SKEntity CreateEntity(SKEntity definition)
    {
        if (!_useRawEntities)
            throw new InvalidOperationException("Attempted to create entity without Raw Entity definitions enabled!");

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
    public SKEntity Spawn(string handle)
    {
        // No matter what, an entity should be made.
        // TODO: Nullability fallbacks may be needed from here and "up the chain" of calls.
        SKEntity entity;
        // If using raw entities, get definition and attempt to create a direct copy.
        if (_useRawEntities)
        {
            if (!TryGetDefinition(handle, out SKEntity? definition) && definition != null)
                throw new Exception($"Failed to create entity-definition copy using handle {handle}");
            entity = CreateEntity(definition!);
        }
        // If not using raw entities, attempt to grab a template from definitions and create using that overload.
        else
        {
            if (!TryGetDefinition(handle, out EntityTemplate? definition) && definition != null)
                throw new Exception($"Failed to create entity template using handle {handle}");
            entity = CreateEntity(definition!);
        }

        _allEntities.Add(entity);
        return entity;
    }

    #endregion

    #region Entity Registry

    private static readonly Dictionary<string, AEntityCommon> _definitions = new();

    /// <summary>
    /// Calls <see cref="RegisterTemplate{TYaml, TTemplate}"/> with a default to the <see cref="EntityYaml"/> type.
    /// </summary>
    /// <param name="yaml">The yaml file of the template.</param>
    /// <typeparam name="T">Type of template being registered. Forces inheritance.</typeparam>
    /// <exception cref="ArgumentException"></exception>
    public void RegisterEntity<T>(EntityYaml yaml) where T : AEntityCommon
    {
        // Register raw entity
        if (typeof(T) == typeof(SKEntity))
        {
            // Error handling - don't use raw entities whilst it's disabled!
            if (!_useRawEntities)
                throw new InvalidCastException("Attempted to create Raw Entity whilst UseRawEntities = false!");
            RegisterDefinition<EntityYaml, SKEntity>(yaml);
        }
        // Attempt register of template
        else if (typeof(T) == typeof(EntityTemplate))
        {
            // Error handling - don't use entity templates whilst raw entities are enabled!
            if (_useRawEntities)
                throw new InvalidCastException("Attempted to create Entity Template whilst UseRawEntities = true!");
            RegisterTemplate<EntityYaml, EntityTemplate>(yaml);
        }
    }

    /// <summary>
    /// Creates copyable entity template from a provided Yaml file, and Template type.
    /// </summary>
    /// <param name="yaml"></param>
    /// <typeparam name="TYaml"></typeparam>
    /// <typeparam name="TTemplate"></typeparam>
    /// <exception cref="YamlException"></exception>
    public void RegisterTemplate<TYaml, TTemplate>(TYaml yaml)
        where TYaml : EntityYaml
        where TTemplate : EntityTemplate
    {
        // Get components.
        var components = BuildComponentsFromYaml(yaml);

        // Call dynamic constructors instead.
        var template = EntityTemplate.CreateFromYaml<TTemplate>(yaml, components);

        if (string.IsNullOrEmpty(template.Handle))
            throw new YamlException("Template must have ReferenceId");

        RegisterTemplate(template);
    }

    /// <summary>
    /// Creates a raw entity definition using provided yaml.
    /// </summary>
    /// <param name="yaml"></param>
    /// <typeparam name="TYaml"></typeparam>
    /// <typeparam name="TTemplate"></typeparam>
    /// <exception cref="YamlException"></exception>
    public void RegisterDefinition<TYaml, TTemplate>(TYaml yaml)
        where TYaml : EntityYaml
        where TTemplate : SKEntity
    {
        // Get components.
        var components = BuildComponentsFromYaml(yaml);

        var entity = new SKEntity(yaml, components);
        if (entity is null)
            throw new YamlException("SKEntity created was null!");

        RegisterTemplate(entity);
    }

    /// <summary>
    /// Helper for extracting components from a yaml file. Should work with any kind that inherits <see cref="EntityYaml"/>.
    /// Does NOT support other yaml types that implement this. This is for the ECS ONLY
    /// </summary>
    private Dictionary<Type, object> BuildComponentsFromYaml(EntityYaml yaml)
    {
        var components = new Dictionary<Type, object>();

        foreach (ComponentYaml yamlComponent in yaml.Components)
        {
            if (!_componentRegistry.RegisteredComponentTypesDictionary
                    .TryGetValue(yamlComponent.Type.Replace("Component", string.Empty), out Type? componentType))
            {
                Log($"Unknown component type: {yamlComponent.Type}", LOG.FILE_WARNING);
                continue;
            }

            object component = Activator.CreateInstance(componentType)
                               ?? throw new InvalidOperationException(
                                   $"Cannot create {componentType.Name} in {nameof(BuildComponentsFromYaml)}");

            // Handle component variables.
            foreach (var field in yamlComponent.Fields)
            {
                PropertyInfo? property = componentType.GetProperty(field.Key,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                // If the property can't be written to, then why bother.
                if (property?.CanWrite != true)
                    continue;

                try
                {
                    var converted = Convert.ChangeType(field.Value, property.PropertyType);
                    property.SetValue(component, converted);
                }
                catch
                {
                    Log($"Failed to set {field.Key} on {componentType.Name}", LOG.FILE_WARNING);
                }
            }

            components[componentType] = component; // Override.
        }

        return components;
    }

    /// <summary>
    /// Register an entity Definition raw or template according to <see cref="AEntityCommon"/>.
    /// </summary>
    private static void RegisterTemplate(AEntityCommon definition) => _definitions[definition.Handle] = definition;

    /// <summary>
    /// Safe[r] TryGet method to retrieve an Entity Definition *OR* Template using a reference id.
    /// </summary>
    /// <returns>True if a template was found. False if one was not. The output is also Null if one was not found.</returns>
    public static bool TryGetDefinition<T>(string referenceId, out T? definition) where T : AEntityCommon
    {
        var gotValue = _definitions.TryGetValue(referenceId, out AEntityCommon? entityCommon);
        definition = (T)entityCommon!;
        return gotValue;
    }

    /// <summary>
    /// Inquiry to the entity manager for a possible entity definition.
    /// </summary>
    /// <param name="handle">Reference ID that the Entity Registry SHOULD have.</param>
    /// <returns>True if a template was found. False if one was not.</returns>
    public static bool ContainsDefinition(string handle) => _definitions.ContainsKey(handle);

    #endregion
}