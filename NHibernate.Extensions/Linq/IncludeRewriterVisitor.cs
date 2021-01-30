using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NHibernate.Extensions.Linq
{
    public class IncludeRewriterVisitor : ExpressionVisitor
    {
        public static readonly HashSet<string> SingleResultMethods = new HashSet<string>
        {
            "FirstOrDefault",
            "First",
            "SingleOrDefault",
            "Single",
            "Last",
            "LastOrDefault"
        };

        public static readonly HashSet<string> SkipTakeMethods = new HashSet<string>
        {
            "Skip",
            "Take"
        };

        public static readonly HashSet<string> CountMethods = new HashSet<string>
        {
            "Count",
            "LongCount"
        };

        private static readonly MethodInfo WhereMethod;

        static IncludeRewriterVisitor()
        {
            WhereMethod = typeof(Queryable).GetMethods().First(o => o.Name == "Where");
        }

        public bool SingleResult { get; set; }

        public string SingleResultMethodName { get; set; }

        public bool SkipTake { get; set; }

        public bool Count { get; set; }

        public Expression Modify(Expression expression)
        {
            //Check if is the last called method is a Count method
            if (expression is MethodCallExpression methodCallExpr && CountMethods.Contains(methodCallExpr.Method.Name))
            {
                Count = true;
            }

            return Visit(expression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (SkipTakeMethods.Contains(node.Method.Name))
                SkipTake = true;

            if (SingleResultMethods.Contains(node.Method.Name))
            {
                SingleResult = true;
                SingleResultMethodName = node.Method.Name;
                if (node.Arguments.Count == 2)
                {
                    return Visit(Expression.Call(null,
                        WhereMethod.MakeGenericMethod(node.Method.GetGenericArguments()[0]),
                        new[]
                        {
                            node.Arguments[0],
                            node.Arguments[1]
                        }));
                }
                return Visit(node.Arguments[0]);
            }
            return base.VisitMethodCall(node);
        }
    }

}
