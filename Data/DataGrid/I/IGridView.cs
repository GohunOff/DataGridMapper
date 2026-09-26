using System.Windows.Forms;

namespace MC.Data.DataGrid.I
{
    public interface IGridView<T>
    {
        Control Control { get; }
        void SetData(T data);
    }
}
