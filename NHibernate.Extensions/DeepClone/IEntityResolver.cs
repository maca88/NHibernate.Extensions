using NHibernate.Persister.Entity;

namespace NHibernate.Extensions.DeepClone
{
    public interface IEntityResolver
    {
        object Resolve(object entity, AbstractEntityPersister entityPersister);

        bool CanResolve(System.Type entityType, object entity);
    }
}
