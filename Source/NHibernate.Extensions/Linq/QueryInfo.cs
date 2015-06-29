using System.Collections.Generic;
using System.Linq;

namespace NHibernate.Extensions.Linq
{
    internal class QueryInfo
    {
        private readonly IQueryable _originalQuery;

        public QueryInfo(IQueryable query)
        {
            _originalQuery = query;
            Query = query;
        }

        public IQueryable Query { get; set; }

        public string CollectionPath { get; set; }

        public QueryInfo Next { get; set; }

        public QueryInfo Previous { get; set; }

        public QueryInfo GetLast()
        {
            var current = this;
            while (current.Next != null)
            {
                current = current.Next;
            }
            return current;
        }

        public QueryInfo GetFirst()
        {
            var current = this;
            while (current.Previous != null)
            {
                current = current.Previous;
            }
            return current;
        }

        public List<IQueryable> GetQueries()
        {
            var current = GetFirst();
            var queries = new List<IQueryable> { current.Query };

            while (current.Next != null)
            {
                current = current.Next;
                queries.Add(current.Query);
            }
            return queries;
        }

        public QueryInfo GetOrCreateNext()
        {
            if (Next != null) return Next;
            var next = new QueryInfo(_originalQuery);
            Next = next;
            next.Previous = this;
            return next;
        }

    }
}
