using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SKSSL.Scenes;
using SKSSL.YAML;
// ReSharper disable RedundantBaseConstructorCall

// ReSharper disable ClassNeverInstantiated.Global

namespace SKSSL.ECS;

/// <summary>
/// Instanced Entity representing an object present within game memory. Entities are contained within a
/// <see cref="World"/>, and contain <see cref="ComponentIndices"/> for pointing to component arrays.
/// </summary>
public record SKEntity : AEntityCommon
{
    /// <summary>
    /// Can be overwritten to allow for safe type-casting.
    /// </summary>
    public virtual Type EntityType => typeof(SKEntity);

    #region Fields

    /// <summary>
    /// Static Reference ID of this particular entry to a template reference.
    /// </summary>
    internal sealed override string Handle { get; init; } = null!;

    /// <summary>
    /// Unique runtime ID (only set on spawned instances, -1 on templates)
    /// </summary>
    public int RuntimeId { get; private set; } = -1;

    /// Manually assign runtime ID for if entity is created manually.
    /// Should NOT be called outside of <see cref="EntityManager"/>.
    protected internal void SetRuntimeId(int id) => RuntimeId = id;

    /// Defers back to the <see cref="RuntimeId"/> for compatability reasons between projects.
    public int Id => RuntimeId;

    /// <inheritdoc/>
    internal sealed override string NameKey { get; set; }

    /// <inheritdoc/>
    internal sealed override string DescriptionKey { get; set; }

    /// <inheritdoc/>
    /// Virtual for allow overrides, permitting manually-defined type-specific default components.
    public override IReadOnlyDictionary<Type, object> DefaultComponents { get; init; } = new Dictionary<Type, object>();

    /// <summary>
    /// Array of component indices. Index = ComponentTypeId&lt;T&gt;.Id, Value = slot in ComponentArray&lt;T&gt; (-1 if missing)
    /// <br/><br/>
    /// For every index, there is a unique component type.
    /// <seealso cref="ComponentArray{T}"/>
    /// </summary>
    public readonly int[] ComponentIndices;

    /// <summary>
    /// Reverse-reference back to the world that this entity inhabits.
    /// </summary>
    public IWorld? World;
    #endregion

    #region Constructors (Active Entities)

    /// <summary>
    /// Entities may use inherited template types to fill certain details in
    /// their constructors but always MUST call this base constructor. 
    /// </summary>
    /// <param name="id">Unique numerical of the entity.</param>
    /// <param name="count">Number of component indices in the game.</param>
    /// <param name="template">Provided template. Uses base <see cref="EntityTemplate"/> by default.</param>
    protected SKEntity(int id, int count, EntityTemplate template)
        : this(
            count: count,
            handle: template.Handle,
            name: template.NameKey,
            description:
            template.DescriptionKey,
            id)
    {
    }

    /// <summary>
    /// Manual constructor to create active entity.
    /// </summary>
    /// <param name="count"></param>
    /// <param name="handle"></param>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="id"></param>
    protected SKEntity(int count, string handle, string name, string description, int? id) : base()
    {
        Handle = handle;
        NameKey = name;
        DescriptionKey = description;

        // For raw definitions, which do not have runtime IDs.
        if (id.HasValue)
            RuntimeId = id.Value;

        ComponentIndices = new int[count];
        Array.Fill(ComponentIndices, -1); // ← All slots start as "missing"
    }

    #endregion

    #region Constructors (Raw Definition / Pseudo-Template)

    internal SKEntity(EntityYaml yaml, IReadOnlyDictionary<Type, object> components) : base(yaml, components)
    {
    }

    #endregion
    
    public void Initialize()
    {
    }

    public void Draw(SpriteBatch spriteBatch)
    {
    }

    public void Update(GameTime gameTime)
    {
    }
}