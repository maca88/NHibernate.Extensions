using NHibernate.Persister.Entity;
using NHibernate.Type;

namespace NHibernate.Extensions
{
    public class DeepCloneParentEntity
    {
        public object Entity { get; set; }

        public AbstractEntityPersister EntityPersister { get; set; }

        public IType ChildType { get; set; }

        public string[] ReferencedColumns { get; set; }
    }
}
