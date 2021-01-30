using System;
using System.Collections.Generic;
using System.Linq;

namespace NHibernate.Extensions
{
    public class QueryRelationNode
    {
        public QueryRelationNode()
        {
            Children = new Dictionary<string, QueryRelationNode>();
        }

        public string FullPath
        {
            get { return GetFullPath(this); }
        }

        public string GetFullPath(QueryRelationNode node)
        {
            if (node.Parent == null) return "";
            var fPath = GetFullPath(node.Parent);
            return string.IsNullOrEmpty(fPath) ? node.Path : String.Format("{0}.{1}", fPath, node.Path);
        }

        public string Path { get; private set; }

        public bool IsLeaf { get { return Children.Count == 0; } }

        public QueryRelationNode Parent { get; set; }

        public Dictionary<string, QueryRelationNode> Children { get; private set; }

        public void Add(string fullPath)
        {
            var paths = fullPath.Split('.');
            var path = paths.First();
            var key = Children.Keys.FirstOrDefault(o => o == path);
            if (key == null)
            {
                Children.Add(path, new QueryRelationNode { Path = path, Parent= this });
            }
            if (paths.Length > 1)
                Children[path].Add(string.Join(".", paths.Skip(1)));
        }
    }
}
