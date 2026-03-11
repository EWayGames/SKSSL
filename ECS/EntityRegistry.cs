using System.Reflection;
using SKSSL.YAML;
using VYaml.Emitter;
using static SKSSL.DustLogger;

namespace SKSSL.ECS;

///<summary>
/// Registry of all Entity Definitions. Called by <see cref="EntityManager"/>.
/// <seealso cref="ComponentRegistry"/>
/// </summary>
public abstract class EntityRegistry
{
    internal static readonly Dictionary<string, AEntityCommon> Definitions = new();

    /// <inheritdoc cref="RegisterEntity{T, EntityYaml}"/>
    public static void RegisterEntity<T>(EntityYaml yaml) where T : AEntityCommon =>
        RegisterEntity<T, EntityYaml>(yaml);

    /// <summary>
    /// Handles registration of entity ambiguously between SKEntity and EntityTemplate derivations.
    /// </summary>
    /// <remarks>
    /// Provided that the Derived Type T is an EntityTemplate or SKEntity, will either call a direct  
    /// Calls <see cref="RegisterTemplate{TYaml}"/> but defaults <see cref="EntityYaml"/> type,
    /// or <see cref="RegisterRawEntity{TYaml}"/> to register an entity directly.
    /// 
    /// When registering specialized templates, use <see cref="RegisterTemplate{TYaml}"/> instead.
    /// </remarks>
    /// <param name="yaml">The yaml file of the template.</param>
    /// <typeparam name="T">Derived Type of entity intermediate type registered. Forces inheritance.</typeparam>
    /// <typeparam name="Y"></typeparam>
    public static void RegisterEntity<T, Y>(Y yaml)
        where T : AEntityCommon
        where Y : EntityYaml
    {
        if (!SSLGame.UseECS)
        {
            Log($"Attempted to register {yaml.Type} entity without initializing Entity Manager!", LOG.SYSTEM_WARNING);
            return;
        }

        Type derivedType = typeof(T);

        // Register raw entity
        if (typeof(SKEntity).IsAssignableFrom(derivedType))
        {
            RegisterRawEntity(yaml, derivedType);
        }
        // Attempt register of template
        else if (typeof(EntityTemplate).IsAssignableFrom(derivedType))
        {
            RegisterTemplate(yaml, derivedType);
        }
        else
        {
            throw new InvalidOperationException("Unknown type for registration");
        }
    }

    /// <summary>
    /// Creates copyable entity template from a provided Yaml file, and Template type.
    /// </summary>
    /// <param name="yaml">Yaml instance to process.</param>
    /// <param name="derivedType">Assumed derived type from EntityTemplate</param>
    /// <typeparam name="TYaml">Yaml Class</typeparam>
    /// <exception cref="YamlEmitterException">Thrown when ReferenceId / Handle not provided in YAML.</exception>
    public static void RegisterTemplate<TYaml>(TYaml yaml, Type derivedType) where TYaml : EntityYaml
    {
        if (!SSLGame.UseECS)
        {
            Log($"Attempted to register {yaml.Type} entity without initializing Entity Manager!", LOG.SYSTEM_WARNING);
            return;
        }

        // Build components
        var components = BuildComponentsFromEntityYaml(yaml);

        // Get dynamic template constructor.
        ConstructorInfo? ctor = derivedType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: [yaml.GetType(), components.GetType()],
            modifiers: null);
        if (ctor == null)
            throw new InvalidOperationException($"Type {derivedType} has no matching constructor.");

        // Call constructor
        var templateObj = ctor.Invoke([yaml, components]);

        if (templateObj is not EntityTemplate template)
            throw new YamlEmitterException("Created template is not an EntityTemplate!");

        if (string.IsNullOrEmpty(template.Handle))
            throw new YamlEmitterException("Invalid template!");

        RegisterDefinition(template);
    }

    /// <summary>
    /// Creates a raw entity definition using provided yaml.
    /// </summary>
    /// <param name="yaml"></param>
    /// <typeparam name="TYaml"></typeparam>
    /// <param name="derivedType"></param>
    /// <exception cref="YamlEmitterException"></exception>
    private static void RegisterRawEntity<TYaml>(TYaml yaml, Type derivedType)
        where TYaml : EntityYaml
    {
        var components = BuildComponentsFromEntityYaml(yaml);

        // Create instance dynamically
        object? instance = Activator.CreateInstance(
            derivedType, yaml, components // constructor parameters
        );

        // Cast to the derived type
        var entity = Convert.ChangeType(instance, derivedType);

        if (entity is not SKEntity typedEntity)
            throw new YamlEmitterException("Entity created was not of expected type!");

        RegisterDefinition(typedEntity);
    }

    /// <summary>
    /// Helper for extracting components from a yaml file. Should work with any kind that inherits <see cref="EntityYaml"/>.
    /// Does NOT support other yaml types that implement this. This is for the ECS ONLY
    /// </summary>
    private static Dictionary<Type, object> BuildComponentsFromEntityYaml(EntityYaml yaml)
    {
        var components = new Dictionary<Type, object>();

        if (yaml.Components == null)
        {
            yaml.Components = [];
            return components;
        }

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
                                   $"Cannot create {componentType.Name} in {nameof(BuildComponentsFromEntityYaml)}");

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
                    Log($"Failed to change type {field.Key} on {componentType.Name}", LOG.FILE_WARNING);
                }
            }

            components[componentType] = component; // Override.
        }

        return components;
    }

    /// <summary>
    /// Register an entity Definition raw or template according to <see cref="AEntityCommon"/>.
    /// </summary>
    private static void RegisterDefinition(AEntityCommon definition) => Definitions[definition.Handle] = definition;

    /// <summary>
    /// Safe[r] TryGet method to retrieve an Entity Definition *OR* Template using a reference id.
    /// </summary>
    /// <returns>True if a template was found. False if one was not. The output is also Null if one was not found.</returns>
    public static bool TryGetDefinition<T>(string referenceId, out T? definition) where T : AEntityCommon
    {
        var gotValue = EntityManager.Definitions.TryGetValue(referenceId, out AEntityCommon? _);

        if (EntityManager.Definitions[referenceId] is T typed)
        {
            definition = typed;
            return true;
        }

        definition = null;
        return gotValue;
    }

    /// <summary>
    /// Inquiry to the entity manager for a possible entity definition.
    /// </summary>
    /// <param name="handle">Reference ID that the Entity Registry SHOULD have.</param>
    /// <returns>True if a template was found. False if one was not.</returns>
    public static bool ContainsDefinition(string handle) => EntityManager.Definitions.ContainsKey(handle);
}