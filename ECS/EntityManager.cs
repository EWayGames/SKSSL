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

    /// All inactive Entity Definitions, which ubiquitously inherit <see cref="IEntityCommon"/>.
    public static IReadOnlyDictionary<string, IEntityCommon> Definitions => _definitions;

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

        return FinalizeEntityWorldAndComponents(ref entity, definition.DefaultComponents);
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

        return FinalizeEntityWorldAndComponents(ref entity, template.DefaultComponents);
    }

    /// <summary>
    /// Generalization of final steps in completing entity creation.
    /// </summary>
    private SKEntity FinalizeEntityWorldAndComponents(
        ref SKEntity entity,
        IReadOnlyDictionary<Type, object> defaultComponents)
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

    private static readonly Dictionary<string, IEntityCommon> _definitions = new();

    /// <summary>
    /// Calls <see cref="RegisterTemplate{TYaml, TTemplate}"/> with a default to the <see cref="EntityYaml"/> type.
    /// </summary>
    /// <param name="yaml">The yaml file of the template.</param>
    /// <typeparam name="T">Type of template being registered. Forces inheritance.</typeparam>
    /// <exception cref="ArgumentException"></exception>
    public void RegisterTemplate<T>(EntityYaml yaml) where T : EntityTemplate => RegisterTemplate<EntityYaml, T>(yaml);

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
    /// Register a template.
    /// </summary>
    private static void RegisterTemplate(EntityTemplate template) => _definitions[template.Handle] = template;

    /// <summary>
    /// Retrieves a template from the defined templates list. Throws an exception.
    /// <remarks>I suggest using <see cref="TryGetDefinition{T}"/> instead and add additional handling for safety.</remarks>
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when template not found using provided reference id.</exception>
    public static EntityTemplate GetTemplate(string referenceId)
    {
        if (!TryGetDefinition(referenceId, out EntityTemplate? template))
            throw new KeyNotFoundException(
                $"Call on {nameof(GetTemplate)} found no template for reference id: {referenceId}");

        return template!;
    }

    /// <summary>
    /// Safe[r] TryGet method to retrieve an Entity Definition *OR* Template using a reference id.
    /// </summary>
    /// <returns>True if a template was found. False if one was not. The output is also Null if one was not found.</returns>
    public static bool TryGetDefinition<T>(string referenceId, out T? definition) where T : IEntityCommon
    {
        var gotValue = _definitions.TryGetValue(referenceId, out IEntityCommon? entityCommon);
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