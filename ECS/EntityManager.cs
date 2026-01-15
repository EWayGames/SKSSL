using System.Reflection;
using SKSSL.Extensions;
using SKSSL.Scenes;
using SKSSL.YAML;
using YamlDotNet.Core;
using static SKSSL.DustLogger;

namespace SKSSL.ECS;

/// <summary>
/// Instantiated Manager for all <see cref="SKEntity"/> instances contained within it. Fundamental ECS
/// infrastructure that currently softly requires a <see cref="BaseWorld"/> to be contained-in.
/// </summary>
public class EntityManager
{
    private static int _nextId = 0;
    private readonly List<SKEntity> _allEntities = [];

    /// Get all active entities.
    public IReadOnlyList<SKEntity> AllEntities => _allEntities;

    /// All entity template definitions.
    public static IReadOnlyDictionary<string, EntityTemplate> Definitions => _definitions;


    /// <summary>
    /// Remove all entities contained within this entity Manager.
    /// </summary>
    public void WipeAllEntities()
    {
        // TODO: MIGHT require additional unloading? The list just clears references for the GC. Components these
        //  entities had aren't clear, and created IDs aren't reset back to start from 0.
        _allEntities.Clear();
    }

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
        EntityTemplate template = GetTemplate(handle);
        SKEntity entity = CreateEntity(template, world);
        _allEntities.Add(entity);
        return entity;
    }

    #region Entity Registry

    private static readonly Dictionary<string, EntityTemplate> _definitions = new();

    /// <summary>
    /// Calls <see cref="RegisterTemplate{TYaml, TTemplate}"/> with a default to the <see cref="EntityYaml"/> type.
    /// </summary>
    /// <param name="yaml">The yaml file of the template.</param>
    /// <typeparam name="T">Type of template being registered. Forces inheritance.</typeparam>
    /// <exception cref="ArgumentException"></exception>
    public static void RegisterTemplate<T>(EntityYaml yaml) where T : EntityTemplate
        => RegisterTemplate<EntityYaml, T>(yaml);

    /// <summary>
    /// Creates copyable entity template from a provided Yaml file, and Template type.
    /// </summary>
    /// <param name="yaml"></param>
    /// <typeparam name="TYaml"></typeparam>
    /// <typeparam name="TTemplate"></typeparam>
    /// <exception cref="YamlException"></exception>
    public static void RegisterTemplate<TYaml, TTemplate>(TYaml yaml)
        where TYaml : EntityYaml
        where TTemplate : EntityTemplate
    {
        // Get components.
        var components = BuildComponentsFromYaml(yaml);

        // Call dynamic constructors instead.
        var template = EntityTemplate.CreateFromYaml<TTemplate>(yaml, components);

        if (string.IsNullOrEmpty(template.ReferenceId))
            throw new YamlException("Template must have ReferenceId");

        RegisterTemplate(template);
    }

    /// <summary>
    /// Helper for extracting components from a yaml file. Should work with any kind that inherits <see cref="EntityYaml"/>.
    /// Does NOT support other yaml types that implement this. This is for the ECS ONLY
    /// </summary>
    private static Dictionary<Type, object> BuildComponentsFromYaml(EntityYaml yaml)
    {
        var components = new Dictionary<Type, object>();

        foreach (ComponentYaml yamlComponent in yaml.Components)
        {
            if (!ComponentRegistry.RegisteredComponentTypesDictionary
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
    private static void RegisterTemplate(EntityTemplate template) => _definitions[template.ReferenceId] = template;

    /// <summary>
    /// Retrieves a template from the defined templates list. Throws an exception.
    /// <remarks>I suggest using <see cref="TryGetTemplate"/> instead and add additional handling for safety.</remarks>
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when template not found using provided reference id.</exception>
    public static EntityTemplate GetTemplate(string referenceId)
    {
        if (!TryGetTemplate(referenceId, out EntityTemplate? template))
            throw new KeyNotFoundException(
                $"Call on {nameof(GetTemplate)} found no template for reference id: {referenceId}");

        return template!;
    }

    /// <summary>
    /// Safe[r] TryGet method to retrieve a template using a reference id.
    /// </summary>
    /// <returns>True if a template was found. False if one was not. The output is also Null if one was not found.</returns>
    public static bool TryGetTemplate(string referenceId, out EntityTemplate? template)
        => _definitions.TryGetValue(referenceId, out template);

    /// <summary>
    /// Inquiry to the entity manager for a possible entity definition.
    /// </summary>
    /// <param name="handle">Reference ID that the Entity Registry SHOULD have.</param>
    /// <returns>True if a template was found. False if one was not.</returns>
    public static bool ContainsTemplate(string handle) => _definitions.ContainsKey(handle);

    #endregion
}