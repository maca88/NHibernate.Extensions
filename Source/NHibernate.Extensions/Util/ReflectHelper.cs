using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NHibernate.Extensions.Util
{
    internal static class ReflectHelper
    {
#if NH5
        public static MethodInfo GetMethodDefinition(Expression<System.Action> method)
        {
            return NHibernate.Util.ReflectHelper.GetMethodDefinition(method);
        }

        public static MethodInfo GetMethodDefinition<TSource>(Expression<Action<TSource>> method)
        {
            return NHibernate.Util.ReflectHelper.GetMethodDefinition(method);
        }
#elif NH4
        public static MethodInfo GetMethodDefinition(Expression<System.Action> method)
        {
            return NHibernate.Linq.ReflectionHelper.GetMethodDefinition(method);
        }
        public static MethodInfo GetMethodDefinition<TSource>(Expression<Action<TSource>> method)
        {
            return NHibernate.Linq.ReflectionHelper.GetMethodDefinition(method);
        }
#endif
    }
}
