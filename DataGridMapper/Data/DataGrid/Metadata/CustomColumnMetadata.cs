using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MC.Data.DataGrid.Metadata
{
    public sealed class CustomColumnMetadata
    {
        public Type ViewType { get; }

        public Type DataType { get; }

        public bool UsesCache { get; }

        public bool RequiresRenderHost { get; }

        public string KeyPropertyName { get; }
    }
}
