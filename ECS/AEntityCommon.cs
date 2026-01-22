using SKSSL.Localization;
using SKSSL.YAML;
using YamlDotNet.Serialization;

// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace SKSSL.ECS;

/// <summary>
/// Common abstraction for <see cref="SKEntity"/> and <see cref="EntityTemplate"/> objects.
/// Allows ECS to store one of either type in its definitions, depending on use-case.
/// </summary>
public abstract record AEntityCommon
{
    /// <summary>
    /// For direct raw-serialization of entities. Completely unused if prioritizing yaml templates.
    /// </summary>
    [YamlMember(Alias = "type")]
    public virtual string RawType { get; set; }
    
    /// <summary>
    /// Definition's Reference ID to later refer-to when making copies.
    /// </summary>
    [YamlMember(Alias = "id")]
    public abstract string Handle { get; init; }

    /// <summary>
    /// Localization for name.
    /// </summary>
    [YamlMember(Alias = "name")]
    public abstract string NameKey { get; set; }

    /// <summary>
    /// Localization for description.
    /// </summary>
    [YamlMember(Alias = "description")]
    public abstract string DescriptionKey { get; set; }

    /// <returns>Localized name from Name Key.</returns>
    public void GetName() => Loc.Get(NameKey);

    /// <returns>Localized Description from Description Key.</returns>
    public void GetDescription() => Loc.Get(DescriptionKey);

    /// Predefined class-specific dictionary of components.
    public abstract IReadOnlyDictionary<Type, object> DefaultComponents { get; init; }

    /// Constructor for Entity Yaml basic fields and default components. This is for definitions.
    protected AEntityCommon(EntityYaml yaml, IReadOnlyDictionary<Type, object> components)
    {
        // ReSharper disable VirtualMemberCallInConstructor
        Handle = yaml.ReferenceId;
        NameKey = yaml.Name;
        DescriptionKey = yaml.Description;
        DefaultComponents = components;
    }

    /// Blank constructor for Common Entity root. Avoid using this unless absolutely necessary.
    /// Used for creating active <see cref="SKEntity"/> instances in the ECS, where properties are set elsewhere.
    protected AEntityCommon()
    {
    }
}