using MC.Data.DataGrid.I;
using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace MC.Data.DataGrid.CustomColumn
{
    public class CustomColumn<TData, TView> :
        DataGridViewColumn,
        IGridPropertyMetadataProvider
        where TView : Control, I.IGridView<TData>, new()
        where TData : I.ICustomColumnData
    {
        public ControlRenderHost RenderHost { get; private set; }

        public TView View { get; private set; }

        public MC.Data.DataGrid.BitmapCache.BitmapCache Cache
        {
            get;
            private set;
        }

        public Func<TData, object> KeySelector { get; private set; }

        public GridPropertyMetadata PropertyMetadata
        {
            get;
            private set;
        }

        public CustomColumn(
            ControlRenderHost renderHost,
            MC.Data.DataGrid.BitmapCache.BitmapCache bitmapCache,
            string propertyName)
            : base(new CustomCell<TData, TView>())
        {
            if (renderHost == null)
                throw new ArgumentNullException("renderHost");

            if (bitmapCache == null)
                throw new ArgumentNullException("bitmapCache");

            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException(
                    "propertyName nie może być pusty.",
                    "propertyName");

            RenderHost = renderHost;
            Cache = bitmapCache;

            // One shared view instance
            View = new TView();

            KeySelector = delegate (TData value)
            {
                return value;
            };

            PropertyInfo property =
                typeof(TData).GetProperty(
                    propertyName,
                    BindingFlags.Public |
                    BindingFlags.Instance);

            if (property == null)
            {
                throw new ArgumentException(
                    "Typ " +
                    typeof(TData).Name +
                    " nie posiada właściwości '" +
                    propertyName +
                    "'.",
                    "propertyName");
            }

            GridViewAttribute attribute =
                property
                    .GetCustomAttributes(
                        typeof(GridViewAttribute),
                        true)
                    .OfType<GridViewAttribute>()
                    .FirstOrDefault();

            PropertyMetadata =
                new GridPropertyMetadata(
                    property,
                    attribute);

            DataPropertyName =
                PropertyMetadata.PropertyName;
        }

        public override object Clone()
        {
            CustomColumn<TData, TView> clone =
                (CustomColumn<TData, TView>)base.Clone();

            clone.RenderHost =
                RenderHost;

            clone.View =
                View;

            clone.Cache =
                Cache;

            clone.KeySelector =
                KeySelector;

            clone.PropertyMetadata =
                PropertyMetadata;

            return clone;
        }
    }
}
