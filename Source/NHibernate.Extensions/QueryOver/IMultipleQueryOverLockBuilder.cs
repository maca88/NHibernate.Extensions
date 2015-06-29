using System;
using System.Linq.Expressions;
using NHibernate.Criterion.Lambda;

namespace NHibernate.Extensions
{
    [Serializable]
    public class IMultipleQueryOverLockBuilder<TRoot, TSubType> : IQueryOverLockBuilder<TRoot, TSubType> where TRoot : class
    {
        public IMultipleQueryOverLockBuilder(MultipleQueryOver<TRoot, TSubType> root, Expression<Func<object>> alias)
            : base(root, alias)
        {
        }
    }
}
