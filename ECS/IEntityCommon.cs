using SKSSL.Localization;
using SKSSL.YAML;

namespace SKSSL.ECS;

/// <summary>
/// Common interface for <see cref="SKEntity"/> and <see cref="EntityTemplate"/> objects.
/// Allows ECS to store one of either type in its definitions, depending on use-case.
/// </summary>
public interface IEntityCommon
{
    /// <summary>
    /// Definition's Reference ID to later refer-to when making copies.
    /// </summary>
    internal string Handle { get; init; }
    
    /// <summary>
    /// Localization for name.
    /// </summary>
    internal string NameKey { get; set; }

    /// <summary>
    /// Localization for description.
    /// </summary>
    internal string DescriptionKey { get; set; }

    public void GetName() => Loc.Get(NameKey);
    public void GetDescription() => Loc.Get(DescriptionKey);
    
    /// Predefined class-specific dictionary of components.
    public IReadOnlyDictionary<Type, object> DefaultComponents { get; init; }
}