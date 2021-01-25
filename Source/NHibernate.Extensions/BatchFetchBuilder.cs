using NHibernate.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NHibernate.Extensions
{
    public class BatchFetchBuilder<TEntity> : IBatchFetchBuilder<TEntity>
    {
        private readonly ISession _session;
        private readonly int _batchSize;

        public BatchFetchBuilder(ISession session, int batchSize)
        {
            _session = session;
            _batchSize = batchSize;
        }

        IBatchFetchBuilder<TEntity, TKey> IBatchFetchBuilder<TEntity>.SetKeys<TKey>(ICollection<TKey> keys, Expression<Func<TEntity, TKey>> keyExpresion)
        {
            return new BatchFetchBuilder<TEntity, TKey>(_session, keys, keyExpresion, _batchSize);
        }
    }

    public class BatchFetchBuilder<TEntity, TKey> : IBatchFetchBuilder<TEntity, TKey>
    {
        protected readonly ISession Session;
        protected readonly ICollection<TKey> Keys;
        protected readonly Expression<Func<TEntity, TKey>> KeyExpresion;
        protected readonly int BatchSize;

        public BatchFetchBuilder(ISession session, ICollection<TKey> keys, Expression<Func<TEntity, TKey>> keyExpresion,
            int batchSize)
        {
            Session = session;
            Keys = keys;
            KeyExpresion = keyExpresion;
            BatchSize = batchSize;
        }

        public Func<IQueryable<TEntity>, IQueryable<TEntity>> BeforeQueryExecutionFunction { get; protected set; }

        IBatchFetchBuilder<TEntity, TKey> IBatchFetchBuilder<TEntity, TKey>.BeforeQueryExecution(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryFunc)
        {
            return new BatchFetchBuilder<TEntity, TKey>(Session, Keys.ToList(), KeyExpresion, BatchSize)
            {
                BeforeQueryExecutionFunction = queryFunc
            };
        }

        IBatchFetchBuilder<TEntity, TKey, T> IBatchFetchBuilder<TEntity, TKey>.Select<T>(Expression<Func<TEntity, T>> selectExpr)
        {
            return new BatchFetchBuilder<TEntity, TKey, T>(Session, Keys.ToList(), KeyExpresion, BatchSize, selectExpr)
            {
                BeforeQueryExecutionFunction = BeforeQueryExecutionFunction
            };
        }

        List<TEntity> IBatchFetchBuilder<TEntity, TKey>.Execute()
        {
            return Execute(q => q);
        }

        public virtual Task<List<TEntity>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(q => q, cancellationToken);
        }

        protected List<T> Execute<T>(Func<IQueryable<TEntity>, IQueryable<T>> convertQuery)
        {
            var parameter = KeyExpresion.Parameters[0];
            var method = BatchFetchExtension.ContainsMethodInfo.MakeGenericMethod(typeof(TKey));
            var result = new List<T>();
            var currIndex = 0;
            var itemsCount = Keys.Count;
            while (currIndex < itemsCount)
            {
                var batchNum = Math.Min(BatchSize, itemsCount - currIndex);
                var batchItems = Keys.Skip(currIndex).Take(batchNum).ToList();
                var value = Expression.Constant(batchItems, typeof(IEnumerable<TKey>));
                var containsMethod = Expression.Call(method, value, KeyExpresion.Body);
                var predicate = Expression.Lambda<Func<TEntity, bool>>(containsMethod, parameter);
                var query = Session.Query<TEntity>()
                    .Where(predicate);

                if (BeforeQueryExecutionFunction != null)
                {
                    query = BeforeQueryExecutionFunction(query);
                }

                result.AddRange(convertQuery(query).ToArray());
                currIndex += batchNum;
            }

            return result;
        }



        protected async Task<List<T>> ExecuteAsync<T>(Func<IQueryable<TEntity>, IQueryable<T>> convertQuery, CancellationToken cancellationToken = default)
        {
            var parameter = KeyExpresion.Parameters[0];
            var method = BatchFetchExtension.ContainsMethodInfo.MakeGenericMethod(typeof(TKey));
            var result = new List<T>();
            var currIndex = 0;
            var itemsCount = Keys.Count;
            while (currIndex < itemsCount)
            {
                var batchNum = Math.Min(BatchSize, itemsCount - currIndex);
                var batchItems = Keys.Skip(currIndex).Take(batchNum).ToList();
                var value = Expression.Constant(batchItems, typeof(IEnumerable<TKey>));
                var containsMethod = Expression.Call(method, value, KeyExpresion.Body);
                var predicate = Expression.Lambda<Func<TEntity, bool>>(containsMethod, parameter);
                var query = Session.Query<TEntity>()
                    .Where(predicate);

                if (BeforeQueryExecutionFunction != null)
                {
                    query = BeforeQueryExecutionFunction(query);
                }

                var results = await convertQuery(query).ToListAsync(cancellationToken).ConfigureAwait(false);
                result.AddRange(results);
                currIndex += batchNum;
            }

            return result;
        }
    }


    public class BatchFetchBuilder<TEntity, TKey, TResult> : BatchFetchBuilder<TEntity, TKey>, IBatchFetchBuilder<TEntity, TKey, TResult>
    {
        public BatchFetchBuilder(ISession session, ICollection<TKey> keys, Expression<Func<TEntity, TKey>> keyExpresion, int batchSize,
            Expression<Func<TEntity, TResult>> selectExpression)
            : base(session, keys, keyExpresion, batchSize)
        {
            SelectExpression = selectExpression;
        }

        public Expression<Func<TEntity, TResult>> SelectExpression { get; }



        IBatchFetchBuilder<TEntity, TKey, TResult> IBatchFetchBuilder<TEntity, TKey, TResult>.BeforeQueryExecution(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryFunc)
        {
            return new BatchFetchBuilder<TEntity, TKey, TResult>(Session, Keys.ToList(), KeyExpresion, BatchSize, SelectExpression)
            {
                BeforeQueryExecutionFunction = queryFunc
            };
        }

        List<TResult> IBatchFetchBuilder<TEntity, TKey, TResult>.Execute()
        {
            return Execute(q => q.Select(SelectExpression));
        }

        public new Task<List<TResult>> ExecuteAsync<T>(Func<IQueryable<TEntity>, IQueryable<T>> convertQuery, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(convertQuery, cancellationToken);
        }


    }
}
