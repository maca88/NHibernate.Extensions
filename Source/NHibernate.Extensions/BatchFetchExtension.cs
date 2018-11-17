using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NHibernate.Linq;

namespace NHibernate.Extensions
{
    public static class BatchFetchExtension
    {
        internal static readonly MethodInfo ContainsMethodInfo;

        static BatchFetchExtension()
        {
            ContainsMethodInfo = typeof(Enumerable)
                .GetMethods()
                .Where(x => x.Name == "Contains")
                .Single(x => x.GetParameters().Length == 2);
        }

        /// <summary>
        /// Batch fetching a collecion of keys by using ISession Linq provider
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="session">NHibernate session</param>
        /// <param name="keys">Collection of keys that will be retrieved from the database</param>
        /// <param name="propertyExpr">Expression pointing to the property that represents the key</param>
        /// <param name="batchSize">Number of records that will be retrieved within one execution</param>
        /// <param name="queryFunc">Function to modify the query prior execution</param>
        /// <returns>The fetched entites.</returns>
        public static List<TEntity> BatchFetch<TEntity, TProperty>(this ISession session, ICollection<TProperty> keys,
            Expression<Func<TEntity, TProperty>> propertyExpr,
            int batchSize,
            Func<IQueryable<TEntity>, IQueryable<TEntity>> queryFunc = null)
        {
            if (propertyExpr == null)
            {
                throw new ArgumentNullException(nameof(propertyExpr));
            }

            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            return session.BatchFetch<TEntity>(batchSize)
                .SetKeys(keys, propertyExpr)
                .BeforeQueryExecution(queryFunc)
                .Execute();
        }

        /// <summary>
        /// Batch fetching a collecion of keys by using ISession Linq provider
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="session">NHibernate session</param>
        /// <param name="batchSize">Number of records that will be retrieved within one execution</param>
        /// <returns>The batch fetch builder.</returns>
        public static IBatchFetchBuilder<TEntity> BatchFetch<TEntity>(this ISession session, int batchSize)
        {
            return new BatchFetchBuilder<TEntity>(session, batchSize);
        }
    }
}
