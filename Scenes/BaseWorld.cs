using SKSSL.Managers;
using SKSSL.Registry;

namespace SKSSL.Scenes;

public abstract class BaseWorld
{
    public readonly EntityManager _entityManager = new();
    
    public SKEntity SpawnEntity(string referenceId) => _entityManager.Spawn(referenceId, this);
}