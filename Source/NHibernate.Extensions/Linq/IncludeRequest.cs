using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Extensions.Internal;
using NHibernate.Linq;

namespace NHibernate.Extensions.Linq
{
    public class IncludeRequest<TChild, TRoot> : NhQueryable<TRoot>, IIncludeQueryable<TChild, TRoot>
    {
        public IncludeRequest(string basePath, IQueryProvider provider, Expression expression)
            : base(provider, expression)
        {
            BasePath = basePath;
        }

        public string BasePath { get; }

        public IQueryable<TRoot> ThenInclude(Expression<Func<TChild, object>> include)
        {
            var path = ExpressionHelper.GetFullPath(include.Body);
            return this.Include($"{BasePath}.{path}");
        }

        public IIncludeQueryable<T, TRoot> ThenInclude<T>(Expression<Func<TChild, IEnumerable<T>>> include)
        {
            var path = ExpressionHelper.GetFullPath(include.Body);
            var newPath = $"{BasePath}.{path}".Trim('.');
            return new IncludeRequest<T, TRoot>(newPath, this.IncludeInternal(newPath), Expression);
        }
    }
}
