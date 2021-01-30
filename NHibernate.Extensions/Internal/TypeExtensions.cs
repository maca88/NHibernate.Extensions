using System;
using System.Linq;

namespace NHibernate.Extensions.Internal
{
    internal static class TypeExtensions
    {
        public static System.Type GetGenericType(this System.Type givenType, System.Type genericType)
        {
            while (true)
            {
                if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                {
                    return givenType;
                }

                var type = givenType.GetInterfaces().FirstOrDefault(it => it.IsGenericType && it.GetGenericTypeDefinition() == genericType);
                if (type != null)
                {
                    return type;
                }

                var baseType = givenType.BaseType;
                if (baseType == null)
                {
                    return null;
                }
                givenType = baseType;
            }
        }

        public static bool IsAssignableToGenericType(this System.Type givenType, System.Type genericType)
        {
            var interfaceTypes = givenType.GetInterfaces();

            if (interfaceTypes.Any(it => it.IsGenericType && it.GetGenericTypeDefinition() == genericType))
            {
                return true;
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                return true;

            var baseType = givenType.BaseType;
            return baseType != null && IsAssignableToGenericType(baseType, genericType);
        }

        /// <summary>
        /// http://stackoverflow.com/questions/2490244/default-value-of-a-type-at-runtime
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static object GetDefaultValue(this System.Type t)
        {
            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }

        public static bool IsSimpleType(this System.Type type)
        {
            return
                type.IsPrimitive ||
                type.IsValueType ||
                type.IsEnum ||
                type == typeof(String);
        }
    }
}
