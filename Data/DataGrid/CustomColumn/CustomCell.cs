using MC.Data.DataGrid.BitmapCache;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace MC.Data.DataGrid.CustomColumn
{
    public class CustomCell<TData, TView> : DataGridViewCell
        where TView : Control, I.IGridView<TData>, new()
        where TData : I.ICustomColumnData
    {
        private CustomColumn<TData, TView> CustomColumn
        {
            get
            {
                return OwningColumn as CustomColumn<TData, TView>;
            }
        }

        public override Type FormattedValueType
        {
            get { return typeof(TData); }
        }

        protected override void Paint(
            Graphics graphics,
            Rectangle clipBounds,
            Rectangle cellBounds,
            int rowIndex,
            DataGridViewElementStates cellState,
            object value,
            object formattedValue,
            string errorText,
            DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            CustomColumn<TData, TView> column = CustomColumn;

            if (column == null)
            {
                base.Paint(
                    graphics,
                    clipBounds,
                    cellBounds,
                    rowIndex,
                    cellState,
                    value,
                    formattedValue,
                    errorText,
                    cellStyle,
                    advancedBorderStyle,
                    paintParts);

                return;
            }

            DataGridViewPaintParts parts =
                paintParts &
                ~DataGridViewPaintParts.ContentForeground;

            base.Paint(
                graphics,
                clipBounds,
                cellBounds,
                rowIndex,
                cellState,
                value,
                formattedValue,
                errorText,
                cellStyle,
                advancedBorderStyle,
                parts);

            if (cellBounds.Width <= 0 ||
                cellBounds.Height <= 0)
            {
                return;
            }

            if (!(value is TData))
            {
                return;
            }

            TData data = (TData)value;


            BitmapCacheKey key = new BitmapCacheKey(
                data.Id,
                cellBounds.Size,
                ImageFormat.Png,
                90L);

            Bitmap bitmap;

            if (!column.Cache.TryGet(key, out bitmap))
            {
                column.View.SetData(data);

                using (Bitmap rendered =
                    column.RenderHost.Render(
                        column.View,
                        cellBounds.Size))
                {
                    if (rendered == null)
                    {
                        return;
                    }

                    column.Cache.Set(
                        key,
                        rendered);
                }

                if (!column.Cache.TryGet(
                    key,
                    out bitmap))
                {
                    return;
                }
            }

            if (bitmap == null)
            {
                return;
            }

            using (bitmap)
            {
                graphics.DrawImage(
                    bitmap,
                    cellBounds);
            }
        }

        public override object Clone()
        {
            return (CustomCell<TData, TView>)base.Clone();
        }
    }
}
