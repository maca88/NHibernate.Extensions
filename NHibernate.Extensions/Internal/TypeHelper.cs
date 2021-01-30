using NHibernate.Intercept;
using NHibernate.Proxy;
using NHibernate.Proxy.DynamicProxy;

namespace NHibernate.Extensions.Internal
{
    internal static class TypeHelper
    {
        public static bool IsSubclassOfRawGeneric(System.Type generic, System.Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        /// <summary>
        /// Gets the underlying class type of a persistent object that may be proxied
        /// </summary>
        public static System.Type GetUnproxiedType(this object entity, bool allowInitialization)
        {
            if (entity is INHibernateProxy nhProxy)
            {
                if (nhProxy.HibernateLazyInitializer.IsUninitialized && !allowInitialization)
                {
                    return nhProxy.HibernateLazyInitializer.PersistentClass;
                }

                // We have to initialize in case of a subclass to get the concrete type
                entity = nhProxy.HibernateLazyInitializer.GetImplementation();
            }

            switch (entity)
            {
                case IFieldInterceptorAccessor interceptorAccessor:
                    return interceptorAccessor.FieldInterceptor.MappedClass;
                default:
                    return entity.GetType();
            }
        }
	}
}
