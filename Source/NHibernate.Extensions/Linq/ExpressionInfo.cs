using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace NHibernate.Extensions.Linq
{
    internal class ExpressionInfo
    {
        private readonly Expression _originalQuery;

        public ExpressionInfo(Expression expression)
        {
            _originalQuery = expression;
            Expression = expression;
        }

        public Expression Expression { get; set; }

        public string CollectionPath { get; set; }

        public ExpressionInfo Next { get; set; }

        public ExpressionInfo Previous { get; set; }

        public ExpressionInfo GetLast()
        {
            var current = this;
            while (current.Next != null)
            {
                current = current.Next;
            }
            return current;
        }

        public ExpressionInfo GetFirst()
        {
            var current = this;
            while (current.Previous != null)
            {
                current = current.Previous;
            }
            return current;
        }

        public List<Expression> GetExpressions()
        {
            var current = GetFirst();
            var queries = new List<Expression> { current.Expression };

            while (current.Next != null)
            {
                current = current.Next;
                queries.Add(current.Expression);
            }
            return queries;
        }

        public ExpressionInfo GetOrCreateNext()
        {
            if (Next != null) return Next;
            var next = new ExpressionInfo(_originalQuery);
            Next = next;
            next.Previous = this;
            return next;
        }

    }
}
