namespace SKSSL.ECS;

public static class EntityExtensions
{
    private static EntityContext CurrentContext
        => ECSController.EntityContext ??
           throw new InvalidOperationException("No active ECS context! Initialize ECS first!");

    public static ref T GetComponent<T>(this SKEntity entity) where T : struct, ISKComponent =>
        ref CurrentContext.Components.GetComponent<T>(entity);

    public static object AddComponent(this SKEntity entity, Type type) =>
        CurrentContext.Components.AddComponent(entity, type);

    public static T AddComponent<T>(this SKEntity entity) where T : struct, ISKComponent =>
        (T)AddComponent(entity, typeof(T));
    
    public static List<object> GetAllComponents(this SKEntity entity) => CurrentContext.Components.GetAllComponents(entity);

    public static bool HasComponent<T>(this SKEntity entity) where T : struct, ISKComponent
        => HasComponent(entity, typeof(T));

    public static bool HasComponent(this SKEntity entity, Type componentType)
        => entity.ComponentIndices[CurrentContext.Components.GetComponentTypeId(componentType)] != -1;

}