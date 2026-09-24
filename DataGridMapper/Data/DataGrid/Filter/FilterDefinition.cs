using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MC.Data.DataGrid.Filter
{
    public sealed class FilterDefinition
    {
        public string PropertyName { get; }

        public FilterOperator Operator { get; }

        public object Value { get; }

        public FilterDefinition(
            string propertyName,
            FilterOperator @operator,
            object value = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException(
                    "Property name cannot be empty.",
                    nameof(propertyName));

            PropertyName = propertyName;
            Operator = @operator;
            Value = value;
        }
    }
}
