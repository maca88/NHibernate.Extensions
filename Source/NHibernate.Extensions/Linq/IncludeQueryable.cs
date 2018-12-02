using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate.Extensions.Internal;
using NHibernate.Linq;

namespace NHibernate.Extensions.Linq
{
    public class IncludeQueryable<TRoot> : NhQueryable<TRoot>, IIncludeQueryable<TRoot>, IIncludeQueryable
    {
        public IncludeQueryable(IQueryProvider provider, Expression expression)
            : base(provider, expression)
        {
        }

        IIncludeQueryable<TRoot> IIncludeQueryable<TRoot>.WithIncludeOptions(Action<IIncludeOptions> action)
        {
            if (Provider is IncludeQueryProvider includeQueryProvider)
            {
                return new IncludeQueryable<TRoot>(includeQueryProvider.WithIncludeOptions(action), Expression);
            }

            return new IncludeQueryable<TRoot>(Provider, Expression);
        }

        IIncludeQueryable IIncludeQueryable.WithIncludeOptions(Action<IIncludeOptions> action)
        {
            if (Provider is IncludeQueryProvider includeQueryProvider)
            {
                return new IncludeQueryable<TRoot>(includeQueryProvider.WithIncludeOptions(action), Expression);
            }

            return new IncludeQueryable<TRoot>(Provider, Expression);
        }
    }

    public class IncludeQueryable<TChild, TRoot> : NhQueryable<TRoot>, IIncludeQueryable<TChild, TRoot>
    {
        public IncludeQueryable(string basePath, IQueryProvider provider, Expression expression)
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
            return new IncludeQueryable<T, TRoot>(newPath, this.IncludeInternal(newPath), Expression);
        }

        IIncludeQueryable<TRoot> IIncludeQueryable<TRoot>.WithIncludeOptions(Action<IIncludeOptions> action)
        {
            return new IncludeQueryable<TRoot>(((IncludeQueryProvider)Provider).WithIncludeOptions(action), Expression);
        }
    }
}
