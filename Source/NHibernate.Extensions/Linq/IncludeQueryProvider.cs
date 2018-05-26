using System;
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
using NHibernate.Impl;
using NHibernate.Linq;
using NHibernate.Persister.Collection;
using NHibernate.Type;
using ReflectHelper = NHibernate.Extensions.Util.ReflectHelper;
using TypeHelper = NHibernate.Extensions.Internal.TypeHelper;

namespace NHibernate.Extensions.Linq
{
    public class IncludeQueryProvider : DefaultQueryProvider
    {
        protected static readonly MethodInfo CreateQueryMethodDefinition;
        protected static readonly MethodInfo FetchMethod;
        protected static readonly MethodInfo FetchManyMethod;
        protected static readonly MethodInfo ThenFetchMethod;
        protected static readonly MethodInfo ThenFetchManyMethod;
        protected static readonly MethodInfo WhereMethod;
        protected static readonly MethodInfo ContainsMethod;
        protected static readonly MethodInfo ToFutureMethod;
        protected static readonly MethodInfo ToFutureValueMethod;

        static IncludeQueryProvider()
        {
            CreateQueryMethodDefinition =
                ReflectHelper.GetMethodDefinition((IncludeQueryProvider p) => p.CreateQuery<object>(null));
            FetchMethod =
                ReflectHelper.GetMethodDefinition(() => EagerFetchingExtensionMethods.Fetch<object, object>(null, null));
            FetchManyMethod =
                ReflectHelper.GetMethodDefinition(() => EagerFetchingExtensionMethods.FetchMany<object, object>(null, null));

            ThenFetchMethod =
                ReflectHelper.GetMethodDefinition(() => EagerFetchingExtensionMethods.ThenFetch<object, object, object>(null, null));
            ThenFetchManyMethod =
                ReflectHelper.GetMethodDefinition(() => EagerFetchingExtensionMethods.ThenFetchMany<object, object, object>(null, null));

            ToFutureMethod =
                ReflectHelper.GetMethodDefinition(() => LinqExtensionMethods.ToFuture<object>(null));
            ToFutureValueMethod =
                ReflectHelper.GetMethodDefinition(() => LinqExtensionMethods.ToFutureValue<object>(null));

            WhereMethod = ReflectHelper.GetMethodDefinition(() => Queryable.Where<object>(null, o => true));
            ContainsMethod = ReflectHelper.GetMethodDefinition(() => Queryable.Contains<object>(null, null));
        }

        protected static T CreateNhFetchRequest<T>(MethodInfo currentFetchMethod, IQueryable query,
            System.Type originating, System.Type related, LambdaExpression expression)
        {
            var callExpression = Expression.Call(currentFetchMethod, query.Expression, expression);
            return
                (T)
                    Activator.CreateInstance(typeof (NhFetchRequest<,>).MakeGenericType(originating, related),
                        query.Provider,
                        callExpression);
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
        public readonly List<string> IncludePaths = new List<string>();


        public IncludeQueryProvider(System.Type type, ISessionImplementor session) : base(session)
        {
            Type = type;
        }

        public IncludeQueryProvider Include(string path)
        {
            IncludePaths.Add(path);
            return this;
        }

        public override IQueryable<T> CreateQuery<T>(Expression expression)
        {
            var newQuery = new NhQueryable<T>(this, expression);
            if (!typeof (T).IsAssignableFrom(Type)) //Select and other methods that returns other types
                throw new NotSupportedException("IncludeQueryProvider does not support mixing types");
            Type = typeof (T); //Possbile typecast to a base type
            return newQuery;
        }

#if NH5
        public override async Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<object>(cancellationToken);
            }
            var resultVisitor = new IncludeRewriterVisitor();
            expression = resultVisitor.Modify(expression);

            if (resultVisitor.Count)
                return await base.ExecuteAsync(expression, cancellationToken);

            var nhQueryable = (IQueryable) Activator.CreateInstance(typeof (NhQueryable<>).MakeGenericType(Type),
                new DefaultQueryProvider(Session), expression);

            return resultVisitor.SkipTake
                ? (object) await ExecuteWithSubquery(nhQueryable, resultVisitor, cancellationToken)
                : (object) await ExecuteWithoutSubQuery(nhQueryable, resultVisitor, cancellationToken);
        }
#endif

        public override object Execute(Expression expression)
        {
            var resultVisitor = new IncludeRewriterVisitor();
            expression = resultVisitor.Modify(expression);

            if (resultVisitor.Count)
                return base.Execute(expression);

            var nhQueryable = (IQueryable) Activator.CreateInstance(typeof (NhQueryable<>).MakeGenericType(Type),
                new DefaultQueryProvider(Session), expression);
            return resultVisitor.SkipTake
                ? ExecuteWithSubquery(nhQueryable, resultVisitor, null)
                : ExecuteWithoutSubQuery(nhQueryable, resultVisitor, null);
        }

#if NH4
        public override dynamic ExecuteFuture(Expression expression)
        {
            var resultVisitor = new IncludeRewriterVisitor();
            expression = resultVisitor.Modify(expression);

            //if (resultVisitor.Count)
            //	return await base.Execute(expression, async);

            var nhQueryable = (IQueryable)Activator.CreateInstance(typeof(NhQueryable<>).MakeGenericType(Type),
                new DefaultQueryProvider(Session), expression);

            return resultVisitor.SkipTake
                ? ExecuteWithSubqueryFuture(nhQueryable, resultVisitor)
                : ExecuteWithoutSubQueryFuture(nhQueryable, resultVisitor) as object;
        }
#elif NH5
        public override IFutureEnumerable<TResult> ExecuteFuture<TResult>(Expression expression)
        {
            var resultVisitor = new IncludeRewriterVisitor();
            expression = resultVisitor.Modify(expression);

            //if (resultVisitor.Count)
            //	return await base.Execute(expression, async);

            var nhQueryable = (IQueryable<TResult>) new NhQueryable<TResult>(new DefaultQueryProvider(Session), expression);

            return resultVisitor.SkipTake
                ? (IFutureEnumerable<TResult>)ExecuteWithSubqueryFuture(nhQueryable, resultVisitor)
                : (IFutureEnumerable<TResult>)ExecuteWithoutSubQueryFuture(nhQueryable, resultVisitor);
        }
#endif

        #region ExecuteWithSubquery

        private dynamic ExecuteWithSubquery(IQueryable query, IncludeRewriterVisitor visitor, CancellationToken? cancellationToken)
        {
            return ExecuteQueryTree(RemoveSkipAndTake(query), visitor, cancellationToken);
        }

        private dynamic ExecuteWithSubqueryFuture(IQueryable query, IncludeRewriterVisitor visitor)
        {
            return ExecuteQueryTreeFuture(RemoveSkipAndTake(query), visitor);
        }

#endregion

#region ExecuteWithoutSubQuery

        private dynamic ExecuteWithoutSubQuery(IQueryable query, IncludeRewriterVisitor visitor, CancellationToken? cancellationToken)
        {
            return ExecuteQueryTree(query, visitor, cancellationToken);
        }

#if NH4
        private object ExecuteWithoutSubQueryFuture(IQueryable query, IncludeRewriterVisitor visitor)
        {
            return ExecuteQueryTreeFuture(query, visitor);
        }
#elif NH5
        private dynamic ExecuteWithoutSubQueryFuture<TResult>(IQueryable<TResult> query, IncludeRewriterVisitor visitor)
        {
            return ExecuteQueryTreeFuture(query, visitor);
        }
#endif

#endregion

#region ExecuteQueryTree

        private dynamic ExecuteQueryTree(IQueryable query, IncludeRewriterVisitor visitor, CancellationToken? cancellationToken)
        {
            var tree = new QueryRelationTree();
            object result = null;
            foreach (var path in IncludePaths)
            {
                tree.AddNode(path);
            }

            var leafs = tree.GetLeafs();
            leafs.Sort();
            var queries = leafs.Aggregate(new QueryInfo(query), FetchFromPath).GetQueries();
            var i = 0;
            foreach (var q in queries)
            {
                if (i == 0)
                    result = ToFutureMethod.MakeGenericMethod(Type).Invoke(null, new object[] { q }); //q.ToFuture();
                else
                    ToFutureMethod.MakeGenericMethod(Type).Invoke(null, new object[] { q }); //q.ToFuture();
                i++;
            }
#if NH5
            if (result != null && result.GetType().IsAssignableToGenericType(typeof(IFutureEnumerable<>)))
            {
                result = cancellationToken.HasValue 
                    ? result.GetType().GetMethod("GetEnumerableAsync").Invoke(result, new object[] { cancellationToken.Value })
                    : result.GetType().GetMethod("GetEnumerable").Invoke(result, null);
            }
#endif
            if (visitor.SingleResult)
            {
#if NH4
                return GetValue(result, visitor.SingleResultMethodName);
#elif NH5
                return cancellationToken.HasValue
                    ? GetValueAsync(result, visitor.SingleResultMethodName)
                    : GetValue(result, visitor.SingleResultMethodName);
#endif
            }
            return result;
        }

        private object ExecuteQueryTreeFuture(IQueryable query, IncludeRewriterVisitor visitor)
        {
            var tree = new QueryRelationTree();
            object result = null;
            foreach (var path in IncludePaths)
            {
                tree.AddNode(path);
            }

            var leafs = tree.GetLeafs();
            leafs.Sort();
            var queries = leafs.Aggregate(new QueryInfo(query), FetchFromPath).GetQueries();
            var i = 0;
            foreach (var q in queries)
            {
                if (i == 0)
                    result = ToFutureMethod.MakeGenericMethod(Type).Invoke(null, new object[] {q}); //q.ToFuture();
                else
                    ToFutureMethod.MakeGenericMethod(Type).Invoke(null, new object[] {q}); //q.ToFuture();
                i++;
            }
            return result;
        }

#endregion ExecuteQueryTree

#if NH5
        private async Task<dynamic> GetValueAsync(dynamic itemsTask, string methodName)
        {
            var items = await itemsTask.ConfigureAwait(false);
            return GetValue(items, methodName);
        }
#endif

        private dynamic GetValue(object items, string methodName)
        {
            var methodInfo =
                typeof (Enumerable).GetMethods()
                    .First(o => o.Name == methodName && o.GetParameters().Length == 1)
                    .MakeGenericMethod(Type);
            try
            {
                return methodInfo.Invoke(null, new[] {items});
            }
            catch (TargetInvocationException e)
            {
#if NH4
                throw;
#elif NH5
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
#endif
            }
            return null;
        }

        private IQueryable RemoveSkipAndTake(IQueryable query)
        {
            var pe = Expression.Parameter(Type);
            var contains = Expression.Call(null,
                ContainsMethod.MakeGenericMethod(Type),
                new Expression[]
                {
                    Expression.Constant(query),
                    pe
                });
            var where = Expression.Call(null,
                WhereMethod.MakeGenericMethod(Type),
                new[]
                {
                    new SkipTakeVisitor().RemoveSkipAndTake(query.Expression),
                    Expression.Lambda(contains, pe)
                });
            return (IQueryable) CreateQueryMethodDefinition.MakeGenericMethod(Type)
                .Invoke(query.Provider, new object[] {where});
        }

        private QueryInfo FetchFromPath(QueryInfo queryInfo, string path)
        {
            var query = queryInfo.Query;
            var currentType = Type;
            var metas = Session.Factory.GetAllClassMetadata()
                .Select(o => o.Value)
#if NH4
                .Where(o => Type.IsAssignableFrom(o.GetMappedClass(EntityMode.Poco)))
#elif NH5
                .Where(o => Type.IsAssignableFrom(o.MappedClass))
#endif
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
#if NH4
                    throw new Exception(string.Format("Property '{0}' does not exist in the type '{1}'", propName,
                        meta.GetMappedClass(EntityMode.Poco).FullName));
#elif NH5
                    throw new Exception($"Property '{propName}' does not exist in the type '{meta.MappedClass.FullName}'");
#endif

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
                    if (!string.IsNullOrEmpty(queryInfo.CollectionPath) &&
                        !collPath.StartsWith(queryInfo.CollectionPath))
                    {
                        //We have to continue fetching within a new base query
                        var nextQueryInfo = queryInfo.GetOrCreateNext();
                        return FetchFromPath(nextQueryInfo, path);
                    }

                    queryInfo.CollectionPath = collPath;
                }
                else
                {
                meta = Session.Factory.GetClassMetadata(assocType.GetAssociatedEntityName(Session.Factory));
                }

                MethodInfo fetchMethod;

                //Try to get the actual property type (so we can skip casting as relinq will throw an exception)
                var relatedProp = currentType.GetProperty(propName);

#if NH4
                var relatedType = relatedProp != null ? relatedProp.PropertyType : meta?.GetMappedClass(EntityMode.Poco);
#elif NH5
                var relatedType = relatedProp != null ? relatedProp.PropertyType : meta?.MappedClass;
#endif
                if (propType.IsCollectionType && relatedProp != null && relatedType.IsGenericType)
                {
                    relatedType = propType.GetType().IsAssignableToGenericType(typeof(GenericMapType<,>))
                        ? typeof(KeyValuePair<,>).MakeGenericType(relatedType.GetGenericArguments())
                        : relatedType.GetGenericArguments()[0];
                }

                var convertToType = propType.IsCollectionType
                    ? typeof (IEnumerable<>).MakeGenericType(relatedType)
                    : null;

                var expression = CreatePropertyExpression(currentType, propName, convertToType);
                //var relatedType = meta.GetMappedClass(EntityMode.Poco); 
                //var convertToType = propType.IsCollectionType
                //    ? typeof (IEnumerable<>).MakeGenericType(relatedType)
                //    : null;
                //var expression = CreatePropertyExpression(currentType, propName, convertToType);
                //No fetch before
                if (TypeHelper.IsSubclassOfRawGeneric(typeof (NhQueryable<>), query.GetType()) || index == 0)
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

                query = CreateNhFetchRequest<IQueryable>(fetchMethod, query, Type, relatedType, expression);
                currentType = relatedType;
                index++;
            }
            queryInfo.Query = query;
            return queryInfo;
        }

    }

}
