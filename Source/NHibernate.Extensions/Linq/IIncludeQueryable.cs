using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace NHibernate.Extensions.Linq
{
    public interface IIncludeQueryable<TChild, TRoot> : IQueryable<TRoot>
    {
        IQueryable<TRoot> ThenInclude(Expression<Func<TChild, object>> include);

        IIncludeQueryable<T, TRoot> ThenInclude<T>(Expression<Func<TChild, IEnumerable<T>>> include);
    }
}
