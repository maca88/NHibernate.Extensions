using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NHibernate.Linq;

namespace NHibernate.Extensions
{
    public static class BatchFetchExtension
    {
        private static readonly MethodInfo ContainsMethodInfo;

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
        /// <returns></returns>
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
            var parameter = propertyExpr.Parameters[0];
            var method = ContainsMethodInfo.MakeGenericMethod(typeof(TProperty));
            var result = new List<TEntity>();
            var currIndex = 0;
            var itemsCount = keys.Count;
            while (currIndex < itemsCount)
            {
                var batchNum = Math.Min(batchSize, itemsCount - currIndex);
                var batchItems = keys.Skip(currIndex).Take(batchNum).ToList();
                var value = Expression.Constant(batchItems, typeof(IEnumerable<TProperty>));
                var containsMethod = Expression.Call(method, value, propertyExpr.Body);
                var predicate = Expression.Lambda<Func<TEntity, bool>>(containsMethod, parameter);
                var query = session.Query<TEntity>()
                    .Where(predicate);
                if (queryFunc != null)
                {
                    query = queryFunc(query);
                }
                result.AddRange(query.ToArray());
                currIndex += batchNum;
            }
            return result;
        }
    }
}
