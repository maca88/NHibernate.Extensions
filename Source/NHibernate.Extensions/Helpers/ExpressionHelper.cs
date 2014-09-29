using System;
using System.Linq.Expressions;
using NHibernate.Extensions.Expressions;
using NHibernate.Util;

namespace NHibernate.Extensions.Helpers
{
    public static class ExpressionHelper
    {
        public static string GetExpressionPath(Expression expression)
        {
            return GetExpressionInfo(expression, new ExpressionInfo());
        }

        public static string GetExpressionInfo(Expression expression, ExpressionInfo expressionInfo)
        {
            string fullPath;
            var memberExpression = expression as MemberExpression;
            if (memberExpression != null)
            {
                if (memberExpression.Expression.NodeType == ExpressionType.MemberAccess
                    || memberExpression.Expression.NodeType == ExpressionType.Call)
                {
                    if (memberExpression.Member.DeclaringType.IsNullable())
                    {
                        // it's a Nullable<T>, so ignore any .Value
                        if (memberExpression.Member.Name == "Value")
                        {
                            fullPath = GetExpressionInfo(memberExpression.Expression, expressionInfo);
                            expressionInfo.FullPath = fullPath;
                            return fullPath;
                        }
                    }
                    fullPath = GetExpressionInfo(memberExpression.Expression, expressionInfo) + "." + memberExpression.Member.Name;
                    expressionInfo.FullPath = fullPath;
                    expressionInfo.AddSubExpression(memberExpression);
                    return fullPath;
                    //return GetExpressionInfo(memberExpression.Expression) + "." + memberExpression.Member.Name;
                }
                if (IsConversion(memberExpression.Expression.NodeType))
                {
                    fullPath = (GetExpressionInfo(memberExpression.Expression, expressionInfo) + "." + memberExpression.Member.Name).TrimStart('.'); ;
                    expressionInfo.FullPath = fullPath;
                    expressionInfo.AddSubExpression(memberExpression);
                    return fullPath;
                    //return (GetExpressionInfo(memberExpression.Expression) + "." + memberExpression.Member.Name).TrimStart('.');
                }

                fullPath = memberExpression.Member.Name;
                expressionInfo.FullPath = fullPath;
                expressionInfo.AddSubExpression(memberExpression);
                return fullPath;
                //return memberExpression.Member.Name;
            }

            var unaryExpression = expression as UnaryExpression;
            if (unaryExpression != null)
            {
                if (!IsConversion(unaryExpression.NodeType))
                    throw new Exception("Cannot interpret member from " + expression);
                fullPath = GetExpressionInfo(unaryExpression.Operand, expressionInfo);
                expressionInfo.FullPath = fullPath;
                return fullPath;
                //return GetExpressionInfo(unaryExpression.Operand);
            }

            var methodCallExpression = expression as MethodCallExpression;
            if (methodCallExpression != null)
            {
                /*
                if (methodCallExpression.Method.Name == "GetType")
                    return ClassMember(methodCallExpression.Object);
                */
                if (methodCallExpression.Method.Name == "get_Item")
                {
                    fullPath = GetExpressionInfo(methodCallExpression.Object, expressionInfo);
                    expressionInfo.FullPath = fullPath;
                    return fullPath;
                    //return GetExpressionInfo(methodCallExpression.Object);
                }

                if (methodCallExpression.Method.Name == "First")
                {
                    fullPath = GetExpressionInfo(methodCallExpression.Arguments[0], expressionInfo);
                    expressionInfo.FullPath = fullPath;
                    return fullPath;
                    //return GetExpressionInfo(methodCallExpression.Arguments[0]);
                }

                throw new Exception("Unrecognised method call in expression " + methodCallExpression);
            }

            if (expression is ParameterExpression)
                return "";

            throw new Exception("Could not determine member from " + expression);
        }

        private static bool IsConversion(ExpressionType expressionType)
        {
            if (expressionType != ExpressionType.Convert)
            {
                return (bool)(expressionType == ExpressionType.ConvertChecked);
            }
            return true;
        }
        /*
        private static string ClassMember(Expression expression, SessionFactoryInfo sessionFactoryInfo)
        {
            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                return (GetExpressionInfo(expression, sessionFactoryInfo) + ".class");
            }
            return "class";
        }*/
    }
}
