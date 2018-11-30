using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NHibernate.Engine;
using NHibernate.Extensions.Internal;
using NHibernate.Extensions.Linq;
using NHibernate.Util;
using Remotion.Linq;

namespace NHibernate.Linq
{
    public static class LinqExtensions
    {
        private static readonly MethodInfo EnumerableToListMethod;
        private static readonly MethodInfo IncludeMethod;

        static LinqExtensions()
        {
            EnumerableToListMethod = ReflectHelper.GetMethodDefinition(() => Enumerable.ToList(new object[0]))
                .GetGenericMethodDefinition();
            IncludeMethod = ReflectHelper.GetMethodDefinition(() => Include(default(IQueryable<object>), ""))
                .GetGenericMethodDefinition();
        }

        public static IIncludeQueryable<T> Include<T>(this IQueryable<T> query, Expression<Func<T, object>> include)
        {
            var path = ExpressionHelper.GetFullPath(include.Body);
            return Include(query, path);
        }

        public static IIncludeQueryable<TChild, T> Include<T, TChild>(this IQueryable<T> query, Expression<Func<T, IEnumerable<TChild>>> include)
        {
            var path = ExpressionHelper.GetFullPath(include.Body);
            return new IncludeQueryable<TChild,T>(path, IncludeInternal(query, path), query.Expression);
        }

        public static IIncludeQueryable<T> Include<T>(this IQueryable<T> query, string path)
        {
            return new IncludeQueryable<T>(IncludeInternal(query, path), query.Expression);
        }

        public static IIncludeQueryable Include(this IQueryable query, string path)
        {
            var queryType = query.GetType();
            queryType = queryType.GetGenericType(typeof(IQueryable<>));
            if (queryType == null)
            {
                throw new InvalidOperationException($"Query of type {query.GetType()} cannot be converted to {typeof(IQueryable<>)}.");
            }

            return (IIncludeQueryable) IncludeMethod.MakeGenericMethod(queryType.GetGenericArguments()[0])
                .Invoke(null, new object[] {query, path});
        }

        internal static IQueryProvider IncludeInternal<T>(this IQueryable<T> query, string path)
        {
            if (query.Provider is IncludeQueryProvider includeQueryProvider)
            {
                includeQueryProvider = new IncludeQueryProvider(typeof(T), includeQueryProvider);
            }
            else if (query.Provider is DefaultQueryProvider defaultQueryProvider)
            {
                includeQueryProvider = new IncludeQueryProvider(typeof(T), defaultQueryProvider);
            }
            else
            {
                return query.Provider;
            }

            includeQueryProvider.Include(path);
            return includeQueryProvider;
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
