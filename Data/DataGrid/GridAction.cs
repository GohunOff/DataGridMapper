using System;
using System.Windows.Forms;

namespace MC.Data.DataGrid
{
    public sealed class GridAction<T>
     where T : class
    {
        public string Key { get; }

        public event EventHandler<GridActionEventArgs<T>> Click;

        internal GridAction(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(
                    "Action key cannot be empty.",
                    nameof(key));

            Key = key;
        }

        internal void Raise(
            T item,
            DataGridView grid,
            int rowIndex,
            int columnIndex)
        {
            Click?.Invoke(
                this,
                new GridActionEventArgs<T>(
                    item,
                    grid,
                    rowIndex,
                    columnIndex));
        }
    }
}
