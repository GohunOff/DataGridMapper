using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static MC.Data.DataGrid.GridFilter;

namespace MC.Data.DataGrid.Filter
{
    public sealed class FilterContext
    {
        public DataGridView Grid { get; set; }

        public BindingSource BindingSource { get; set; }

        public object OriginalDataSource { get; set; }

        public bool OwnsBindingSource { get; set; }

        public I.IDataSourceAdapter DataSource { get; set; }

        public Panel FilterPanel { get; set; }

        public Dictionary<string, FilterDefinition> Filters
        {
            get;
            private set;
        }
        public Dictionary<int, FilterEditor> Controls
        {
            get;
            private set;
        }

        public FilterContext()
        {
            Filters =
                new Dictionary<string, FilterDefinition>(
                    StringComparer.OrdinalIgnoreCase);

            Controls =
                new Dictionary<int, FilterEditor>();
        }
    }
}
