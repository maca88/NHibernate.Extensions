using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Type;

namespace NHibernate.Extensions.Linq
{
    public interface IIncludeOptions
    {
        /// <summary>
        /// Set a function that ignores a relation from being fetched when true is returned. When a relation is ignored
        /// within a path, all relations that are after the ignored one, will also be ignored.
        /// </summary>
        IIncludeOptions SetIgnoreIncludedRelationFunction(Func<ISessionFactory, IAssociationType, bool> func);

        /// <summary>
        /// Set the maximum columns that are allowed in a single sql query when combining multiple included paths into a single query.
        /// </summary>
        IIncludeOptions SetMaximumColumnsPerQuery(int value);
    }
}
