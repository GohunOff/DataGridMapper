using System;
using System.Windows.Forms;

namespace MC.Data.DataGrid.Filter
{
    public static class BindingSourceResolver
    {
        public static object Resolve(
            BindingSource bindingSource)
        {
            if (bindingSource == null)
                throw new ArgumentNullException(
                    "bindingSource");

            object source =
                bindingSource.DataSource;

            while (source is BindingSource)
            {
                source =
                    ((BindingSource)source).DataSource;
            }

            return source;
        }
    }
}
