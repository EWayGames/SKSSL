using SKSSL.Localization;
using SKSSL.YAML;

// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace SKSSL.ECS;

/// <summary>
/// Common interface for <see cref="SKEntity"/> and <see cref="EntityTemplate"/> objects.
/// Allows ECS to store one of either type in its definitions, depending on use-case.
/// </summary>
public abstract record AEntityCommon
{
    /// <summary>
    /// Definition's Reference ID to later refer-to when making copies.
    /// </summary>
    internal abstract string Handle { get; init; }

    /// <summary>
    /// Localization for name.
    /// </summary>
    internal abstract string NameKey { get; set; }

    /// <summary>
    /// Localization for description.
    /// </summary>
    internal abstract string DescriptionKey { get; set; }

    public void GetName() => Loc.Get(NameKey);
    public void GetDescription() => Loc.Get(DescriptionKey);

    /// Predefined class-specific dictionary of components.
    public abstract IReadOnlyDictionary<Type, object> DefaultComponents { get; init; }

    protected AEntityCommon(EntityYaml yaml, IReadOnlyDictionary<Type, object> components)
    {
        // ReSharper disable VirtualMemberCallInConstructor
        Handle = yaml.ReferenceId;
        NameKey = yaml.Name;
        DescriptionKey = yaml.Description;
        DefaultComponents = components;
    }

    protected AEntityCommon()
    {
    }
}