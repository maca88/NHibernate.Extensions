using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using NHibernate.Metadata;
using NHibernate.Persister.Collection;
using NHibernate.Persister.Entity;

namespace NHibernate.Extensions.Linq
{
    internal class ExpressionInfo
    {
        private readonly Expression _originalQuery;
        private string _currentPath;
        private readonly Dictionary<string, int> _includedPaths = new Dictionary<string, int>();

        public ExpressionInfo(Expression expression, IClassMetadata metadata)
        {
            _originalQuery = expression;
            Expression = expression;
            Metadata = metadata;
            TotalColumns = GetTotalColumns(metadata);
        }

        public IClassMetadata Metadata { get; }

        public int TotalColumns { get; private set; }

        public ExpressionInfo Next { get; private set; }

        public ExpressionInfo Previous { get; private set; }

        public Expression Expression { get; set; }

        public string CollectionPath { get; set; }

        public bool IsExpressionModified => Expression != _originalQuery;

        public ExpressionInfo GetFirst()
        {
            var current = this;
            while (current.Previous != null)
            {
                current = current.Previous;
            }
            return current;
        }

        public string AddIncludedProperty(string propertyName, IClassMetadata propertyTypeMetadata, IQueryableCollection collectionMetadata, bool root)
        {
            if (root)
            {
                _currentPath = propertyName;
            }
            else if (!string.IsNullOrEmpty(_currentPath))
            {
                _currentPath = $"{_currentPath}.{propertyName}";
            }

            if (_includedPaths.ContainsKey(_currentPath))
            {
                return null;
            }

            var columns = propertyTypeMetadata == null
                ? GetTotalColumns(collectionMetadata)
                : GetTotalColumns(propertyTypeMetadata);

            _includedPaths.Add(_currentPath, columns);
            TotalColumns += columns;

            return _currentPath;
        }

        public void RemoveIncludedProperty(string includedPath)
        {
            TotalColumns -= _includedPaths[includedPath];
            _includedPaths.Remove(includedPath);
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
            var next = new ExpressionInfo(_originalQuery, Metadata);
            Next = next;
            next.Previous = this;
            return next;
        }

        private static int GetTotalColumns(IClassMetadata metadata)
        {
            var entityPersister = (AbstractEntityPersister)metadata;
            var totalColumns = entityPersister.IdentifierColumnNames.Length;
            for (int i = 0, length = metadata.PropertyNames.Length; i < length; i++)
            {
                totalColumns += entityPersister.GetPropertyColumnNames(i).Length;
            }

            return totalColumns;
        }

        private static int GetTotalColumns(IQueryableCollection collection)
        {
            return collection.ElementColumnNames.Length +
                   collection.KeyColumnNames.Length +
                   collection.IndexColumnNames.Length;
        }

    }
}
