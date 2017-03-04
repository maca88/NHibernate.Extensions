using System;
using System.Linq.Expressions;
using NHibernate.Util;

namespace NHibernate.Extensions.Internal
{
    internal static class ExpressionHelper
    {
        public static string GetFullPath(Expression expression)
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
                            fullPath = GetFullPath(memberExpression.Expression);
                            return fullPath;
                        }
                    }
                    fullPath = GetFullPath(memberExpression.Expression) + "." + memberExpression.Member.Name;
                    return fullPath;
                }
                if (IsConversion(memberExpression.Expression.NodeType))
                {
                    fullPath = (GetFullPath(memberExpression.Expression) + "." + memberExpression.Member.Name).TrimStart('.'); ;
                    return fullPath;
                }

                fullPath = memberExpression.Member.Name;
                return fullPath;
            }

            var unaryExpression = expression as UnaryExpression;
            if (unaryExpression != null)
            {
                if (!IsConversion(unaryExpression.NodeType))
                    throw new Exception("Cannot interpret member from " + expression);
                fullPath = GetFullPath(unaryExpression.Operand);
                return fullPath;
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
                    fullPath = GetFullPath(methodCallExpression.Object);
                    return fullPath;
                }

                if (methodCallExpression.Method.Name == "First")
                {
                    fullPath = GetFullPath(methodCallExpression.Arguments[0]);
                    return fullPath;
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
                return (GetFullPath(expression, sessionFactoryInfo) + ".class");
            }
            return "class";
        }*/
    }
}
