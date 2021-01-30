using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate.Extensions.Internal;

namespace NHibernate.Extensions
{
    public class QueryRelationTree
    {
        public QueryRelationTree()
        {
            Node = new QueryRelationNode();
        }

        private QueryRelationNode Node { get; set; }

        public void AddNode<T>(Expression<Func<T, object>> expression)
        {
            var fullPath = ExpressionHelper.GetFullPath(expression.Body);
            Node.Add(fullPath);
        }

        public void AddNode(string path)
        {
            Node.Add(path);
        }

        public Dictionary<int, List<string>> DeepFirstSearch()
        {
            var result = new Dictionary<int, List<string>>();
            var idx = 0;
            DeepFirstSearchRecursive(Node.Children, result, ref idx);
            return result;
        }

        public List<string> GetLeafs()
        {
            return DeepFirstSearch().Select(pair => pair.Value.Last()).ToList();
        } 

        private static void DeepFirstSearchRecursive(Dictionary<string, QueryRelationNode> children, Dictionary<int, List<string>> result, ref int idx)
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
                    var lst = result[idx].Last() + ".";
                    if (node.FullPath.StartsWith(lst))
                    {
                        result[idx].Add(node.FullPath);
                    }
                    else
                    {
                        idx++;
                        result.Add(idx, new List<string>());
                        var relPaths = node.FullPath.Split('.');
                        for (var i = 1; i <= relPaths.Length; i++)
                        {
                            result[idx].Add(string.Join(".", relPaths.Take(i)));
                        }
                    }
                        
                }
                DeepFirstSearchRecursive(node.Children, result, ref idx);
            }
        }
    }
}
