using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NHibernate.Extensions
{
    public interface IBatchFetchBuilder<TEntity>
    {
        IBatchFetchBuilder<TEntity, TKey> SetKeys<TKey>(ICollection<TKey> keys, Expression<Func<TEntity, TKey>> keyExpresion);
    }

    public interface IBatchFetchBuilder<TEntity, TKey>
    {
        IBatchFetchBuilder<TEntity, TKey> BeforeQueryExecution(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryFunc);

        IBatchFetchBuilder<TEntity, TKey, T> Select<T>(Expression<Func<TEntity, T>> selectExpr);

        List<TEntity> Execute();


        Task<List<TEntity>> ExecuteAsync(CancellationToken cancellationToken = default);
    }

    public interface IBatchFetchBuilder<TEntity, TKey, TResult>
    {
        IBatchFetchBuilder<TEntity, TKey, TResult> BeforeQueryExecution(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryFunc);

        List<TResult> Execute();

        Task<List<TEntity>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
