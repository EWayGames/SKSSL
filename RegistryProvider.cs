namespace SKSSL;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class RegistryProvider<T> : IServiceProvider where T : class, new()
{
    /// <summary>
    /// 
    /// </summary>
    public T? Registry { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public void Replace(T replacement) => Registry = replacement;

    /// <inheritdoc />
    public object? GetService(Type serviceType)
    {
        return serviceType == typeof(T) ? Registry : null;
    }
}