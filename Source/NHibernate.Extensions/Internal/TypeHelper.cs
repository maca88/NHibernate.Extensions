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
        public static System.Type GetUnproxiedType(this object persistentObject)
        {
            switch (persistentObject)
            {
                case INHibernateProxy nhibernateProxy:
                    return nhibernateProxy.HibernateLazyInitializer.PersistentClass;
                case IFieldInterceptorAccessor fieldInterceptor:
                    return fieldInterceptor.FieldInterceptor == null
                        ? fieldInterceptor.GetType().BaseType
                        : fieldInterceptor.FieldInterceptor.MappedClass;
#pragma warning disable 618
                case IProxy proxy: // Deprecated it in NH 5.2
#pragma warning restore 618
                    switch (proxy.Interceptor)
                    {
                        case ILazyInitializer lazyInitializer:
                            return lazyInitializer.PersistentClass;
                        case IFieldInterceptorAccessor fieldInterceptorAccessor:
                            return fieldInterceptorAccessor.FieldInterceptor == null
                                ? fieldInterceptorAccessor.GetType().BaseType
                                : fieldInterceptorAccessor.FieldInterceptor.MappedClass;
                        default:
                            return persistentObject.GetType();
                    }
                default:
                    return persistentObject.GetType();
            }
        }
	}
}
