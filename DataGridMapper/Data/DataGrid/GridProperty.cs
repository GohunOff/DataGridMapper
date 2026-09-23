using MC.Data.DataGrid.CustomColumn;
using MC.Data.DataGrid.I;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace MC.Data.DataGrid
{
    public static class GridProperty
    {
        [AttributeUsage(AttributeTargets.Property)]
        public class GridViewAttribute : Attribute
        {
            public enum eColumnType
            {
                None,
                Text,
                Number,
                Date,
                Time,
                DateTime,
                Button,
                Tooltip,
                MemoEdit,
                Boolean,
                CustomColumn
            }

            public string Name { get; set; }
            public bool Ignore { get; set; }
            public bool Visibility { get; set; } = true;
            public bool AllowEdit { get; set; }
            public string ColumnName { get; set; }
            public eColumnType ColumnType { get; set; }
            public string Format { get; set; }
            public Type CustomColumnType { get; set; }

            public GridViewAttribute(
                string name,
                bool ignore = false,
                bool visibility = true,
                bool allowEdit = false,
                string columnName = "",
                string format = "",
                eColumnType columnType = eColumnType.None)
            {
                Name = name;
                Ignore = ignore;
                Visibility = visibility;
                AllowEdit = allowEdit;
                ColumnName = columnName;
                ColumnType = columnType;
                Format = format;
            }
        }

        private static string GetFormat(
                    GridViewAttribute attribute,
                    string defaultFormat)
        {
            return string.IsNullOrWhiteSpace(attribute.Format)
                ? defaultFormat
                : attribute.Format;
        }

        /// <summary>
        /// Initializes a standard WinForms DataGridView
        /// based on GridViewAttribute attributes.
        /// </summary>
        public static bool InitData<T>(DataGridView grid, ControlRenderHost ancherRenderHost = null)
            where T : class
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));

            grid.Columns.Clear();

            PropertyInfo[] properties = typeof(T).GetProperties();

            bool hasGridAttributes = properties.Any(p =>
                p.GetCustomAttribute<GridViewAttribute>() != null);

            if (!hasGridAttributes)
            {
                MessageBox.Show(
                     $"Type '{typeof(T).FullName}' does not contain any properties " +
                     "marked with the GridViewAttribute.",
                     "DataGridView Configuration",
                     MessageBoxButtons.OK,
                     MessageBoxIcon.Warning);

                return false;
            }

            grid.SuspendLayout();
            try
            {
                ConfigureGrid(grid);

                int visibleIndex = 0;

                foreach (PropertyInfo property in properties)
                {
                    GridViewAttribute attribute =
                        property.GetCustomAttribute<GridViewAttribute>();

                    if (attribute == null)
                        continue;

                    if (attribute.Ignore)
                        continue;

                    ValidateColumnType(property, attribute);

                    string propertyName = property.Name;

                    DataGridViewColumn column =
                        grid.Columns.Cast<DataGridViewColumn>()
                            .FirstOrDefault(c =>
                                string.Equals(
                                    c.DataPropertyName,
                                    propertyName,
                                    StringComparison.OrdinalIgnoreCase));

                    if (column == null)
                    {
                        column = CreateColumn(property, attribute, ancherRenderHost);

                        if (column == null)
                            continue;

                        grid.Columns.Add(column);
                    }

                    column.Name = string.IsNullOrEmpty(attribute.ColumnName)
                        ? propertyName
                        : attribute.ColumnName;

                    column.DataPropertyName = propertyName;

                    column.HeaderText = string.IsNullOrEmpty(attribute.Name)
                        ? propertyName
                        : attribute.Name;

                    column.Tag = attribute;

                    column.Visible = attribute.Visibility;

                    if (attribute.Visibility)
                    {
                        column.DisplayIndex = visibleIndex;
                        visibleIndex++;
                    }

                    column.ReadOnly = !attribute.AllowEdit;

                    column.SortMode =
                        DataGridViewColumnSortMode.Automatic;

                    ConfigureColumn(
                        column,
                        property,
                        attribute);
                }

                ConfigureToolTips(grid);

                grid.AutoGenerateColumns = false;
                grid.AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill;

                grid.AllowUserToAddRows = false;
                grid.AllowUserToDeleteRows = false;
                grid.AllowUserToResizeRows = false;

                grid.RowHeadersVisible = false;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                     ex.Message,
                     "DataGridView Configuration Error",
                     MessageBoxButtons.OK,
                     MessageBoxIcon.Error);
                Debug.WriteLine(ex);
                throw;
            }
            finally
            {
                grid.ResumeLayout(true);
            }
        }

        private static void ValidateColumnType(
            PropertyInfo property,
            GridViewAttribute attribute)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (attribute == null)
                throw new ArgumentNullException(nameof(attribute));

            Type propertyType =
                Nullable.GetUnderlyingType(property.PropertyType)
                ?? property.PropertyType;

            switch (attribute.ColumnType)
            {
                case GridViewAttribute.eColumnType.None:
                case GridViewAttribute.eColumnType.Text:
                case GridViewAttribute.eColumnType.Tooltip:
                case GridViewAttribute.eColumnType.MemoEdit:
                case GridViewAttribute.eColumnType.Button:
                    // No type restriction.
                    break;
                case GridViewAttribute.eColumnType.CustomColumn:
                    ValidateCustomColumnType(property, attribute);
                    break;

                case GridViewAttribute.eColumnType.Boolean:

                    if (propertyType != typeof(bool))
                    {
                        throw new InvalidOperationException(
                            $"Property '{property.DeclaringType?.FullName}.{property.Name}' " +
                            $"has type '{property.PropertyType.FullName}', " +
                            "but ColumnType.Boolean requires type bool.");
                    }

                    break;

                case GridViewAttribute.eColumnType.Number:

                    if (!IsNumericType(propertyType))
                    {
                        throw new InvalidOperationException(
                            $"Property '{property.DeclaringType?.FullName}.{property.Name}' " +
                            $"has type '{property.PropertyType.FullName}', " +
                            "but ColumnType.Number requires a numeric type.");
                    }

                    break;

                case GridViewAttribute.eColumnType.Date:

                    if (propertyType != typeof(DateTime))
                    {
                        throw new InvalidOperationException(
                            $"Property '{property.DeclaringType?.FullName}.{property.Name}' " +
                            $"has type '{property.PropertyType.FullName}', " +
                            "but ColumnType.Date requires type DateTime.");
                    }

                    break;

                case GridViewAttribute.eColumnType.Time:

                    if (propertyType != typeof(TimeSpan) &&
                        propertyType != typeof(DateTime))
                    {
                        throw new InvalidOperationException(
                            $"Property '{property.DeclaringType?.FullName}.{property.Name}' " +
                            $"has type '{property.PropertyType.FullName}', " +
                            "but ColumnType.Time requires type TimeSpan or DateTime.");
                    }

                    break;

                case GridViewAttribute.eColumnType.DateTime:

                    if (propertyType != typeof(DateTime))
                    {
                        throw new InvalidOperationException(
                            $"Property '{property.DeclaringType?.FullName}.{property.Name}' " +
                            $"has type '{property.PropertyType.FullName}', " +
                            "but ColumnType.DateTime requires type DateTime.");
                    }

                    break;

                default:

                    throw new ArgumentOutOfRangeException(
                        nameof(attribute.ColumnType),
                        attribute.ColumnType,
                        $"Unsupported column type for property '{property.Name}'.");
            }
        }

        private static void ValidateCustomColumnType(
            PropertyInfo property,
            GridViewAttribute attribute)
        {
            if (attribute.CustomColumnType == null)
            {
                throw new InvalidOperationException(
                    $"CustomColumnType has not been specified for property '{property.Name}'.");
            }

            Type dataType =
                Nullable.GetUnderlyingType(property.PropertyType)
                ?? property.PropertyType;

            Type viewType =
                attribute.CustomColumnType;

            if (!typeof(Control).IsAssignableFrom(viewType))
            {
                throw new InvalidOperationException(
                    $"{viewType.FullName} must inherit from Control.");
            }

            Type gridViewInterface =
                typeof(IGridView<>).MakeGenericType(dataType);

            if (!gridViewInterface.IsAssignableFrom(viewType))
            {
                throw new InvalidOperationException(
                    $"{viewType.FullName} must implement " +
                    $"{gridViewInterface.FullName}.");
            }

            Type customColumnType =
                typeof(CustomColumn<,>).MakeGenericType(
                    dataType,
                    viewType);

            Type bitmapCacheType =
                typeof(MC.Data.DataGrid.BitmapCache.BitmapCache);

            ConstructorInfo constructor =
                customColumnType.GetConstructor(
                    new[]
                    {
                typeof(ControlRenderHost),
                bitmapCacheType
                    });

            if (constructor == null)
            {
                throw new InvalidOperationException(
                    $"Type '{customColumnType.FullName}' does not have the required constructor: " +
                    $"({typeof(ControlRenderHost).FullName}, {bitmapCacheType.FullName}).");
            }
        }


        private static bool IsNumericType(Type type)
        {
            return type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(int)
                || type == typeof(uint)
                || type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal);
        }

        /// <summary>
        /// Configures the basic properties of the DataGridView.
        /// </summary>
        private static void ConfigureGrid(DataGridView grid)
        {
            grid.AutoGenerateColumns = false;

            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;

            grid.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;

            grid.MultiSelect = false;

            grid.RowHeadersVisible = false;

            grid.AutoSizeRowsMode =
                DataGridViewAutoSizeRowsMode.None;

            grid.RowTemplate.Height = 80;

            grid.DefaultCellStyle.WrapMode =
                DataGridViewTriState.True;

            grid.ColumnHeadersDefaultCellStyle.WrapMode =
                DataGridViewTriState.True;

            grid.EnableHeadersVisualStyles = true;
        }

        /// <summary>
        /// Creates the appropriate DataGridView column type.
        /// </summary>
        private static DataGridViewColumn CreateColumn(
            PropertyInfo property,
            GridViewAttribute attribute, 
            ControlRenderHost renderHost = null)
        {
            switch (attribute.ColumnType)
            {
                case GridViewAttribute.eColumnType.Button:
                    return new DataGridViewButtonColumn();

                case GridViewAttribute.eColumnType.Boolean:
                    return new DataGridViewCheckBoxColumn();

                case GridViewAttribute.eColumnType.Number:
                    return new DataGridViewTextBoxColumn();

                case GridViewAttribute.eColumnType.Date:
                case GridViewAttribute.eColumnType.Time:
                case GridViewAttribute.eColumnType.DateTime:
                    return new DataGridViewTextBoxColumn();

                case GridViewAttribute.eColumnType.MemoEdit:
                    return new DataGridViewTextBoxColumn();
                case GridViewAttribute.eColumnType.CustomColumn:
                    {
                        return CreateCustomColumn(
                            property,
                            attribute,
                            renderHost);
                    }

                case GridViewAttribute.eColumnType.Tooltip:
                case GridViewAttribute.eColumnType.Text:
                case GridViewAttribute.eColumnType.None:
                default:
                    return new DataGridViewTextBoxColumn();
            }
        }

        /// <summary>
        /// Configures a specific column.
        /// </summary>
        private static void ConfigureColumn(
            DataGridViewColumn column,
            PropertyInfo property,
            GridViewAttribute attribute)
        {
            column.ReadOnly = !attribute.AllowEdit;

            column.DefaultCellStyle.WrapMode =
                DataGridViewTriState.True;

            switch (attribute.ColumnType)
            {
                case GridViewAttribute.eColumnType.Date:
                    column.DefaultCellStyle.Format =
                        GetFormat(attribute, "dd.MM.yyyy");
                    break;

                case GridViewAttribute.eColumnType.Time:
                    column.DefaultCellStyle.Format =
                        GetFormat(attribute, "HH:mm:ss");
                    break;

                case GridViewAttribute.eColumnType.DateTime:
                    column.DefaultCellStyle.Format =
                        GetFormat(attribute, "dd.MM.yyyy HH:mm:ss");
                    break;

                case GridViewAttribute.eColumnType.Number:
                    column.DefaultCellStyle.Format =
                        GetFormat(attribute, "N2");

                    column.DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleRight;

                    break;

                case GridViewAttribute.eColumnType.Boolean:
                    column.DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleCenter;

                    break;

                case GridViewAttribute.eColumnType.MemoEdit:
                    column.DefaultCellStyle.WrapMode =
                        DataGridViewTriState.True;

                    break;

                case GridViewAttribute.eColumnType.Button:
                    DataGridViewButtonColumn buttonColumn =
                        column as DataGridViewButtonColumn;

                    if (buttonColumn != null)
                    {
                        buttonColumn.UseColumnTextForButtonValue = true;
                        buttonColumn.Text =
                            string.IsNullOrEmpty(attribute.Name)
                                ? property.Name
                                : attribute.Name;
                    }

                    break;     
            }

        }

        /// <summary>
        /// Enables tooltip support.
        /// </summary>
        private static void ConfigureToolTips(DataGridView grid)
        {
            grid.CellToolTipTextNeeded -= Grid_CellToolTipTextNeeded;
            grid.CellToolTipTextNeeded += Grid_CellToolTipTextNeeded;
        }

        private static void Grid_CellToolTipTextNeeded(
            object sender,
            DataGridViewCellToolTipTextNeededEventArgs e)
        {
            DataGridView grid = sender as DataGridView;

            if (grid == null)
                return;

            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            DataGridViewColumn column =
                grid.Columns[e.ColumnIndex];

            GridViewAttribute attribute =
                GetAttribute(column);

            if (attribute == null)
                return;

            if (attribute.ColumnType !=
                GridViewAttribute.eColumnType.Tooltip)
                return;

            object value =
                grid.Rows[e.RowIndex]
                    .Cells[e.ColumnIndex]
                    .Value;

            e.ToolTipText =
                value == null
                    ? string.Empty
                    : Convert.ToString(value);
        }

        /// <summary>
        /// Gets the attribute assigned to the column.
        /// </summary>
        private static GridViewAttribute GetAttribute(
            DataGridViewColumn column)
        {
            return column?.Tag as GridViewAttribute;
        }

        private static DataGridViewColumn CreateCustomColumn(
            PropertyInfo property,
            GridViewAttribute attribute,
            ControlRenderHost renderHost)
        {
            if (renderHost == null)
            {
                throw new InvalidOperationException(
                    "RenderHost is required for CustomColumn.");
            }

            Type dataType =
                Nullable.GetUnderlyingType(property.PropertyType)
                ?? property.PropertyType;

            Type viewType =
                attribute.CustomColumnType;

            Type customColumnType =
                typeof(CustomColumn<,>)
                    .MakeGenericType(
                        dataType,
                        viewType);

            Type bitmapCacheType =
                typeof(MC.Data.DataGrid.BitmapCache.BitmapCache);

            ConstructorInfo constructor =
                customColumnType.GetConstructor(
                    new[]
                    {
                typeof(ControlRenderHost),
                bitmapCacheType
                    });

            if (constructor == null)
            {
                throw new InvalidOperationException(
                    $"Type '{customColumnType.FullName}' does not have the required constructor: " +
                    $"({typeof(ControlRenderHost).FullName}, {bitmapCacheType.FullName}).");
            }

            return (DataGridViewColumn)constructor.Invoke(
                new object[]
                {
            renderHost,
            new MC.Data.DataGrid.BitmapCache.BitmapCache(20)
                });
        }
    }
}
