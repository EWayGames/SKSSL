using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using static SKSSL.DustLogger; // I like my DustLogger. I will use it everywhere.
using Type = System.Type; // For reflection purposes.

// ReSharper disable InvalidXmlDocComment

namespace SKSSL.ECS;

#region ComponentArray<> Definition

/// <summary>
/// Contains the component instances for each registered entity.
/// </summary>
/// <remarks>This list is instantiated. It gets pretty complicated, but is essentially used to store component type data.</remarks>
/// <typeparam name="T">Type of components being stored in this particular list.</typeparam>
/// <seealso cref="List"/>
public class ComponentArray<T> : IComponentArray where T : struct, ISKComponent
{
    /// <summary>
    /// Constructor of Component Array that creates empty array on instantiation.
    /// </summary>
    /// <param name="capacity">Number of items maximum this array can contain.</param>
    public ComponentArray(int capacity) => _items = new T[capacity];

    /// Empty constructor that forces default capacity to 1024.
    public ComponentArray() : this(1024)
    {
    }

    /// Private list of contained items.
    private T[] _items;

    /// <summary>
    /// Number of entries present within the component array.
    /// </summary>
    public int Count { get; private set; } = 0;

    /// <summary>
    /// Expands list of available items.
    /// </summary>
    /// <returns>Reference to <see cref="_items"/> slot.</returns>
    public ref T Add()
    {
        // Double item space every time it's over max.
        if (Count >= _items.Length)
            Array.Resize(ref _items, _items.Length * 2);

        return ref _items[Count++];
    }

    /// <summary>
    /// Removes component by setting it to default (nulls out value).
    /// Index remains valid but component is considered "removed".
    /// You MUST check IsValid(index) before using GetAt().
    /// </summary>
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _items.Length)
            throw new IndexOutOfRangeException($"Index {index} out of bounds (array size: {_items.Length})");

        _items[index] = default;
    }

    /// <param name="index">Index of desired registered type.</param>
    /// <returns>Type definition at index.</returns>
    /// <exception cref="IndexOutOfRangeException">If (<see cref="Count"/> &gt; index &lt; 0 )</exception>
    public ref T GetAt(int index)
    {
        if (index < 0 || index >= Count)
            throw new IndexOutOfRangeException($"{nameof(GetAt)} index #{index} out of range.");

        return ref _items[index];
    }

    public ref T this[int index] => ref _items[index];
    object IComponentArray.this[int index] => _items[index];
}

#endregion

/// Central registry that creates, handles, gets, an deletes components.
public class ComponentRegistry
{
    #region Fast Component Creation

    private static readonly Dictionary<Type, Func<object>> _creators = new();

    internal static object FastCreate(Type type)
    {
        if (_creators.TryGetValue(type, out var creator))
            return creator();

        Func<object> newCreator;

        // Try to find parameterless constructor
        ConstructorInfo? ctor = type.GetConstructor(Type.EmptyTypes);
        if (ctor != null)
        {
            // Fast path: compile expression tree once
            NewExpression newExpr = Expression.New(ctor);
            var lambda = Expression.Lambda<Func<object>>(newExpr);
            newCreator = lambda.Compile();
        }
        else
        {
            // Slow but safe fallback: use Activator
            newCreator = () => Activator.CreateInstance(type)
                               ?? throw new InvalidOperationException(
                                   $"Cannot instantiate {type.Name}: no parameterless constructor and Activator failed.");
        }

        // Cache for next time (thread-safe enough)
        _creators[type] = newCreator;

        return newCreator();
    }

    #endregion

    private readonly Dictionary<Type, int> _typeToId = new();
    private readonly Dictionary<int, Type> _idToType = new();
    private readonly Dictionary<string, Type> _registeredComponents = new();

    /// All registered component class-types contained in the system.
    public IReadOnlyDictionary<string, Type> RegisteredComponentTypesDictionary => _registeredComponents;

    /// <summary>
    /// Dictionary of all active components.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, object>
        _activeComponentArrays = new(); // Type -> ComponentArray<T>

    private static int _nextTypeId = 0;
    private static bool Initialized { get; set; } = false;


    /// Number of Components registered in the registry. Gets next available Component ID.
    public static int Count => _nextTypeId;

    /// <param name="id">ID of Registered Component</param>
    /// <returns>Null or Type Definition based on provided ID.</returns>
    public Type? GetType(int id) => _idToType.GetValueOrDefault(id);

    #region Component Registration and Assembly Checks

    /// <summary>
    /// Uses reflection to get all defined components in the (relevant) assemblies, and initializes them.
    /// </summary>
    public void InitializeComponents()
    {
        if (Initialized) return;
        Initialized = true;

        // Keeping stopwatch timer for releases. It's nice to have.
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(IsRelevantAssembly)
            .ToArray();

        Log($"Scanning {assemblies.Length} assemblies for components...");

        int componentCount = 0;
        foreach (Assembly assembly in assemblies)
        {
            var types = GetTypesSafe(assembly);
            foreach (Type type in types)
            {
                if (!IsValidComponent(type))
                    continue;
                GetOrRegister(type); // Registers
                componentCount++;
            }
        }

        stopwatch.Stop();
        Initialized = true;

        // Logging
        Log($"Registered {componentCount} components in {stopwatch.ElapsedMilliseconds}ms");
        Log("Registered types:");
        foreach (Type type in _registeredComponents.Values)
            Log($"  {type.Name} -> ID {GetOrRegister(type)}");

        return;

        Type[] GetTypesSafe(Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null).ToArray()!;
            }
            catch
            {
                return [];
            }
        }
    }

    /// <summary>
    /// Filters game assemblies. Includes hard-coded assemblies that use SKSSL, KBSL, or Kuiperbilt.
    /// </summary>
    private static bool IsRelevantAssembly(Assembly assembly)
    {
        string name = assembly.GetName().Name ?? "";

        // Skip problematic/problematic assemblies
        if (name.StartsWith("MonoGame.", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("mscorlib") ||
            name.StartsWith("netstandard") ||
            assembly.IsDynamic ||
            assembly.ReflectionOnly)
            return false;

        // Hard-coding our supported assemblies.
        return name.Contains("SKSSL") ||
               name.Contains("KBSL") ||
               name.Contains("Kuiperbilt");
        // TODO: I should really add support for an additional user-defined assemblies. Make a virtual call? A wrapper maybe?
    }

    private static bool IsValidComponent(Type t) =>
        typeof(ISKComponent).IsAssignableFrom(t) &&
        !t.IsAbstract &&
        !t.IsInterface &&
        !t.IsGenericTypeDefinition;

    #endregion

    // I just couldn't choose which to implement. There's multiple ways to do this and i am picky about performance.
    //  The compiler shall handle the rest of this, consequences be damned.

    #region ComponentArray Activators

#pragma warning disable CS0162 // Unreachable code detected
#pragma warning disable SYSLIB0050

    private object GetOrCreateComponentArrayActivator(Type componentType)
    {
        return _activeComponentArrays.GetOrAdd(componentType, CreateArray);

        static object CreateArray(Type t)
        {
            Type arrayType = typeof(ComponentArray<>).MakeGenericType(t);
            return Activator.CreateInstance(arrayType)!;
        }
    }

    private object GetOrCreateComponentArrayRuntime(Type componentType)
    {
        return _activeComponentArrays.GetOrAdd(componentType, t =>
        {
            Type arrayType = typeof(ComponentArray<>).MakeGenericType(t);
            return RuntimeHelpers.GetUninitializedObject(arrayType);
            // Requires manual call .Initialize() or just let List<T> default init
        });
    }
#pragma warning restore SYSLIB0050
#pragma warning restore CS0162 // Unreachable code detected

    #endregion

    #region Pseudo-Extensions

    #region Get Methods

    /// <summary>
    /// Used for extensions that attempt to retrieve a defined component from an entity.
    /// </summary>
    internal bool TryGetComponentIndex(SKEntity entity, Type componentType, out int index)
    {
        var compId = entity.ComponentIndices[_typeToId.TryGetValue(componentType, out index) ? index : -1];

        // Effectively HasComponent() call, but index is reused later so... ¯\_(ツ)_/¯
        if (compId != -1)
            return true;

        Log($"Tried to get bad component index. Entity {entity.RuntimeId} missing component type {componentType.Name}",
            LOG.GENERAL_WARNING);
        return false;
    }

    /// <summary>
    /// Convenient version of <see cref="GetOrCreateComponentArray"/> that which it calls.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public ComponentArray<T> GetOrCreateComponentArray<T>() where T : struct, ISKComponent
        => (ComponentArray<T>)GetOrCreateComponentArray(typeof(T));

    /// <summary>
    /// Gets or creates the ComponentArray<T> for the given component type.
    /// Called only once per component type.
    /// </summary>
    public object GetOrCreateComponentArray(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        return _activeComponentArrays.GetOrAdd(componentType, CreateComponentArray);

        static object CreateComponentArray(Type t)
        {
            // Build ComponentArray<componentType>
            Type arrayType = typeof(ComponentArray<>).MakeGenericType(t);

            // Call the public parameterless constructor
            return Activator.CreateInstance(arrayType)
                   ?? throw new InvalidOperationException($"Failed to instantiate ComponentArray<{t.Name}>");
        }
    }

    /// <param name="array"><see cref="ComponentArray{T}"/> of Active components.</param>
    /// <param name="index">Index of component provided by an <see cref="SKEntity"/> up the chain.</param>
    /// <returns>
    /// Gets a component using a <see cref="ComponentArray{T}"/> and provided index of the component's position
    /// within the array.
    /// </returns>
    internal static ISKComponent? GetComponentAt(object array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        return ((IComponentArray)array)[index] as ISKComponent;
    }

    internal static T GetComponentAt<T>(ComponentArray<T> array, int index) where T : struct, ISKComponent
        => array.GetAt(index);

    /// <returns>ID of component defined in type dictionary, or -1.</returns>
    /// <exception cref="ArgumentException">Provided type not present in dictionary.</exception>
    public int GetComponentTypeId(Type componentType)
    {
        if (!_typeToId.TryGetValue(componentType, out int id))
            throw new ArgumentException($"Component type {componentType.Name} not registered!");
        return id;
    }

    /// <inheritdoc cref="GetComponentTypeId(type)"/>
    private int GetComponentTypeId<T>() => GetComponentTypeId(typeof(T));

    /// <summary>
    /// Multipurpose method used to retrieve an ID of a registered type, or additionally
    /// register said-type before returning.
    /// </summary>
    /// <param name="type">A class-type definition hopefully implementing <see cref="ISKComponent"/>.</param>
    /// <returns>Integer ID of (what should be) a Type implementing <see cref="ISKComponent"/>.</returns>
    private int GetOrRegister(Type type)
    {
        if (_typeToId.TryGetValue(type, out int id))
            return id;

        id = Interlocked.Increment(ref _nextTypeId) - 1;
        // For reverse-checking in entities.
        _typeToId[type] = id;
        // For entity ID lists to types.
        _idToType[id] = type;
        // For deserializing entities. Renames TestComponent -> Test for deserialization reasons.
        _registeredComponents[type.Name.Replace("Component", string.Empty)] = type;

        return id;
    }

    #endregion

    // ""Unsafe"" get methods.

    #region More Get Methods

    /// <summary>
    /// Acts like <see cref="GetComponent{T}"/> but directly expects a provided type.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="componentType">The runtime type of the component (must implement ISKComponent).</param>
    /// <returns>The component instance (boxed as ISKComponent), or null if not found (or throws based on preference).</returns>
    /// <exception cref="InvalidOperationException">Thrown if the entity does not have the component or type is invalid.</exception>
    public ISKComponent? GetComponent(SKEntity entity, Type componentType)
    {
        if (!typeof(ISKComponent).IsAssignableFrom(componentType))
            throw new ArgumentException($"Type {componentType.Name} must implement ISKComponent.",
                nameof(componentType));

        if (!TryGetComponentIndex(entity, componentType, out var index))
            return null;

        var array = _activeComponentArrays[componentType];
        return GetComponentAt(array, index);
    }

    /// <summary>
    /// Gets the component of the specified type from this entity.
    /// </summary>
    /// <typeparam name="T">The component type (must implement ISKComponent).</typeparam>
    /// <returns>A reference to the component if found; otherwise throws.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the entity does not have the component.</exception>
    public ref T GetComponent<T>(SKEntity entity) where T : struct, ISKComponent
    {
        if (!TryGetComponentIndex(entity, typeof(T), out var index))
            Log($"{nameof(GetComponent)} is going through with an invalid index! Not good!", LOG.SYSTEM_WARNING);

        return ref GetOrCreateComponentArray<T>().GetAt(index);
    }


    /// <summary>
    /// Acts like <see cref="GetComponent{T}"/> but more direct and possibly dangerous.
    /// </summary>
    /// <param name="entity">Entity whose components are queried.</param>
    /// <typeparam name="T">Type of component.</typeparam>
    /// <returns>Component reference.</returns>
    public ref T GetComponentRef<T>(SKEntity entity) where T : struct, ISKComponent
    {
        var array = (ComponentArray<T>)_activeComponentArrays[typeof(T)];
        int idx = entity.ComponentIndices[GetComponentTypeId<T>()];
        return ref array.GetAt(idx);
    }

    #endregion

    // ""Unsafe"" add methods.

    #region AddComponent

    /// <summary>
    /// Adds a new component of type T to the entity and returns a mutable reference to it.
    /// </summary>
    /// <param name="entity">Entity that a component is added to.</param>
    /// <typeparam name="T">The component type (must be struct and implement ISKComponent).</typeparam>
    /// <returns>A reference to the newly added component (zero-initialized).</returns>
    [Obsolete("Use AddComponent(entity, type) instead.")]
    public ref T AddComponent<T>(SKEntity entity) where T : struct, ISKComponent
    {
        var array = GetOrCreateComponentArray<T>();

        // Calls a custom Add() -> returns ref to the new slot
        ref T componentSlot = ref array.Add();

        // Default-initialize (zero-init for struct — fast and safe)
        componentSlot = default; // or = new T(); Maybe?

        int typeId = GetComponentTypeId<T>();
        entity.ComponentIndices[typeId] = array.Count - 1; // Count already incremented, indexed by zero.

        // Basically returns a component placed in a slot equal to the index reference contained in the entity.
        return ref componentSlot;
    }

    /// <summary>
    /// Adds a component of the specified runtime type and returns the new component boxed instance.
    /// </summary>
    /// <param name="entity">Entity that a component is added to.</param>
    /// <param name="componentType">The runtime type of the component to add.</param>
    /// <returns>The newly added component instance (boxed as object).</returns>
    /// <exception cref="ArgumentException">If the type doesn't implement ISKComponent.</exception>
    /// <exception cref="InvalidOperationException">If reflection fails or array is missing.</exception>
    public object AddComponent(SKEntity entity, Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        if (!typeof(ISKComponent).IsAssignableFrom(componentType))
            throw new ArgumentException($"Type {componentType.Name} does not implement ISKComponent");

        // Get or create the component array
        object arrayObj = GetOrCreateComponentArray(componentType);

        // Call custom Add() via reflection to allocate slot
        MethodInfo addMethod = arrayObj.GetType()
                                   .GetMethod("Add", Type.EmptyTypes)
                               ?? throw new InvalidOperationException(
                                   $"Missing Add() on ComponentArray<{componentType.Name}>");

        // Increments count and returns discarded [ref]erence.
        addMethod.Invoke(arrayObj, null);

        // Get the new index
        int newIndex = (int)(arrayObj.GetType().GetProperty("Count")?.GetValue(arrayObj)
                             ?? throw new InvalidOperationException(
                                 $"No Count field in ComponentArray<{componentType.Name}> found."))
                       - 1; // -1 due to zero-based indexing.

        // Store index in entity
        int typeId = GetComponentTypeId(componentType);
        entity.ComponentIndices[typeId] = newIndex;

        // Return the actual component instance (by value) — perfect for initialization
        MethodInfo getAtMethod = arrayObj.GetType().GetMethod("GetAt")
                                 ?? throw new InvalidOperationException(
                                     $"Missing GetAt(int) on ComponentArray<{componentType.Name}>");

        // Returns ref T, but boxed to object
        object component = getAtMethod.Invoke(arrayObj, [newIndex])
                           ?? throw new InvalidOperationException("GetAt returned null");

        return component;
    }

    #endregion

    // AddComponent calls surrounded in Try-Catch.

    #region TryAddComponent

    public bool TryAddComponent<T>(SKEntity entity, out T component) where T : struct, ISKComponent
    {
        try
        {
            component = (T)AddComponent(entity, typeof(T));
            return true;
        }
        catch
        {
            component = default;
            return false;
        }
    }

    public bool TryAddComponent(SKEntity entity, Type componentType, out object? component)
    {
        try
        {
            component = AddComponent(entity, componentType);
            return true;
        }
        catch
        {
            component = null;
            return false;
        }
    }

    #endregion

    // No Try-Catch needed.

    #region TryGetComponent

    /// <summary>
    /// Attempts to safely retrieve a component from an entity.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="component">Component output for use.</param>
    /// <typeparam name="T">Expected Component Type within entity.</typeparam>
    /// <returns>False if a component wasn't found.</returns>
    public bool TryGetComponent<T>(SKEntity entity, out T component) where T : struct, ISKComponent
    {
        component = default;

        int typeId = GetComponentTypeId<T>();
        int index = entity.ComponentIndices[typeId];

        if (index == -1)
            return false;

        component = GetOrCreateComponentArray<T>().GetAt(index);
        return true;
    }

    /// <summary>
    /// Attempts to retrieve a component using explicit type, outputting null interface of <see cref="ISKComponent"/>
    /// if not found.
    /// </summary>
    public bool TryGetComponent(SKEntity entity, Type componentType, out ISKComponent? component)
    {
        component = null;

        if (!typeof(ISKComponent).IsAssignableFrom(componentType))
            return false;

        int typeId = GetComponentTypeId(componentType);
        int index = entity.ComponentIndices[typeId];

        if (index == -1)
            return false;

        var array = _activeComponentArrays[componentType];
        component = GetComponentAt(array, index);
        return true;
    }

    #endregion

    // Best not to use this.

    #region GetAllComponents

    /// <summary>
    /// Gets a list of all components in an entity as a snapshot at the time of the call meaning changes to the entity
    /// won't affect the returned list. Will require casting. Assumes that all returned components are valid.
    /// </summary>
    /// <returns>A list of all components currently attached to this entity (boxed as object).</returns>
    /// <remarks>
    /// Components are returned boxed. Pattern-matching or casting will be needed to access specific types.
    /// This is intended for debugging, serialization, inspection, or rare runtime needs.
    /// For performance, use <see cref="GetComponent{T}"/> instead.
    /// </remarks>
    public ref List<object> GetAllComponents(SKEntity entity)
    {
        // Return a ref to a static thread-local list to avoid allocations in hot paths
        // Still safe since it's ref-local-scoped.
        ref var resultList = ref ThreadLocalList<object>.GetOrCreate();

        resultList.Clear();
        var indices = entity.ComponentIndices;

        foreach ((int typeId, Type? componentType) in _idToType)
        {
            // Checking to make sure the thing has it.
            int indexOfComponentEntry = indices[typeId];
            if (indexOfComponentEntry == -1)
                continue; // Short-circuit

            var array = _activeComponentArrays[componentType];
            var component = GetComponentAt(array, indexOfComponentEntry);
            if (component is not null)
                resultList.Add(component);
        }

        return ref resultList;
    }

    private static class ThreadLocalList<T>
    {
        [ThreadStatic] private static List<T>? _list;

        public static ref List<T> GetOrCreate()
        {
            _list ??= new List<T>(8);
            return ref _list!;
        }
    }

    #endregion

    #endregion
}

/// <summary>
/// Interface for indexable component array that stores Component IDs.
/// </summary>
public interface IComponentArray
{
    object? this[int index] { get; }
}