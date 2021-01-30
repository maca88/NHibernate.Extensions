using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Type;

namespace NHibernate.Extensions.Linq
{
    public class IncludeOptions : IIncludeOptions
    {
        public static IncludeOptions Default = new IncludeOptions();

        internal IncludeOptions Clone()
        {
            return new IncludeOptions
            {
                IgnoreIncludedRelationFunction = IgnoreIncludedRelationFunction,
                MaximumColumnsPerQuery = MaximumColumnsPerQuery
            };
        }

        public int? MaximumColumnsPerQuery { get; set; }

        public Func<ISessionFactory, IAssociationType, bool> IgnoreIncludedRelationFunction { get; set; }

        /// <inheritdoc />
        IIncludeOptions IIncludeOptions.SetIgnoreIncludedRelationFunction(Func<ISessionFactory, IAssociationType, bool> func)
        {
            IgnoreIncludedRelationFunction = func;
            return this;
        }

        /// <inheritdoc />
        IIncludeOptions IIncludeOptions.SetMaximumColumnsPerQuery(int value)
        {
            MaximumColumnsPerQuery = value;
            return this;
        }
    }
}
