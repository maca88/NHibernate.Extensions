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

        //public static IQueryable<T> Lock<T>(this IQueryable<T> query, LockMode lockMode, string alias = null)
        //{
        //    var method = ReflectHelper.GetMethodDefinition(() => Lock<object>(null, lockMode, alias)).MakeGenericMethod(typeof(T));
        //    var callExpression = Expression.Call(method, query.Expression, Expression.Constant(lockMode), Expression.Constant(alias));
        //    return new NhQueryable<T>(query.Provider, callExpression);
        //}

        public static IQueryable<T> Include<T>(this IQueryable<T> query, Expression<Func<T, object>> include)
        {
            var path = ExpressionHelper.GetFullPath(include.Body);
            return Include(query, path);
        }

        public static IIncludeQueryable<TChild, T> Include<T, TChild>(this IQueryable<T> query, Expression<Func<T, IEnumerable<TChild>>> include)
        {
            var path = ExpressionHelper.GetFullPath(include.Body);
            return new IncludeRequest<TChild,T>(path, IncludeInternal(query, path), query.Expression);
        }

        public static IQueryable<T> Include<T>(this IQueryable<T> query, string path)
        {
            return new NhQueryable<T>(IncludeInternal(query, path), query.Expression);
        }

        public static IQueryable Include(this IQueryable query, string path)
        {
            var queryType = query.GetType();
            queryType = queryType.GetGenericType(typeof(QueryableBase<>));
            if (queryType == null)
            {
                throw new InvalidOperationException("Include method is supported only for NHibernate queries");
            }

            return (IQueryable) IncludeMethod.MakeGenericMethod(queryType.GetGenericArguments()[0])
                .Invoke(null, new object[] {query, path});
        }

        internal static IncludeQueryProvider IncludeInternal<T>(this IQueryable<T> query, string path)
        {
            if (!(query.Provider is INhQueryProvider))
            {
                throw new InvalidOperationException("Include method is supported only for NHibernate queries");
            }

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
                throw new InvalidOperationException($"Query provider {query.Provider} is not supported.");
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
