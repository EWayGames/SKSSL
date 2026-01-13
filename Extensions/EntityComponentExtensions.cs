// ReSharper disable UnusedMember.Global

using System.Reflection;
using static SKSSL.DustLogger;

namespace SKSSL.ECS;

/// <summary>
/// Includes extension and overload methods for handling <see cref="SKEntity"/> objects and their components.
/// </summary>
public static partial class ComponentRegistry
{
    // Convenient methods for detecting if an entity has a component.

    #region HasComponent

    public static bool HasComponent<T>(this SKEntity entity) where T : struct, ISKComponent
        => HasComponent(entity, typeof(T));

    public static bool HasComponent(this SKEntity entity, Type componentType)
        => entity.ComponentIndices[GetComponentTypeId(componentType)] != -1;

    #endregion

    // ""Unsafe"" add methods.

    #region AddComponent

    /// <summary>
    /// Adds a new component of type T to the entity and returns a mutable reference to it.
    /// </summary>
    /// <param name="entity">Entity that a component is added to.</param>
    /// <typeparam name="T">The component type (must be struct and implement ISKComponent).</typeparam>
    /// <returns>A reference to the newly added component (zero-initialized).</returns>
    public static ref T AddComponent<T>(this SKEntity entity) where T : struct, ISKComponent
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
    public static object AddComponent(this SKEntity entity, Type componentType)
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

    public static bool TryAddComponent<T>(this SKEntity entity, out T component) where T : struct, ISKComponent
    {
        try
        {
            component = entity.AddComponent<T>();
            return true;
        }
        catch
        {
            component = default;
            return false;
        }
    }

    public static bool TryAddComponent(this SKEntity entity, Type componentType, out object? component)
    {
        try
        {
            component = entity.AddComponent(componentType);
            return true;
        }
        catch
        {
            component = null;
            return false;
        }
    }

    #endregion

    // ""Unsafe"" get methods.

    #region GetComponent

    /// <summary>
    /// Acts like <see cref="GetComponent{T}"/> but directly expects a provided type.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="componentType">The runtime type of the component (must implement ISKComponent).</param>
    /// <returns>The component instance (boxed as ISKComponent), or null if not found (or throws based on preference).</returns>
    /// <exception cref="InvalidOperationException">Thrown if the entity does not have the component or type is invalid.</exception>
    public static ISKComponent? GetComponent(this SKEntity entity, Type componentType)
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
    public static ref T GetComponent<T>(this SKEntity entity) where T : struct, ISKComponent
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
    public static ref T GetComponentRef<T>(this SKEntity entity) where T : struct, ISKComponent
    {
        var array = (ComponentArray<T>)_activeComponentArrays[typeof(T)];
        int idx = entity.ComponentIndices[GetComponentTypeId<T>()];
        return ref array.GetAt(idx);
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
    public static bool TryGetComponent<T>(this SKEntity entity, out T component) where T : struct, ISKComponent
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
    public static bool TryGetComponent(this SKEntity entity, Type componentType, out ISKComponent? component)
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
    public static ref List<object> GetAllComponents(this SKEntity entity)
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
}