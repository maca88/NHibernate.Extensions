using System.Collections.Generic;
using System.Linq.Expressions;

namespace NHibernate.Extensions.Expressions
{
    public class ExpressionInfo
    {
        public ExpressionInfo()
        {
            SubExpressions = new Dictionary<string, SubExpressionInfo>();
        }

        public string FullPath { get; set; }

        public Dictionary<string, SubExpressionInfo> SubExpressions { get; private set; }

        public void AddSubExpression(MemberExpression memberExpression)
        {
            if (SubExpressions.ContainsKey(FullPath)) return;
            SubExpressions.Add(FullPath, new SubExpressionInfo
            {
                Path = FullPath,
                Expression = memberExpression,
                MemberType = memberExpression.Member.DeclaringType,
                //IsLazyLoadEnabled = sessionFactoryInfo.IsLazyLoadEnabled(memberExpression.Member.DeclaringType, memberExpression.Member.Name),
                MemberName = memberExpression.Member.Name
            });
        }
    }
}
