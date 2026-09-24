using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MC.Data.DataGrid
{
    public class GridActionEventArgs<T> : EventArgs
      where T : class
    {
        public T Item { get; }

        public DataGridView Grid { get; }

        public int RowIndex { get; }

        public int ColumnIndex { get; }

        public GridActionEventArgs(
            T item,
            DataGridView grid,
            int rowIndex,
            int columnIndex)
        {
            Item = item;
            Grid = grid;
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
        }
    }
}
