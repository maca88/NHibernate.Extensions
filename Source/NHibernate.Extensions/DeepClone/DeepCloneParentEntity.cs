using NHibernate.Persister.Entity;
using NHibernate.Type;

namespace NHibernate.Extensions
{
    public class DeepCloneParentEntity
    {
        public DeepCloneParentEntity(
            object entity,
            IType childType,
            string[] referencedColumns)
        {
            Entity = entity;
            ChildType = childType;
            ReferencedColumns = referencedColumns;
        }

        public object Entity { get; }

        public IType ChildType { get; }

        public string[] ReferencedColumns { get; }
    }
}
