using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NHibernate.Engine;
using NHibernate.Extensions.Internal;
using NHibernate.Extensions.Linq;
using Remotion.Linq;

namespace NHibernate.Linq
{
    public static class LinqExtensions
    {
        private static readonly PropertyInfo SessionPropertyInfo;
        private static readonly MethodInfo EnumerableToListMethod;

        static LinqExtensions()
        {
            SessionPropertyInfo = typeof (DefaultQueryProvider).GetProperty("Session",
                BindingFlags.NonPublic | BindingFlags.Instance);
            EnumerableToListMethod = NHibernate.Extensions.Util.ReflectHelper.GetMethodDefinition(() => Enumerable.ToList(new object[0]))
                .GetGenericMethodDefinition();
        }

        //public static IQueryable<T> Lock<T>(this IQueryable<T> query, LockMode lockMode, string alias = null)
        //{
        //    var method = ReflectHelper.GetMethodDefinition(() => Lock<object>(null, lockMode, alias)).MakeGenericMethod(typeof(T));
        //    var callExpression = Expression.Call(method, query.Expression, Expression.Constant(lockMode), Expression.Constant(alias));
        //    return new NhQueryable<T>(query.Provider, callExpression);
        //}

        public static IQueryable<T> Include<T>(this IQueryable<T> query, Expression<Func<T, object>> include)
        {
            var path = ExpressionHelper.GetFullPath(include.Body);
            Include(query, path);
            return query;
        }

        public static IIncludeQueryable<TChild, T> Include<T, TChild>(this IQueryable<T> query, Expression<Func<T, IEnumerable<TChild>>> include)
        {
            var path = ExpressionHelper.GetFullPath(include.Body);
            Include(query, path);
            return new IncludeRequest<TChild,T>(path, query.Provider, query.Expression);
        }

        public static IQueryable<T> Include<T>(this IQueryable<T> query, string path)
        {
            Include((IQueryable)query, path);
            return query;
        }

        public static IQueryable Include(this IQueryable query, string path)
        {
            var queryType = query.GetType();
            queryType = queryType.GetGenericType(typeof(QueryableBase<>));
            if (queryType == null)
                throw new InvalidOperationException("Include method is supported only for Nhibernate queries");

            var itemType = queryType.GetGenericArguments()[0];

            var providerField = typeof(QueryableBase<>).MakeGenericType(itemType)
                .GetField("_queryProvider", BindingFlags.NonPublic | BindingFlags.Instance);
            if(providerField == null)
                throw new NullReferenceException("providerField");
            var provider = providerField.GetValue(query) as IQueryProvider;
            if (!(provider is IncludeQueryProvider nhProvider))
            {
                var session = SessionPropertyInfo.GetValue(provider, null) as ISessionImplementor;
                nhProvider = new IncludeQueryProvider(itemType, session);
                providerField.SetValue(query, nhProvider);
            }
            nhProvider.Include(path);
            return query;
        }

        private static IEnumerable ToList(this IQueryable query)
        {
            var type = query.GetType().GetGenericArguments()[0];
            
            var methodInfo = EnumerableToListMethod.MakeGenericMethod(type);
            return (IEnumerable)methodInfo.Invoke(null, new object[] {query});
        }

        public static List<T> ToList<T>(this IQueryable query)
        {
            return ToList(query).Cast<T>().ToList();
        }

    }

}
