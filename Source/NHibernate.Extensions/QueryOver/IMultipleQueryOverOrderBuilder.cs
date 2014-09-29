using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NHibernate.Criterion.Lambda;
using NHibernate.Impl;

namespace NHibernate.Extensions
{
    [Serializable]
    public class IMultipleQueryOverOrderBuilder<TRoot, TSubType> : IQueryOverOrderBuilder<TRoot, TSubType> where TRoot : class
    {
        public IMultipleQueryOverOrderBuilder(MultipleQueryOver<TRoot, TSubType> root, Expression<Func<TSubType, object>> path)
            : base(root, path)
        {
        }

        public IMultipleQueryOverOrderBuilder(MultipleQueryOver<TRoot, TSubType> root, Expression<Func<object>> path, bool isAlias)
            : base(root, path, isAlias)
        {
        }

        public IMultipleQueryOverOrderBuilder(MultipleQueryOver<TRoot, TSubType> root, ExpressionProcessor.ProjectionInfo projection)
            : base(root, projection)
        {
        }
    }
}
