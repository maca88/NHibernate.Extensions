using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NHibernate.Criterion;
using NHibernate.Criterion.Lambda;
using NHibernate.Engine;
using NHibernate.Impl;
using NHibernate.SqlCommand;
using NHibernate.Transform;
using Expression = System.Linq.Expressions.Expression;

namespace NHibernate.Extensions
{
    [Serializable]
    public class MultipleQueryOver<TRoot, TSubType> : IQueryOver<TRoot, TSubType> where TRoot : class
    {
        public readonly IQueryOver<TRoot, TSubType> MainQuery;
        public readonly List<Expression<Func<TRoot, object>>> Includes = new List<Expression<Func<TRoot, object>>>();
        public bool SkipTakeUsed = false;

        public MultipleQueryOver(IQueryOver<TRoot, TSubType> mainQuery)
        {
            MainQuery = mainQuery;
        }

        private void Build()
        {
            if (SkipTakeUsed) //TODO: better check
                BuildWithSubquery();
            else
                BuildWithoutSubQuery();
        }

        private void BuildWithoutSubQuery()
        {
            var criteria = (CriteriaImpl)MainQuery.UnderlyingCriteria;
            var tree = new QueryRelationTree();
            foreach (var pathExpr in Includes)
            {
                tree.AddNode(pathExpr);
            }
            foreach (var pair in tree.DeepFirstSearch())
            {
                var query = MainQuery.Clone();
                FillSubQuery(pair.Value, query, criteria);
                query.Future();
            }
        }

        private void FillSubQuery(IEnumerable<string> assocPaths, IQueryOver<TRoot, TRoot> query, CriteriaImpl criteria)
        {
            var idx = 0;
            var aliases = new List<string>();
            foreach (var assocPath in assocPaths)
            {
                var alias = String.Format("{0}{1}", assocPath.Split('.').Last(), idx);
                query.UnderlyingCriteria.CreateAlias(assocPath, alias, JoinType.LeftOuterJoin);
                //SetLock
                if (criteria.LockModes.ContainsKey(alias))
                    query.UnderlyingCriteria.SetLockMode(alias, criteria.LockModes[alias]);
                else if (criteria.LockModes.ContainsKey(CriteriaSpecification.RootAlias)) //Lock all levels
                    query.UnderlyingCriteria.SetLockMode(alias, criteria.LockModes[CriteriaSpecification.RootAlias]);
                aliases.Add(alias);
                idx++;
            }
        }

        private void BuildWithSubquery()
        {
            var criteria = (CriteriaImpl)MainQuery.UnderlyingCriteria;
            var session = (SessionImpl)criteria.Session;
            var metaData = session.SessionFactory.GetClassMetadata(typeof(TRoot));
            var idName = metaData.IdentifierPropertyName; //TODO: multiple props
            var pe = Expression.Parameter(typeof(TRoot));
            var idExprBody = Expression.Property(pe, typeof(TRoot).GetProperty(idName));
            var expr = Expression.Lambda(idExprBody, pe);
            var mainCloned = MainQuery.Clone();
            var projectionList = new List<IProjection>();
            projectionList.Add(ExpressionProcessor.FindMemberProjection(expr.Body).AsProjection());
            mainCloned.UnderlyingCriteria.SetProjection(projectionList.ToArray());
            var tree = new QueryRelationTree();
            foreach (var pathExpr in Includes)
            {
                tree.AddNode(pathExpr);
            }
            foreach (var pair in tree.DeepFirstSearch())
            {
                var query = session.QueryOver<TRoot>();
                //Add a SubQuery
                query.And(Subqueries.PropertyIn(idName, ((QueryOver<TRoot, TRoot>) mainCloned).DetachedCriteria));
                CopyCriteriaValues(criteria, query);
                FillSubQuery(pair.Value, query, criteria);
                query.Future();
            }
        }

        private void CopyCriteriaValues(CriteriaImpl criteria, IQueryOver<TRoot, TRoot> query)
        {
            if (!string.IsNullOrEmpty(criteria.CacheRegion))
                query.CacheRegion(criteria.CacheRegion);
            if (criteria.Cacheable)
                query.Cacheable();
            if (criteria.IsReadOnly)
                query.ReadOnly();
            foreach (var pair in criteria.LockModes)
            {
                query.UnderlyingCriteria.SetLockMode(pair.Key, pair.Value);
            }
            if (criteria.Timeout != RowSelection.NoValue)
                query.UnderlyingCriteria.SetTimeout(criteria.Timeout);
        }

        public IQueryOver<TRoot, TSubType> Include(Expression<Func<TRoot, object>> include)
        {
            Includes.Add(include);
            return this;
        }

        public ICriteria UnderlyingCriteria { get { return MainQuery.UnderlyingCriteria; } }
        public ICriteria RootCriteria { get { return MainQuery.RootCriteria; } }
        public IList<TRoot> List()
        {
            Build();
            return new List<TRoot>(MainQuery.Future());
        }

        public IList<U> List<U>()
        {
            Build();
            return new List<U>(MainQuery.Future<U>());
        }

        public TRoot SingleOrDefault()
        {
            Build();
            return MainQuery.FutureValue().Value;
        }

        public U SingleOrDefault<U>()
        {
            Build();
            return MainQuery.FutureValue<U>().Value;
        }

        public IEnumerable<TRoot> Future()
        {
            Build();
            return MainQuery.Future();
        }

        public IEnumerable<U> Future<U>()
        {
            Build();
            return MainQuery.Future<U>();
        }

        public IFutureValue<TRoot> FutureValue()
        {
            Build();
            return MainQuery.FutureValue();
        }

        public IFutureValue<U> FutureValue<U>()
        {
            Build();
            return MainQuery.FutureValue<U>();
        }

        public IQueryOver<TRoot, TRoot> ToRowCountQuery()
        {
            return MainQuery.ToRowCountQuery();
        }

        public IQueryOver<TRoot, TRoot> ToRowCountInt64Query()
        {
            return MainQuery.ToRowCountInt64Query();
        }

        public int RowCount()
        {
            return MainQuery.RowCount();
        }

        public long RowCountInt64()
        {
            return MainQuery.RowCountInt64();
        }

        public IQueryOver<TRoot, TRoot> Clone()
        {
            var clone = new MultipleQueryOver<TRoot, TRoot>(MainQuery.Clone());
            return clone;
        }

        public IQueryOver<TRoot> ClearOrders()
        {
            MainQuery.ClearOrders();
            return this;
        }

        public IQueryOver<TRoot> Skip(int firstResult)
        {
            SkipTakeUsed = true;
            MainQuery.Skip(firstResult);
            return this;
        }

        public IQueryOver<TRoot> Take(int maxResults)
        {
            SkipTakeUsed = true;
            MainQuery.Take(maxResults);
            return this;
        }

        public IQueryOver<TRoot> Cacheable()
        {
            MainQuery.Cacheable();
            return this;
        }

        public IQueryOver<TRoot> CacheMode(CacheMode cacheMode)
        {
            MainQuery.CacheMode(cacheMode);
            return this;
        }

        public IQueryOver<TRoot> CacheRegion(string cacheRegion)
        {
            MainQuery.CacheRegion(cacheRegion);
            return this;
        }

        public IQueryOver<TRoot> ReadOnly()
        {
            MainQuery.ReadOnly();
            return this;
        }

        public IQueryOver<TRoot, TSubType> And(Expression<Func<TSubType, bool>> expression)
        {
            MainQuery.And(expression);
            return this;
        }

        public IQueryOver<TRoot, TSubType> And(Expression<Func<bool>> expression)
        {
            MainQuery.And(expression);
            return this;
        }

        public IQueryOver<TRoot, TSubType> And(ICriterion expression)
        {
            MainQuery.And(expression);
            return this;
        }

        public IQueryOver<TRoot, TSubType> AndNot(Expression<Func<TSubType, bool>> expression)
        {
            MainQuery.AndNot(expression);
            return this;
        }

        public IQueryOver<TRoot, TSubType> AndNot(Expression<Func<bool>> expression)
        {
            MainQuery.AndNot(expression);
            return this;
        }

        public IQueryOverRestrictionBuilder<TRoot, TSubType> AndRestrictionOn(Expression<Func<TSubType, object>> expression)
        {
            return new IMultipleQueryOverRestrictionBuilder<TRoot, TSubType>(this, ExpressionProcessor.FindMemberProjection(expression.Body));
        }

        public IQueryOverRestrictionBuilder<TRoot, TSubType> AndRestrictionOn(Expression<Func<object>> expression)
        {
            return new IMultipleQueryOverRestrictionBuilder<TRoot, TSubType>(this, ExpressionProcessor.FindMemberProjection(expression.Body));
        }

        public IQueryOver<TRoot, TSubType> Where(Expression<Func<TSubType, bool>> expression)
        {
            MainQuery.Where(expression);
            return this;
        }

        public IQueryOver<TRoot, TSubType> Where(Expression<Func<bool>> expression)
        {
            MainQuery.Where(expression);
            return this;
        }

        public IQueryOver<TRoot, TSubType> Where(ICriterion expression)
        {
            MainQuery.Where(expression);
            return this;
        }

        public IQueryOver<TRoot, TSubType> WhereNot(Expression<Func<TSubType, bool>> expression)
        {
            MainQuery.WhereNot(expression);
            return this;
        }

        public IQueryOver<TRoot, TSubType> WhereNot(Expression<Func<bool>> expression)
        {
            MainQuery.WhereNot(expression);
            return this;
        }

        public IQueryOverRestrictionBuilder<TRoot, TSubType> WhereRestrictionOn(Expression<Func<TSubType, object>> expression)
        {
            return new IMultipleQueryOverRestrictionBuilder<TRoot, TSubType>(this, ExpressionProcessor.FindMemberProjection(expression.Body));
        }

        public IQueryOverRestrictionBuilder<TRoot, TSubType> WhereRestrictionOn(Expression<Func<object>> expression)
        {
            return new IMultipleQueryOverRestrictionBuilder<TRoot, TSubType>(this, ExpressionProcessor.FindMemberProjection(expression.Body));
        }

        public IQueryOver<TRoot, TSubType> Select(params Expression<Func<TRoot, object>>[] projections)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, TSubType> Select(params IProjection[] projections)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, TSubType> SelectList(Func<QueryOverProjectionBuilder<TRoot>, QueryOverProjectionBuilder<TRoot>> list)
        {
            throw new NotSupportedException();
        }

        public IQueryOverOrderBuilder<TRoot, TSubType> OrderBy(Expression<Func<TSubType, object>> path)
        {
            return new IMultipleQueryOverOrderBuilder<TRoot, TSubType>(this, path);
        }

        public IQueryOverOrderBuilder<TRoot, TSubType> OrderBy(Expression<Func<object>> path)
        {
            return new IMultipleQueryOverOrderBuilder<TRoot, TSubType>(this, path, false); 
        }

        public IQueryOverOrderBuilder<TRoot, TSubType> OrderBy(IProjection projection)
        {
            return new IMultipleQueryOverOrderBuilder<TRoot, TSubType>(this, ExpressionProcessor.ProjectionInfo.ForProjection(projection));
        }

        public IQueryOverOrderBuilder<TRoot, TSubType> OrderByAlias(Expression<Func<object>> path)
        {
            return new IMultipleQueryOverOrderBuilder<TRoot, TSubType>(this, path, true);
        }

        public IQueryOverOrderBuilder<TRoot, TSubType> ThenBy(Expression<Func<TSubType, object>> path)
        {
            return new IMultipleQueryOverOrderBuilder<TRoot, TSubType>(this, path); 
        }

        public IQueryOverOrderBuilder<TRoot, TSubType> ThenBy(Expression<Func<object>> path)
        {
            return new IMultipleQueryOverOrderBuilder<TRoot, TSubType>(this, path, false); 
        }

        public IQueryOverOrderBuilder<TRoot, TSubType> ThenBy(IProjection projection)
        {
            return new IMultipleQueryOverOrderBuilder<TRoot, TSubType>(this, ExpressionProcessor.ProjectionInfo.ForProjection(projection));
        }

        public IQueryOverOrderBuilder<TRoot, TSubType> ThenByAlias(Expression<Func<object>> path)
        {
            return new IMultipleQueryOverOrderBuilder<TRoot, TSubType>(this, path, true); 
        }

        public IQueryOver<TRoot, TSubType> TransformUsing(IResultTransformer resultTransformer)
        {
            MainQuery.TransformUsing(resultTransformer);
            return this;
        }

        public IQueryOverFetchBuilder<TRoot, TSubType> Fetch(Expression<Func<TRoot, object>> path)
        {
            throw new NotSupportedException();
        }

        public IQueryOverLockBuilder<TRoot, TSubType> Lock()
        {
            return new IMultipleQueryOverLockBuilder<TRoot, TSubType>(this, null); 
        }

        public IQueryOverLockBuilder<TRoot, TSubType> Lock(Expression<Func<object>> alias)
        {
            return new IMultipleQueryOverLockBuilder<TRoot, TSubType>(this, alias); 
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<TSubType, U>> path)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<U>> path)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<TSubType, U>> path, Expression<Func<U>> alias)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<U>> path, Expression<Func<U>> alias)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<TSubType, U>> path, JoinType joinType)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<U>> path, JoinType joinType)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<TSubType, U>> path, Expression<Func<U>> alias, JoinType joinType)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<TSubType, U>> path, Expression<Func<U>> alias, JoinType joinType, ICriterion withClause)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<U>> path, Expression<Func<U>> alias, JoinType joinType)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<U>> path, Expression<Func<U>> alias, JoinType joinType, ICriterion withClause)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<TSubType, IEnumerable<U>>> path)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<IEnumerable<U>>> path)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<TSubType, IEnumerable<U>>> path, Expression<Func<U>> alias)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<IEnumerable<U>>> path, Expression<Func<U>> alias)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<TSubType, IEnumerable<U>>> path, JoinType joinType)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<IEnumerable<U>>> path, JoinType joinType)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<TSubType, IEnumerable<U>>> path, Expression<Func<U>> alias, JoinType joinType)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<TSubType, IEnumerable<U>>> path, Expression<Func<U>> alias, JoinType joinType, ICriterion withClause)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<IEnumerable<U>>> path, Expression<Func<U>> alias, JoinType joinType)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, U> JoinQueryOver<U>(Expression<Func<IEnumerable<U>>> path, Expression<Func<U>> alias, JoinType joinType, ICriterion withClause)
        {
            throw new NotSupportedException();
        }

        public IQueryOver<TRoot, TSubType> JoinAlias(Expression<Func<TSubType, object>> path, Expression<Func<object>> alias)
        {
            MainQuery.JoinAlias(path, alias);
            return this;
        }

        public IQueryOver<TRoot, TSubType> JoinAlias(Expression<Func<object>> path, Expression<Func<object>> alias)
        {
            MainQuery.JoinAlias(path, alias);
            return this;
        }

        public IQueryOver<TRoot, TSubType> JoinAlias(Expression<Func<TSubType, object>> path, Expression<Func<object>> alias, JoinType joinType)
        {
            MainQuery.JoinAlias(path, alias, joinType);
            return this;
        }

        public IQueryOver<TRoot, TSubType> JoinAlias<U>(Expression<Func<TSubType, U>> path, Expression<Func<U>> alias, JoinType joinType, ICriterion withClause)
        {
            MainQuery.JoinAlias(path, alias, joinType, withClause);
            return this;
        }

        public IQueryOver<TRoot, TSubType> JoinAlias<U>(Expression<Func<TSubType, IEnumerable<U>>> path, Expression<Func<U>> alias, JoinType joinType, ICriterion withClause)
        {
            MainQuery.JoinAlias(path, alias, joinType, withClause);
            return this;
        }

        public IQueryOver<TRoot, TSubType> JoinAlias(Expression<Func<object>> path, Expression<Func<object>> alias, JoinType joinType)
        {
            MainQuery.JoinAlias(path, alias, joinType);
            return this;
        }

        public IQueryOver<TRoot, TSubType> JoinAlias<U>(Expression<Func<U>> path, Expression<Func<U>> alias, JoinType joinType, ICriterion withClause)
        {
            MainQuery.JoinAlias(path, alias, joinType, withClause);
            return this;
        }

        public IQueryOver<TRoot, TSubType> JoinAlias<U>(Expression<Func<IEnumerable<U>>> path, Expression<Func<U>> alias, JoinType joinType, ICriterion withClause)
        {
            MainQuery.JoinAlias(path, alias, joinType, withClause);
            return this;
        }

        public IQueryOverSubqueryBuilder<TRoot, TSubType> WithSubquery { get { throw new NotSupportedException(); } }
        public IQueryOverJoinBuilder<TRoot, TSubType> Inner { get { throw new NotSupportedException(); } }
        public IQueryOverJoinBuilder<TRoot, TSubType> Left { get { throw new NotSupportedException(); } }
        public IQueryOverJoinBuilder<TRoot, TSubType> Right { get { throw new NotSupportedException(); } }
        public IQueryOverJoinBuilder<TRoot, TSubType> Full { get { throw new NotSupportedException(); } }
    }
}
