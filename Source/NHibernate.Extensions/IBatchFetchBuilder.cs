using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace NHibernate.Extensions
{
    public interface IBatchFetchBuilder<TEntity>
    {
        IBatchFetchBuilder<TEntity, TKey> SetKeys<TKey>(ICollection<TKey> keys, Expression<Func<TEntity, TKey>> keyExpresion);
    }

    public partial interface IBatchFetchBuilder<TEntity, TKey>
    {
        IBatchFetchBuilder<TEntity, TKey> BeforeQueryExecution(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryFunc);

        IBatchFetchBuilder<TEntity, TKey, T> Select<T>(Expression<Func<TEntity, T>> selectExpr);

        List<TEntity> Execute();
    }

    public partial interface IBatchFetchBuilder<TEntity, TKey, TResult>
    {
        IBatchFetchBuilder<TEntity, TKey, TResult> BeforeQueryExecution(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryFunc);

        List<TResult> Execute();
    }
}
