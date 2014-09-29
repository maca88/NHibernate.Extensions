using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NHibernate.Extensions;
using NHibernate.SqlCommand;

namespace NHibernate
{
    public static class QueryOverExtensions
    {
        public static IQueryOver<TRoot, TRoot> Include<TRoot>(this IQueryOver<TRoot, TRoot> query, Expression<Func<TRoot, object>> include) where TRoot : class
        {
            var multiQuery = query as MultipleQueryOver<TRoot, TRoot> ?? new MultipleQueryOver<TRoot, TRoot>(query);
            multiQuery.Include(include);
            return multiQuery;
        }
    }
}
