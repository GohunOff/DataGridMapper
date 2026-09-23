using System;
using System.Drawing;
using System.Windows.Forms;

namespace MC.Data.DataGrid.CustomColumn
{
    public class CustomColumn<TData, TView> : DataGridViewColumn
        where TView : Control, I.IGridView<TData>, new()
         where TData : I.ICustomColumnData
    {
        public ControlRenderHost RenderHost { get; private set; }

        public TView View { get; private set; }

        public MC.Data.DataGrid.BitmapCache.BitmapCache Cache { get; private set; }

        public Func<TData, object> KeySelector { get; private set; }

        public CustomColumn(
            ControlRenderHost renderHost,
            MC.Data.DataGrid.BitmapCache.BitmapCache bitmapCache)
            : base(new CustomCell<TData, TView>())
        {
            if (renderHost == null)
                throw new ArgumentNullException(nameof(renderHost));

            if (bitmapCache == null)
                throw new ArgumentNullException(nameof(bitmapCache));

            RenderHost = renderHost;
            Cache = bitmapCache;

            // One shared view instance
            View = new TView();

            KeySelector = delegate (TData value)
            {
                return value;
            };
        }

        public override object Clone()
        {
            CustomColumn<TData, TView> clone =
                (CustomColumn<TData, TView>)base.Clone();

            clone.RenderHost = RenderHost;
            clone.View = View;
            clone.Cache = Cache;
            clone.KeySelector = KeySelector;

            return clone;
        }
    }
}
