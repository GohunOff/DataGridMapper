using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MC.Data.DataGrid.Filter.I
{
    public interface IDataSourceAdapter
    {
        Type ItemType { get; }

        object ApplyFilters(
            IReadOnlyCollection<FilterDefinition> filters);
    }
}
