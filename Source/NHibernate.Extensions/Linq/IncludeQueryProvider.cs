using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using NHibernate.Engine;
using NHibernate.Extensions.Internal;
using NHibernate.Linq;
using NHibernate.Type;
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
        protected static readonly MethodInfo EvaluateIndependentSubtreesMethod;
        protected static readonly MethodInfo SelectMethod;
        protected static readonly MethodInfo WhereMethod;
        protected static readonly MethodInfo ContainsMethod;
        protected static readonly MethodInfo ToFutureMethod;
        protected static readonly MethodInfo ToFutureValueMethod;
        protected static readonly MethodInfo ToFutureAsyncMethod;
        protected static readonly MethodInfo ToFutureValueAsyncMethod;
        protected static readonly MethodInfo ToListAsyncMethod;

        static IncludeQueryProvider()
        {
            CreateQueryMethodDefinition =
                ReflectionHelper.GetMethodDefinition((IncludeQueryProvider p) => p.CreateQuery<object>(null));
            FetchMethod = typeof (EagerFetchingExtensionMethods).GetMethod("Fetch",
                BindingFlags.Public | BindingFlags.Static);
            FetchManyMethod = typeof (EagerFetchingExtensionMethods).GetMethod("FetchMany",
                BindingFlags.Public | BindingFlags.Static);
            ThenFetchMethod = typeof (EagerFetchingExtensionMethods).GetMethod("ThenFetch",
                BindingFlags.Public | BindingFlags.Static);
            ThenFetchManyMethod = typeof (EagerFetchingExtensionMethods).GetMethod("ThenFetchMany",
                BindingFlags.Public | BindingFlags.Static);
            EvaluateIndependentSubtreesMethod = typeof (ISession).Assembly.GetType(
                "NHibernate.Linq.Visitors.NhPartialEvaluatingExpressionTreeVisitor")
                .GetMethod("EvaluateIndependentSubtrees");

            ToFutureMethod = typeof (LinqExtensionMethods).GetMethods().First(o => o.Name == "ToFuture");
            ToFutureValueMethod =
                typeof (LinqExtensionMethods).GetMethods()
                    .First(o => o.Name == "ToFutureValue" && o.GetParameters().Length == 1);
            ToFutureAsyncMethod = typeof (LinqExtensionMethods).GetMethods().First(o => o.Name == "ToFutureAsync");
            ToFutureValueAsyncMethod =
                typeof (LinqExtensionMethods).GetMethods()
                    .First(o => o.Name == "ToFutureValueAsync" && o.GetParameters().Length == 1);
            SelectMethod = typeof (Queryable).GetMethods().First(o => o.Name == "Select");
            WhereMethod = typeof (Queryable).GetMethods().First(o => o.Name == "Where");
            ContainsMethod = typeof (Queryable).GetMethods().First(o => o.Name == "Contains");
            ToListAsyncMethod =
                typeof (AsyncEnumerable).GetMethods().First(o => o.Name == "ToList" && o.GetParameters().Length == 1);
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
        public IQueryable MainQuery;


        public IncludeQueryProvider(System.Type type, IQueryable mainQuery, ISessionImplementor session) : base(session)
        {
            Type = type;
            MainQuery = mainQuery;
        }

        public IncludeQueryProvider Include(string path)
        {
            IncludePaths.Add(path);
            return this;
        }

        public override IQueryable CreateQuery(Expression expression)
        {
            var m = CreateQueryMethodDefinition.MakeGenericMethod(expression.Type.GetGenericArguments()[0]);
            return (IQueryable) m.Invoke(this, new object[] {expression});
        }

        public override IQueryable<T> CreateQuery<T>(Expression expression)
        {
            var newQuery = new NhQueryable<T>(this, expression);
            var mainQuery = newQuery as IQueryable;
            if (!typeof (T).IsAssignableFrom(Type)) //Select and other methods that returns other types
                throw new NotSupportedException("IncludeQueryProvider does not support mixing types");
            Type = typeof (T); //Possbile typecast to a base type
            MainQuery = mainQuery;
            return newQuery;
        }

        public override async Task<object> ExecuteAsync(Expression expression)
        {
            var resultVisitor = new IncludeRewriterVisitor();
            expression = resultVisitor.Modify(expression);

            if (resultVisitor.Count)
                return await base.ExecuteAsync(expression);

            var nhQueryable = (IQueryable) Activator.CreateInstance(typeof (NhQueryable<>).MakeGenericType(Type),
                new DefaultQueryProvider(Session), expression);

            return resultVisitor.SkipTake
                ? await ExecuteWithSubqueryAsync(nhQueryable, resultVisitor).ConfigureAwait(false)
                : await ExecuteWithoutSubQueryAsync(nhQueryable, resultVisitor).ConfigureAwait(false);
        }

        public override object Execute(Expression expression)
        {
            var resultVisitor = new IncludeRewriterVisitor();
            expression = resultVisitor.Modify(expression);

            if (resultVisitor.Count)
                return base.Execute(expression);

            var nhQueryable = (IQueryable) Activator.CreateInstance(typeof (NhQueryable<>).MakeGenericType(Type),
                new DefaultQueryProvider(Session), expression);

            try
            {
                return resultVisitor.SkipTake
                    ? ExecuteWithSubquery(nhQueryable, resultVisitor)
                    : ExecuteWithoutSubQuery(nhQueryable, resultVisitor);
            }
            catch (AggregateException e)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }
            return null;
        }

        public override object ExecuteFuture(Expression expression)
        {
            var resultVisitor = new IncludeRewriterVisitor();
            expression = resultVisitor.Modify(expression);

            //if (resultVisitor.Count)
            //	return await base.Execute(expression, async);

            var nhQueryable = (IQueryable)Activator.CreateInstance(typeof(NhQueryable<>).MakeGenericType(Type),
                new DefaultQueryProvider(Session), expression);

            return resultVisitor.SkipTake
                ? ExecuteWithSubqueryFuture(nhQueryable, resultVisitor, false)
                : ExecuteWithoutSubQueryFuture(nhQueryable, resultVisitor, false);
        }

        public override object ExecuteFutureAsync(Expression expression)
        {
            var resultVisitor = new IncludeRewriterVisitor();
            expression = resultVisitor.Modify(expression);

            //if (resultVisitor.Count)
            //	return await base.Execute(expression, async);

            var nhQueryable = (IQueryable) Activator.CreateInstance(typeof (NhQueryable<>).MakeGenericType(Type),
                new DefaultQueryProvider(Session), expression);

            return resultVisitor.SkipTake
                ? ExecuteWithSubqueryFuture(nhQueryable, resultVisitor, true)
                : ExecuteWithoutSubQueryFuture(nhQueryable, resultVisitor, true);
        }

        #region ExecuteWithSubquery

        private Task<object> ExecuteWithSubqueryAsync(IQueryable query, IncludeRewriterVisitor visitor)
        {
            return ExecuteQueryTreeAsync(RemoveSkipAndTake(query), visitor);
        }

        private object ExecuteWithSubquery(IQueryable query, IncludeRewriterVisitor visitor)
        {
            return ExecuteQueryTree(RemoveSkipAndTake(query), visitor);
        }

        private object ExecuteWithSubqueryFuture(IQueryable query, IncludeRewriterVisitor visitor, bool async)
        {
            return ExecuteQueryTreeFuture(RemoveSkipAndTake(query), visitor, async);
        }

        #endregion

        #region ExecuteWithoutSubQuery

        private Task<object> ExecuteWithoutSubQueryAsync(IQueryable query, IncludeRewriterVisitor visitor)
        {
            return ExecuteQueryTreeAsync(query, visitor);
        }

        private object ExecuteWithoutSubQuery(IQueryable query, IncludeRewriterVisitor visitor)
        {
            return ExecuteQueryTree(query, visitor);
        }

        private object ExecuteWithoutSubQueryFuture(IQueryable query, IncludeRewriterVisitor visitor, bool async)
        {
            return ExecuteQueryTreeFuture(query, visitor, async);
        }

        #endregion

        #region ExecuteQueryTree

        private object ExecuteQueryTree(IQueryable query, IncludeRewriterVisitor visitor)
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
            if (visitor.SingleResult)
            {
                return GetValue(result, visitor.SingleResultMethodName);
            }
            return result;
        }

        private async Task<object> ExecuteQueryTreeAsync(IQueryable query, IncludeRewriterVisitor visitor)
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
                    result = ToFutureAsyncMethod.MakeGenericMethod(Type).Invoke(null, new object[] {q}); //q.ToFuture();
                else
                    ToFutureAsyncMethod.MakeGenericMethod(Type).Invoke(null, new object[] {q}); //q.ToFuture();
                i++;
            }
            if (result != null && result.GetType().IsAssignableToGenericType(typeof (IAsyncEnumerable<>)))
            {
                result = await ((dynamic) ToListAsyncMethod.MakeGenericMethod(Type).Invoke(null, new[] {result})).ConfigureAwait(false);
            }
            if (visitor.SingleResult)
            {
                return GetValue(result, visitor.SingleResultMethodName);
            }
            return result;
        }

        private object ExecuteQueryTreeFuture(IQueryable query, IncludeRewriterVisitor visitor, bool async)
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
            var toFutureMethod = async ? ToFutureAsyncMethod : ToFutureMethod;
            var i = 0;
            foreach (var q in queries)
            {
                if (i == 0)
                    result = toFutureMethod.MakeGenericMethod(Type).Invoke(null, new object[] {q}); //q.ToFuture();
                else
                    toFutureMethod.MakeGenericMethod(Type).Invoke(null, new object[] {q}); //q.ToFuture();
                i++;
            }
            return result;
        }

        #endregion ExecuteQueryTree

        private object GetValue(object items, string methodName)
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
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
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
                .Where(o => Type.IsAssignableFrom(o.GetMappedClass(EntityMode.Poco)))
                .ToList();
            if (!metas.Any())
                throw new HibernateException(string.Format("Metadata for type '{0}' was not found", Type));
            if (metas.Count > 1)
                throw new HibernateException(string.Format("There are more than one metadata for type '{0}'", Type));

            var meta = metas.First();
            var paths = path.Split('.');
            var index = 0;
            foreach (var propName in paths)
            {
                var propType = meta.GetPropertyType(propName);
                if (propType == null)
                    throw new Exception(string.Format("Property '{0}' does not exist in the type '{1}'", propName,
                        meta.GetMappedClass(EntityMode.Poco).FullName));

                var assocType = propType as IAssociationType;
                if (assocType == null)
                {
                    throw new Exception(string.Format("Property '{0}' does not implement IAssociationType interface",
                        propName));
                }

                if (assocType.IsCollectionType)
                {
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

                meta = Session.Factory.GetClassMetadata(assocType.GetAssociatedEntityName(Session.Factory));

                MethodInfo fetchMethod;

                //Try to get the actual property type (so we can skip casting as relinq will throw an exception)
                var relatedProp = currentType.GetProperty(propName);

                var relatedType = relatedProp != null ? relatedProp.PropertyType : meta.GetMappedClass(EntityMode.Poco);
                if (propType.IsCollectionType && relatedProp != null && relatedType.IsGenericType)
                {
                    relatedType = relatedType.GetGenericArguments()[0];
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
                        ? FetchManyMethod.MakeGenericMethod(new[] {Type, relatedType})
                        : FetchMethod.MakeGenericMethod(new[] {Type, relatedType});
                }
                else
                {
                    fetchMethod = propType.IsCollectionType
                        ? ThenFetchManyMethod.MakeGenericMethod(new[] {Type, currentType, relatedType})
                        : ThenFetchMethod.MakeGenericMethod(new[] {Type, currentType, relatedType});
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
