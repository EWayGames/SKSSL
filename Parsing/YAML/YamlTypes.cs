using System.Drawing;
using RenderingLibrary.Graphics;
using VYaml.Annotations;
using Color = Microsoft.Xna.Framework.Color;
// ReSharper disable PropertyCanBeMadeInitOnly.Global

// ReSharper disable NullableWarningSuppressionIsUsed

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace SKSSL.YAML;

/// <summary>
/// (De)Serializable data type read from YAML files. Further entries that inherit this may have optional parameters
/// implemented either through Nullable&lt;T&gt; variables, or variables with default provided values.
/// <code>
/// Yaml Entry Example:
/// - type: (string)
///   id: (string)
/// </code>
/// </summary>
[YamlObject]
public partial record BaseYamlEntry
{
    /// Explicit type definition for this entry.
    public string Type { get; set; }

    /// Searchable, indexable ID.
    [YamlMember(name: "id")]
    public string ReferenceId { get; set; }
}

/// <summary>
/// <inheritdoc cref="BaseYamlEntry"/>
/// <code>
/// In Addition To:
///   name: (localization)
///   description: (localization)
/// </code>
/// </summary>
[YamlObject]
public partial record BaseLocalizedYamlEntry : BaseYamlEntry
{
    /// Non-localized name key.
    public string Name { get; set; }

    /// Non-localized description key.
    public string Description { get; set; }
}

/// <summary>
/// <inheritdoc cref="BaseLocalizedYamlEntry"/>
/// <code>
/// In Addition To:
///   color: "#RRGGBB"
/// </code>
/// </summary>
[YamlObject]
public partial record BaseLocalizedColorableYamlEntry : BaseLocalizedYamlEntry
{
    /// <summary>
    /// Raw HTML (#RRGGBB) color when viewed on the map or in graphs.
    /// </summary>
    [YamlMember(name: "color")]
    public string YamlColor { get; set; }

    private Color? _color;

    [YamlIgnore]
    public Color Color
    {
        get
        {
            _color ??= ColorTranslator.FromHtml(YamlColor).ToXNA();
            return _color.Value;
        }
        set
        {
            YamlColor = value.ToString();
            _color = value;
        }
    }
}


public class ComponentYaml
{
    // e.g., "RenderableComponent" but named "Renderable"; it's stripped of the "Component" suffix.
    [YamlMember(name: "type")] public string Type { get; set; }

    // Dictionary for flexible fields (for varied components)
    /// <summary>
    /// Variable Fields contained in the record that defines the component.
    /// Private code will require provided component documentation for user-defined entities.
    /// </summary>
    /// <remarks>
    /// It's funky. Field names should be about as 1:1 to the actual component's fields.
    /// As far as I know, it's case sensitive.
    /// </remarks>
    public Dictionary<string, object> Fields { get; set; } = new();
}

/// <summary>
/// <inheritdoc cref="BaseLocalizedYamlEntry"/>
/// <code>
/// In Addition To:
///   components: (Component Yaml Entries)
///     - type: (string)
///       field_1: (varies)
///       field_2: (varies)
///       field_3: (varies)
/// # (Note: Component fields vary between component type.)
/// </code>
/// </summary>
[YamlObject]
public partial record EntityYaml : BaseLocalizedYamlEntry
{
    /// <summary>
    /// Optional field for <see cref="EntityYaml"/> which is exclusively for entities.
    /// </summary>
    [YamlMember(name: "components")]
    public List<ComponentYaml> Components { get; set; } = [];
}