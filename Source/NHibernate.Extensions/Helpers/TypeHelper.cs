using NHibernate.Intercept;
using NHibernate.Proxy;
using NHibernate.Proxy.DynamicProxy;

namespace NHibernate.Extensions.Helpers
{
    public static class TypeHelper
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
        public static System.Type GetUnproxiedType(this object persistentObject)
        {
            var proxy = persistentObject as INHibernateProxy;
            if (proxy != null)
                return proxy.HibernateLazyInitializer.PersistentClass;

            var nhProxy = persistentObject as IProxy;
            if (nhProxy == null)
                return persistentObject.GetType();

            var lazyInitializer = nhProxy.Interceptor as ILazyInitializer;
            if (lazyInitializer != null)
                return lazyInitializer.PersistentClass;

            var fieldInterceptorAccessor = nhProxy.Interceptor as IFieldInterceptorAccessor;
            if (fieldInterceptorAccessor != null)
            {
                return fieldInterceptorAccessor.FieldInterceptor == null
                    ? nhProxy.GetType().BaseType
                    : fieldInterceptorAccessor.FieldInterceptor.MappedClass;
            }

            return persistentObject.GetType();
        }
    }
}
