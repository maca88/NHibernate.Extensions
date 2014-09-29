using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace NHibernate.Extensions.Linq
{
    public class SkipTakeVisitor : ExpressionVisitor
    {
        public static readonly HashSet<string> SkipTakeMethods = new HashSet<string>
        {
            "Skip",
            "Take"
        };

        public Expression RemoveSkipAndTake(Expression expression)
        {
            return Visit(expression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (SkipTakeMethods.Contains(node.Method.Name))
                return base.Visit(node.Arguments[0]);

            return base.VisitMethodCall(node);
        }
    }

}
