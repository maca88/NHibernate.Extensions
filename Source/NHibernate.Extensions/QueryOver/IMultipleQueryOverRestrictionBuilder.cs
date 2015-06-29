using System;
using NHibernate.Criterion.Lambda;
using NHibernate.Impl;

namespace NHibernate.Extensions
{
    [Serializable]
    public class IMultipleQueryOverRestrictionBuilder<TRoot, TSubType> : IQueryOverRestrictionBuilder<TRoot, TSubType> where TRoot : class
    {
        public IMultipleQueryOverRestrictionBuilder(MultipleQueryOver<TRoot, TSubType> root, ExpressionProcessor.ProjectionInfo projection)
            : base(root, projection)
        {
        }
    }
}
