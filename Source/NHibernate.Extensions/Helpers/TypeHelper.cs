using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NHibernate.Extensions.Helpers
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
	}
}
