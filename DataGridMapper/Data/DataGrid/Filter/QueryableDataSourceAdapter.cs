using System;
using System.Collections.Generic;
using System.Linq;

namespace MC.Data.DataGrid.Filter
{
    public sealed class QueryableDataSourceAdapter<T> : I.IDataSourceAdapter
    {
        private readonly IQueryable<T> _source;
        private readonly FilterExpressionBuilder _expressionBuilder;

        public Type ItemType
        {
            get { return typeof(T); }
        }

        public QueryableDataSourceAdapter(
            IQueryable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            _source = source;
            _expressionBuilder =
                new FilterExpressionBuilder();
        }

        public object ApplyFilters(
            IReadOnlyCollection<FilterDefinition> filters)
        {
            if (filters == null)
                throw new ArgumentNullException("filters");

            if (filters.Count == 0)
                return _source;

            var expression =
                _expressionBuilder.Build<T>(filters);

            return _source.Where(expression);
        }
    }
}
