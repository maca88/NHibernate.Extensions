using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Engine;
using NHibernate.Extensions.Internal;
using NHibernate.Linq;
using NHibernate.Persister.Collection;
using NHibernate.Type;
using NHibernate.Util;

namespace NHibernate.Extensions.Linq
{
    public class IncludeQueryProvider : INhQueryProvider
    {
        protected static readonly MethodInfo CreateQueryMethod;
        protected static readonly MethodInfo ExecuteInternalMethod;
        protected static readonly MethodInfo ExecuteInternalAsyncMethod;

        protected static readonly MethodInfo WhereMethod;
        protected static readonly MethodInfo ContainsMethod;
        
        protected static readonly MethodInfo FetchMethod;
        protected static readonly MethodInfo FetchManyMethod;
        protected static readonly MethodInfo ThenFetchMethod;
        protected static readonly MethodInfo ThenFetchManyMethod;
 
        private static readonly Func<DefaultQueryProvider, ISessionImplementor> SessionProvider;

        static IncludeQueryProvider()
        {
            CreateQueryMethod =
                ReflectHelper.GetMethodDefinition((INhQueryProvider p) => p.CreateQuery<object>(null));
            ExecuteInternalMethod =
                ReflectHelper.GetMethodDefinition((IncludeQueryProvider p) => p.ExecuteInternal<object>(null, null));
            ExecuteInternalAsyncMethod =
                ReflectHelper.GetMethodDefinition((IncludeQueryProvider p) => p.ExecuteInternalAsync<object>(null, null, default));

            FetchMethod =
                ReflectHelper.GetMethodDefinition(() => EagerFetchingExtensionMethods.Fetch<object, object>(null, null));
            FetchManyMethod =
                ReflectHelper.GetMethodDefinition(() => EagerFetchingExtensionMethods.FetchMany<object, object>(null, null));
            ThenFetchMethod =
                ReflectHelper.GetMethodDefinition(() => EagerFetchingExtensionMethods.ThenFetch<object, object, object>(null, null));
            ThenFetchManyMethod =
                ReflectHelper.GetMethodDefinition(() => EagerFetchingExtensionMethods.ThenFetchMany<object, object, object>(null, null));

            WhereMethod = ReflectHelper.GetMethodDefinition(() => Queryable.Where<object>(null, o => true));
            ContainsMethod = ReflectHelper.GetMethodDefinition(() => Queryable.Contains<object>(null, null));

            var param = Expression.Parameter(typeof(DefaultQueryProvider));
            var sessionProp = typeof(DefaultQueryProvider).GetProperty("Session",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (sessionProp == null)
            {
                throw new InvalidOperationException($"{typeof(DefaultQueryProvider)} does not have a Session property.");
            }
            SessionProvider = Expression.Lambda<Func<DefaultQueryProvider, ISessionImplementor>>(
                Expression.Property(param, sessionProp), param).Compile();
        }

        private static T GetValue<T>(IEnumerable<T> items, string methodName)
        {
            switch (methodName)
            {
                case "First":
                    return items.First();
                case "FirstOrDefault":
                    return items.FirstOrDefault();
                case "Single":
                    return items.Single();
                case "SingleOrDefault":
                    return items.SingleOrDefault();
                case "Last":
                    return items.Last();
                case "LastOrDefault":
                    return items.LastOrDefault();
                default:
                    throw new InvalidOperationException($"Unknown method: {methodName}");
            }
        }

        protected static LambdaExpression CreatePropertyExpression(System.Type type, string propName,
            System.Type convertToType = null)
        {
            var p = Expression.Parameter(type);
            var body = Expression.Property(p, propName);
            if (convertToType == null) return Expression.Lambda(body, p);
            var converted = Expression.Convert(body, convertToType);
            return Expression.Lambda(converted, p);
        }

        protected System.Type Type;
        private readonly DefaultQueryProvider _queryProvider;
        public readonly List<string> IncludePaths = new List<string>();

        public IncludeQueryProvider(System.Type type, DefaultQueryProvider queryProvider)
        {
            Type = type;
            _queryProvider = queryProvider;
        }

        public IncludeQueryProvider(System.Type type, IncludeQueryProvider includeQueryProvider) : this(type, includeQueryProvider._queryProvider, includeQueryProvider.IncludePaths)
        {
        }

        private IncludeQueryProvider(System.Type type, DefaultQueryProvider queryProvider, IEnumerable<string> includePaths) : this(type, queryProvider)
        {
            IncludePaths.AddRange(includePaths);
        }

        public IncludeQueryProvider Include(string path)
        {
            IncludePaths.Add(path);
            return this;
        }

        public ISessionImplementor Session => SessionProvider(_queryProvider);

        public IQueryable<T> CreateQuery<T>(Expression expression)
        {
            var newQuery = new NhQueryable<T>(this, expression);
            if (!typeof (T).IsAssignableFrom(Type)) //Select and other methods that returns other types
                throw new NotSupportedException("IncludeQueryProvider does not support mixing types");
            Type = typeof (T); //Possbile typecast to a base type
            return newQuery;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            var m = CreateQueryMethod.MakeGenericMethod(expression.Type.GetGenericArguments()[0]);
            return (IQueryable)m.Invoke(this, new object[] { expression });
        }

        public async Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var resultVisitor = new IncludeRewriterVisitor();
            expression = resultVisitor.Modify(expression);

            if (resultVisitor.Count)
            {
                return await _queryProvider.ExecuteAsync<TResult>(expression, cancellationToken);
            }

            if (resultVisitor.SkipTake)
            {
                expression = RemoveSkipAndTake(expression);
            }

            try
            {
                dynamic task = ExecuteInternalAsyncMethod.MakeGenericMethod(Type)
                    .Invoke(this, new object[] {expression, resultVisitor, cancellationToken});
                return await task.ConfigureAwait(false);
            }
            catch (TargetInvocationException e)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                throw;
            }
        }

        public object Execute(Expression expression)
        {
            var resultVisitor = new IncludeRewriterVisitor();
            expression = resultVisitor.Modify(expression);
            if (resultVisitor.Count)
            {
                return _queryProvider.Execute(expression);
            }

            if (resultVisitor.SkipTake)
            {
                expression = RemoveSkipAndTake(expression);
            }

            try
            {
                return ExecuteInternalMethod.MakeGenericMethod(Type).Invoke(this, new object[] {expression, resultVisitor});
            }
            catch (TargetInvocationException e)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                throw;
            }
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return (TResult)Execute(expression);
        }

        public IFutureEnumerable<TResult> ExecuteFuture<TResult>(Expression expression)
        {
            var resultVisitor = new IncludeRewriterVisitor();
            expression = resultVisitor.Modify(expression);
            if (resultVisitor.SkipTake)
            {
                expression = RemoveSkipAndTake(expression);
            }

            return ExecuteQueryTreeFuture<TResult>(expression);
        }

        public IFutureValue<TResult> ExecuteFutureValue<TResult>(Expression expression)
        {
            var resultVisitor = new IncludeRewriterVisitor();
            expression = resultVisitor.Modify(expression);
            if (resultVisitor.SkipTake)
            {
                expression = RemoveSkipAndTake(expression);
            }

            return ExecuteQueryTreeFutureValue<TResult>(expression);
        }

        //public IQueryProvider WithOptions(Action<NhQueryableOptions> setOptions)
        //{
        //    if (_queryProvider is IQueryProviderWithOptions queryProviderWithOptions)
        //    {
        //        return new IncludeQueryProvider(Type, (INhQueryProvider) queryProviderWithOptions.WithOptions(setOptions), IncludePaths);
        //    }
        //    throw new InvalidOperationException($"The underlying query provider {_queryProvider.GetType()} does not support WithOptions method.");
        //}

        public Task<int> ExecuteDmlAsync<T>(QueryMode queryMode, Expression expression, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Dml is not supported when using Include.");
        }

        public int ExecuteDml<T>(QueryMode queryMode, Expression expression)
        {
            throw new NotSupportedException("Dml is not supported when using Include.");
        }

        public void SetResultTransformerAndAdditionalCriteria(IQuery query, NhLinqExpression nhExpression, IDictionary<string, Tuple<object, IType>> parameters)
        {
            _queryProvider.SetResultTransformerAndAdditionalCriteria(query, nhExpression, parameters);
        }

        internal object ExecuteInternal<T>(Expression expression, IncludeRewriterVisitor visitor)
        {
            var future = ExecuteQueryTreeFuture<T>(expression);
            var items = future.GetEnumerable();
            if (visitor.SingleResult)
            {
                return GetValue(items, visitor.SingleResultMethodName);
            }
            return items;
        }

        internal async Task<object> ExecuteInternalAsync<T>(Expression expression, IncludeRewriterVisitor visitor, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var future = ExecuteQueryTreeFuture<T>(expression);
            var items = await future.GetEnumerableAsync(cancellationToken).ConfigureAwait(false);
            if (visitor.SingleResult)
            {
                return GetValue(items, visitor.SingleResultMethodName);
            }
            return items;
        }

        private IFutureEnumerable<T> ExecuteQueryTreeFuture<T>(Expression queryExpression)
        {
            var tree = new QueryRelationTree();
            IFutureEnumerable<T> result = null;
            foreach (var path in IncludePaths)
            {
                tree.AddNode(path);
            }

            var leafs = tree.GetLeafs();
            leafs.Sort();
            var expressions = leafs.Aggregate(new ExpressionInfo(queryExpression), FetchFromPath).GetExpressions();
            var i = 0;
            foreach (var expression in expressions)
            {
                if (i == 0)
                    result = _queryProvider.ExecuteFuture<T>(expression);
                else
                    _queryProvider.ExecuteFuture<T>(expression);
                i++;
            }
            return result;
        }

        private IFutureValue<T> ExecuteQueryTreeFutureValue<T>(Expression queryExpression)
        {
            var tree = new QueryRelationTree();
            IFutureValue<T> result = null;
            foreach (var path in IncludePaths)
            {
                tree.AddNode(path);
            }

            var leafs = tree.GetLeafs();
            leafs.Sort();
            var expressions = leafs.Aggregate(new ExpressionInfo(queryExpression), FetchFromPath).GetExpressions();
            var i = 0;
            foreach (var expression in expressions)
            {
                if (i == 0)
                    result = _queryProvider.ExecuteFutureValue<T>(expression);
                else
                    _queryProvider.ExecuteFuture<T>(expression);
                i++;
            }
            return result;
        }

        private Expression RemoveSkipAndTake(Expression queryExpression)
        {
            var query = _queryProvider.CreateQuery(queryExpression);
            var pe = Expression.Parameter(Type);
            var contains = Expression.Call(null,
                ContainsMethod.MakeGenericMethod(Type),
                new Expression[]
                {
                    Expression.Constant(query),
                    pe
                });
            return Expression.Call(null,
                WhereMethod.MakeGenericMethod(Type),
                new[]
                {
                    new SkipTakeVisitor().RemoveSkipAndTake(queryExpression),
                    Expression.Lambda(contains, pe)
                });
        }

        private ExpressionInfo FetchFromPath(ExpressionInfo expressionInfo, string path)
        {
            return FetchFromPath(expressionInfo, path, true);
        }

        private ExpressionInfo FetchFromPath(ExpressionInfo expressionInfo, string path, bool root)
        {
            var expression = expressionInfo.Expression;
            var currentType = Type;
            var metas = Session.Factory.GetAllClassMetadata()
                .Select(o => o.Value)
                .Where(o => Type.IsAssignableFrom(o.MappedClass))
                .ToList();
            if (!metas.Any())
                throw new HibernateException($"Metadata for type '{Type}' was not found");
            if (metas.Count > 1)
                throw new HibernateException($"There are more than one metadata for type '{Type}'");

            var meta = metas.First();
            var paths = path.Split('.');
            var index = 0;
            foreach (var propName in paths)
            {
                if (meta == null)
                {
                    throw new InvalidOperationException($"Unable to fetch property {propName} from path '{path}', no class metadata found");
                }
                var propType = meta.GetPropertyType(propName);
                if (propType == null)
                    throw new Exception($"Property '{propName}' does not exist in the type '{meta.MappedClass.FullName}'");

                if (!(propType is IAssociationType assocType))
                {
                    throw new Exception($"Property '{propName}' does not implement IAssociationType interface");
                }

                if (assocType.IsCollectionType)
                {
                    var collectionType = assocType as CollectionType;
                    var collectionPersister = (IQueryableCollection)Session.Factory.GetCollectionPersister(collectionType.Role);
                    meta = collectionPersister.ElementType.IsEntityType
                        ? Session.Factory.GetClassMetadata(collectionPersister.ElementPersister.EntityName)
                        : null;

                    var collPath = string.Join(".", paths.Take(index + 1));

                    //Check if we can fetch the collection without create a cartesian product
                    //Fetch can occur only for nested collection
                    if (!string.IsNullOrEmpty(expressionInfo.CollectionPath) &&
                        !collPath.StartsWith(expressionInfo.CollectionPath + "."))
                    {
                        //We have to continue fetching within a new base query
                        var nextQueryInfo = expressionInfo.GetOrCreateNext();
                        return FetchFromPath(nextQueryInfo, path, true);
                    }

                    expressionInfo.CollectionPath = collPath;
                }
                else
                {
                    meta = Session.Factory.GetClassMetadata(assocType.GetAssociatedEntityName(Session.Factory));
                }

                MethodInfo fetchMethod;

                //Try to get the actual property type (so we can skip casting as relinq will throw an exception)
                var relatedProp = currentType.GetProperty(propName);

                var relatedType = relatedProp != null ? relatedProp.PropertyType : meta?.MappedClass;
                if (propType.IsCollectionType && relatedProp != null && relatedType.IsGenericType)
                {
                    relatedType = propType.GetType().IsAssignableToGenericType(typeof(GenericMapType<,>))
                        ? typeof(KeyValuePair<,>).MakeGenericType(relatedType.GetGenericArguments())
                        : relatedType.GetGenericArguments()[0];
                }

                var convertToType = propType.IsCollectionType
                    ? typeof (IEnumerable<>).MakeGenericType(relatedType)
                    : null;

                var propertyExpression = CreatePropertyExpression(currentType, propName, convertToType);
                //No fetch before
                if (root)
                {
                    fetchMethod = propType.IsCollectionType
                        ? FetchManyMethod.MakeGenericMethod(Type, relatedType)
                        : FetchMethod.MakeGenericMethod(Type, relatedType);
                }
                else
                {
                    fetchMethod = propType.IsCollectionType
                        ? ThenFetchManyMethod.MakeGenericMethod(Type, currentType, relatedType)
                        : ThenFetchMethod.MakeGenericMethod(Type, currentType, relatedType);
                }

                
                expression = Expression.Call(fetchMethod, expression, propertyExpression);
                currentType = relatedType;
                index++;
                root = false;
            }
            expressionInfo.Expression = expression;
            return expressionInfo;
        }

    }

}
