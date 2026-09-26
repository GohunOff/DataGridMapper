using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MC.Data.DataGrid.Filter
{
    public sealed class EnumerableDataSourceAdapter<T> : I.IDataSourceAdapter
    {
        private readonly IEnumerable<T> _source;
        private readonly FilterExpressionBuilder _expressionBuilder = new FilterExpressionBuilder();
        public Type ItemType
        {
            get { return typeof(T); }
        }

        public EnumerableDataSourceAdapter(
            IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            _source = source;
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

            var predicate =
                expression.Compile();

            return _source.Where(predicate);
        }

        private void ApplyFilter(FilterContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            var filters =
                context.Filters.Values.ToList();

            var result =
                context.DataSource.ApplyFilters(
                    filters);

            context.BindingSource.DataSource =
                result;

            context.BindingSource.ResetBindings(false);
        }
    }
}
