using System;
using NHibernate.Extensions.DeepClone;
using NHibernate.Persister.Entity;

namespace NHibernate.Extensions.Internal
{
    internal class DelegateEntityResolver : IEntityResolver
    {
        private Func<System.Type, object, bool> _canResolve;
        private Func<object, AbstractEntityPersister, object> _resolver;

        public DelegateEntityResolver(Func<System.Type, object, bool> canResolve, Func<object, AbstractEntityPersister, object> resolver)
        {
            _canResolve = canResolve;
            _resolver = resolver;
        }

        public DelegateEntityResolver(Predicate<System.Type> canResolve, Func<object, AbstractEntityPersister, object> resolver)
        {
            _canResolve = (type, o) => canResolve(type);
            _resolver = resolver;
        }

        public object Resolve(object entity, AbstractEntityPersister entityPersister)
        {
            return _resolver(entity, entityPersister);
        }

        public bool CanResolve(System.Type entityType, object entity)
        {
            return _canResolve(entityType, entity);
        }
    }
}
