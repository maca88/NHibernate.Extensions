using NHibernate.Persister.Entity;
using NHibernate.Type;

namespace NHibernate.Extensions
{
    public class DeepCloneParentEntity
    {
        public DeepCloneParentEntity(
            object entity,
            AbstractEntityPersister entityPersister,
            IType childType,
            string[] referencedColumns)
        {
            Entity = entity;
            EntityPersister = entityPersister;
            ChildType = childType;
            ReferencedColumns = referencedColumns;
        }

        public object Entity { get; }

        public AbstractEntityPersister EntityPersister { get; }

        public IType ChildType { get; }

        public string[] ReferencedColumns { get; }
    }
}
