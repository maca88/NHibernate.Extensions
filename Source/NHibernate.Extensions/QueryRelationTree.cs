using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate.Extensions.Expressions;
using NHibernate.Extensions.Helpers;

namespace NHibernate.Extensions
{
    public class QueryRelationTree<T>
    {
        public QueryRelationTree()
        {
            Node = new QueryRelationNode<T>();
            ExpressionInfo = new ExpressionInfo();
        }

        private QueryRelationNode<T> Node { get; set; }

        public ExpressionInfo ExpressionInfo { get; private set; }

        public void AddNode(Expression<Func<T, object>> expression)
        {
            ExpressionHelper.GetExpressionInfo(expression.Body, ExpressionInfo);
            Node.Add(ExpressionInfo.FullPath);
        }

        public Dictionary<int, List<string>> DeepFirstSearch()
        {
            var result = new Dictionary<int, List<string>>();
            var idx = 0;
            DeepFisrstSearchRecursive(Node.Children, result, ref idx);
            return result;
        }

        private static void DeepFisrstSearchRecursive(Dictionary<string, QueryRelationNode<T>> children, Dictionary<int, List<string>> result, ref int idx)
        {
            foreach (var child in children)
            {
                if (!result.ContainsKey(idx))
                    result.Add(idx, new List<string>());
                var node = child.Value;
                if (!result[idx].Any())
                {
                    result[idx].Add(node.FullPath);
                }
                else
                {
                    var lst = result[idx].Last();
                    if (node.FullPath.StartsWith(lst))
                        result[idx].Add(node.FullPath);
                    else
                    {
                        idx++;
                        result.Add(idx, new List<string>());
                        var relPaths = node.FullPath.Split('.').ToList();
                        for (var i = 1; i <= relPaths.Count; i++)
                        {
                            result[idx].Add(string.Join(".", relPaths.Take(i)));
                        }
                    }
                        
                }
                DeepFisrstSearchRecursive(node.Children, result, ref idx);
            }
        }
    }
}
