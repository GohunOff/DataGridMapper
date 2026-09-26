using MC.Data.DataGrid.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MC.Data.DataGrid.I
{
    public interface IGridMetadataProvider
    {
        GridMetadata GetMetadata(Type modelType);
    }
}
