using MC.Data.DataGrid.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MC.Data.DataGrid.Metadata
{
    public sealed class FilterMetadata
    {
        public bool CanFilter { get; }

        public IReadOnlyCollection<FilterOperator> SupportedOperators { get; }

        public Type ValueType { get; }
    }
}
