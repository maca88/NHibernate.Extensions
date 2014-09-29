using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NHibernate.Engine;
using NHibernate.Linq;
using NHibernate.Type;

namespace NHibernate.Extensions.Linq
{
    public class IncludeQueryProvider : DefaultQueryProvider
    {
        protected static MethodInfo FetchMethod;
        protected static MethodInfo FetchManyMethod;
        protected static MethodInfo ThenFetchMethod;
        protected static MethodInfo ThenFetchManyMethod;
        protected static MethodInfo EvaluateIndependentSubtreesMethod;
        protected static MethodInfo SelectMethod;
        protected static MethodInfo WhereMethod;
        protected static MethodInfo ContainsMethod;

        static IncludeQueryProvider()
        {
            FetchMethod = typeof (EagerFetchingExtensionMethods).GetMethod("Fetch",
                BindingFlags.Public | BindingFlags.Static);
            FetchManyMethod = typeof(EagerFetchingExtensionMethods).GetMethod("FetchMany",
                BindingFlags.Public | BindingFlags.Static);
            ThenFetchMethod = typeof(EagerFetchingExtensionMethods).GetMethod("ThenFetch",
                BindingFlags.Public | BindingFlags.Static);
            ThenFetchManyMethod = typeof(EagerFetchingExtensionMethods).GetMethod("ThenFetchMany",
                BindingFlags.Public | BindingFlags.Static);
            EvaluateIndependentSubtreesMethod = typeof (ISession).Assembly.GetType(
                "NHibernate.Linq.Visitors.NhPartialEvaluatingExpressionTreeVisitor")
                .GetMethod("EvaluateIndependentSubtrees");
            SelectMethod = typeof(Queryable).GetMethods().First(o => o.Name == "Select");
            WhereMethod = typeof(Queryable).GetMethods().First(o => o.Name == "Where");
            ContainsMethod = typeof(Queryable).GetMethods().First(o => o.Name == "Contains");
        }

        public IncludeQueryProvider(ISessionImplementor session) : base(session)
        {
        }

        protected static T CreateNhFetchRequest<T>(MethodInfo currentFetchMethod, IQueryable query, System.Type originating, System.Type related, LambdaExpression expression)
        {
            var callExpression = Expression.Call(currentFetchMethod, query.Expression, expression);
            return (T)Activator.CreateInstance(typeof (NhFetchRequest<,>).MakeGenericType(originating, related), query.Provider,
                callExpression);
        }

        protected static LambdaExpression CreatePropertyExpression(System.Type type, string propName, System.Type convertToType = null)
        {
            var p = Expression.Parameter(type);
            var body = Expression.Property(p, propName);
            if (convertToType == null) return Expression.Lambda(body, p);
            var converted = Expression.Convert(body, convertToType);
            return Expression.Lambda(converted, p);
        }
    }

    public class IncludeQueryProvider<TRoot> : IncludeQueryProvider
    {
        private static readonly MethodInfo CreateQueryMethodDefinition = ReflectionHelper.GetMethodDefinition((IncludeQueryProvider<TRoot> p) => p.CreateQuery<object>(null));
        public NhQueryable<TRoot> MainQuery;
        public readonly List<Expression<Func<TRoot, object>>> Includes = new List<Expression<Func<TRoot, object>>>();

        public IncludeQueryProvider(NhQueryable<TRoot> mainQuery, ISessionImplementor session)
            : base(session)
        {
            MainQuery = mainQuery;
        }

        public IncludeQueryProvider<TRoot> Include(Expression<Func<TRoot, object>> include)
        {
            Includes.Add(include);
            return this;
        }

        public override IQueryable CreateQuery(Expression expression)
        {
            var m = CreateQueryMethodDefinition.MakeGenericMethod(expression.Type.GetGenericArguments()[0]);
            return (IQueryable)m.Invoke(this, new object[] { expression });
        }

        public override IQueryable<T> CreateQuery<T>(Expression expression)
        {
            var newQuery = new NhQueryable<T>(this, expression);
            var mainQuery = newQuery as NhQueryable<TRoot>;
            if (mainQuery != null) //TODO: disallow Select and other methods that returns other types
                MainQuery = mainQuery;
            return newQuery;
        }

        public override object Execute(Expression expression)
        {
            var resultVisitor = new IncludeRewriterVisitor();
            expression = resultVisitor.Modify(expression);
            var nhQueryable = new NhQueryable<TRoot>(new DefaultQueryProvider(Session), expression);
            return resultVisitor.SkipTake
                ? ExecuteWithSubquery(nhQueryable, resultVisitor) 
                : ExecuteWithoutSubQuery(nhQueryable, resultVisitor);
        }

        public override object ExecuteFuture(Expression expression)
        {
            return Execute(expression);
        }

        private object ExecuteWithSubquery(IQueryable<TRoot> query, IncludeRewriterVisitor visitor)
        {
            var type = typeof (TRoot);
            var pe = Expression.Parameter(type);
            var contains = Expression.Call(null,
                ContainsMethod.MakeGenericMethod(type),
                new Expression[]
                {
                    Expression.Constant(query),
                    pe
                });
            var where = Expression.Call(null,
                WhereMethod.MakeGenericMethod(type),
                new[]
                {
                    new SkipTakeVisitor().RemoveSkipAndTake(query.Expression),
                    Expression.Lambda(contains, pe)
                });
            query = (IQueryable<TRoot>) CreateQueryMethodDefinition.MakeGenericMethod(type)
                .Invoke(query.Provider, new object[] {where});


            return ExecuteQueryTree(query, visitor);
        }

        private object ExecuteWithoutSubQuery(NhQueryable<TRoot> query, IncludeRewriterVisitor visitor)
        {
            return ExecuteQueryTree(query, visitor);
        }

        private object ExecuteQueryTree(IQueryable<TRoot> query, IncludeRewriterVisitor visitor)
        {
            var tree = new QueryRelationTree<TRoot>();
            IEnumerable<TRoot> result = null;
            IFutureValue<TRoot> futureVal = null;
            foreach (var pathExpr in Includes)
            {
                tree.AddNode(pathExpr);
            }

            var queries = tree.DeepFirstSearch().Select(pair => FetchFromPath(query, pair.Value.Last())).ToList();

            var i = 0;
            foreach (var q in queries)
            {
                if (i == 0 && visitor.SingleResult && visitor.FutureValue)
                    futureVal = q.ToFutureValue();
                else if (i == 0)
                    result = q.ToFuture();
                else
                    q.ToFuture();
                i++;
            }
            if (futureVal != null)
                return futureVal;
            if (visitor.SingleResult && !visitor.FutureValue)
                return GetValue(result, visitor.SingleResultMethodName);

            return result;
        }

        private static TRoot GetValue(IEnumerable<TRoot> items, string methodName)
        {
            var methodInfo = typeof(Enumerable).GetMethods().First(o => o.Name == methodName && o.GetParameters().Length == 1).MakeGenericMethod(typeof(TRoot));
            try
            {
                return (TRoot) methodInfo.Invoke(null, new object[] {items});
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        private IQueryable<TRoot> FetchFromPath(IQueryable<TRoot> query, string path)
        {
            var type = typeof (TRoot);
            var currentType = type;
            var meta = Session.Factory.GetClassMetadata(type);
            var paths = path.Split('.');

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

                meta = Session.Factory.GetClassMetadata(assocType.GetAssociatedEntityName(Session.Factory));

                MethodInfo fetchMethod;
                var relatedType = meta.GetMappedClass(EntityMode.Poco);
                var convertToType = propType.IsCollectionType
                    ? typeof (IEnumerable<>).MakeGenericType(relatedType)
                    : null;
                var expression = CreatePropertyExpression(currentType, propName, convertToType);
                //No fetch before
                if (query is NhQueryable<TRoot>)
                {
                    fetchMethod = propType.IsCollectionType 
                        ? FetchManyMethod.MakeGenericMethod(new[] {type, relatedType})
                        : FetchMethod.MakeGenericMethod(new[] {type, relatedType});
                }
                else
                {
                    fetchMethod = propType.IsCollectionType
                        ? ThenFetchManyMethod.MakeGenericMethod(new[] { type, currentType, relatedType })
                        : ThenFetchMethod.MakeGenericMethod(new[] { type, currentType, relatedType });
                }

                query = CreateNhFetchRequest<IQueryable<TRoot>>(fetchMethod, query, type, relatedType, expression);
                currentType = relatedType;
            }
            return query;
        }

    }
}
