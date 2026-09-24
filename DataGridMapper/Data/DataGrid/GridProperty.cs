using MC.Data.DataGrid.CustomColumn;
using MC.Data.DataGrid.I;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace MC.Data.DataGrid
{
    public class GridProperty
    {
        private readonly DataGridView grid;
        private readonly ControlRenderHost ancherRenderHost;
        private IGridActionRegistry actionRegistry;
        private Type configuredModelType;
        private DataGridViewCellEventHandler buttonClickHandler;

        public GridProperty(DataGridView grid, ControlRenderHost ancherRenderHost = null)
        {
            this.grid = grid
                ?? throw new ArgumentNullException(nameof(grid));

            this.ancherRenderHost = ancherRenderHost;
        }

        private interface IGridActionRegistry
        {
            Type ModelType { get; }

            bool Contains(string key);
        }
        private sealed class GridActionRegistry<T> : IGridActionRegistry
            where T : class
        {
            private readonly Dictionary<string, GridAction<T>> _actions;

            public Type ModelType
            {
                get { return typeof(T); }
            }

            public GridActionRegistry()
            {
                _actions =
                    new Dictionary<string, GridAction<T>>(
                        StringComparer.OrdinalIgnoreCase);
            }

            public bool Contains(string key)
            {
                return !string.IsNullOrWhiteSpace(key)
                    && _actions.ContainsKey(key);
            }

            public bool TryGet(
                string key,
                out GridAction<T> action)
            {
                return _actions.TryGetValue(
                    key,
                    out action);
            }

            public GridAction<T> Get(string key)
            {
                GridAction<T> action;

                if (!_actions.TryGetValue(
                        key,
                        out action))
                {
                    throw new InvalidOperationException(
                        $"Action '{key}' is not registered.");
                }

                return action;
            }

            public GridAction<T> Register(string key)
            {
                GridAction<T> action;

                if (_actions.TryGetValue(
                        key,
                        out action))
                {
                    return action;
                }

                action = new GridAction<T>(key);

                _actions.Add(
                    key,
                    action);

                return action;
            }
        }

        /// <summary>
        /// Initializes a standard WinForms DataGridView
        /// based on GridViewAttribute attributes.
        /// </summary>
        public bool InitData<T>()
            where T : class
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));

            if (configuredModelType != null &&
                configuredModelType != typeof(T))
            {
                RemoveButtonActions();

                actionRegistry = null;
            }

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
            bool hasButtonColumns = false;
            grid.SuspendLayout();
            try
            {
                ConfigureGrid();

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

                    if (attribute.ColumnType ==
                        GridViewAttribute.eColumnType.Button)
                    {
                        RegisterAction<T>(
                            attribute.ActionName);

                        ValidateAction<T>(
                            attribute.ActionName);
                        hasButtonColumns = true;
                    }

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
                if (hasButtonColumns)
                    ConfigureButtonActions<T>();

                configuredModelType = typeof(T);

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
        public GridAction<T> GetAction<T>(
            string key)
            where T : class
        {
            GridActionRegistry<T> registry =
                ValidateAction<T>(key);

            return registry.Get(key.Trim());
        }

        private void ValidateColumnType(
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

            // ActionName ma sens wyłącznie dla Button.
            if (attribute.ColumnType !=
                GridViewAttribute.eColumnType.Button &&
                !string.IsNullOrWhiteSpace(attribute.ActionName))
            {
                throw new InvalidOperationException(
                    $"Property '{property.DeclaringType?.FullName}.{property.Name}' " +
                    $"has ActionName '{attribute.ActionName}', " +
                    $"but ColumnType is '{attribute.ColumnType}'. " +
                    "ActionName can only be used with ColumnType.Button.");
            }

            switch (attribute.ColumnType)
            {
                case GridViewAttribute.eColumnType.None:
                case GridViewAttribute.eColumnType.Text:
                case GridViewAttribute.eColumnType.Tooltip:
                case GridViewAttribute.eColumnType.MemoEdit:
                    break;
                case GridViewAttribute.eColumnType.Button:
                    if (string.IsNullOrWhiteSpace(attribute.ActionName))
                    {
                        throw new InvalidOperationException(
                           $"Property '{property.DeclaringType?.FullName}.{property.Name}' " +
                           "has ColumnType.Button but ActionName has not been specified. " +
                           "Use GridProperty.InitAction to initialize the action.");
                    }
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
        private void ValidateCustomColumnType(
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
        private GridActionRegistry<T> ValidateAction<T>(
            string actionName)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(actionName))
                throw new ArgumentException(
                    "ActionName cannot be empty.",
                    nameof(actionName));

            string actionKey = actionName.Trim();

            if (actionRegistry == null)
            {
                throw new InvalidOperationException(
                    "No action registry has been configured.");
            }

            if (actionRegistry.ModelType != typeof(T))
            {
                throw new InvalidOperationException(
                    $"DataGridView is configured for model " +
                    $"'{actionRegistry.ModelType.FullName}', " +
                    $"but action '{actionKey}' expects model " +
                    $"'{typeof(T).FullName}'.");
            }

            GridActionRegistry<T> registry =
                actionRegistry as GridActionRegistry<T>;

            if (registry == null)
            {
                throw new InvalidOperationException(
                    $"Invalid GridActionRegistry type for model " +
                    $"'{typeof(T).FullName}'.");
            }

            if (!registry.Contains(actionKey))
            {
                throw new InvalidOperationException(
                    $"Action '{actionKey}' is not registered for model " +
                    $"'{typeof(T).FullName}'.");
            }

            return registry;
        }

        private void ConfigureButtonActions<T>()
            where T : class
        {
            RemoveButtonActions();

            buttonClickHandler =
                Grid_CellContentClick<T>;

            grid.CellContentClick +=
                buttonClickHandler;
        }
        private void Grid_CellContentClick<T>(
            object sender,
            DataGridViewCellEventArgs e)
            where T : class
        {
            DataGridView grid =
                sender as DataGridView;

            if (grid == null)
                return;

            if (e.RowIndex < 0 ||
                e.ColumnIndex < 0)
                return;

            if (e.ColumnIndex >= grid.Columns.Count)
                return;

            DataGridViewColumn column =
                grid.Columns[e.ColumnIndex];

            if (!(column is DataGridViewButtonColumn))
                return;

            GridViewAttribute attribute =
                GetAttribute(column);

            if (attribute == null)
                return;

            if (attribute.ColumnType !=
                GridViewAttribute.eColumnType.Button)
                return;

            if (string.IsNullOrWhiteSpace(
                    attribute.ActionName))
                return;

            if (e.RowIndex >= grid.Rows.Count)
                return;

            T item =
                grid.Rows[e.RowIndex].DataBoundItem as T;

            if (item == null)
                return;

            // Waliduje:
            // 1. grid
            // 2. model T
            // 3. registry
            // 4. ActionName
            GridActionRegistry<T> registry =
                ValidateAction<T>(
                    attribute.ActionName);

            GridAction<T> action =
                registry.Get(
                    attribute.ActionName);

            action.Raise(
                item,
                grid,
                e.RowIndex,
                e.ColumnIndex);
        }
        private void RegisterAction<T>(
            string actionName)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(actionName))
                throw new ArgumentException(
                    "ActionName cannot be empty.",
                    nameof(actionName));

            string actionKey = actionName.Trim();

            if (actionRegistry == null)
            {
                GridActionRegistry<T> newRegistry =
                    new GridActionRegistry<T>();

                newRegistry.Register(actionKey);

                actionRegistry = newRegistry;

                return;
            }

            if (actionRegistry.ModelType != typeof(T))
            {
                throw new InvalidOperationException(
                    $"DataGridView is already configured for " +
                    $"model '{actionRegistry.ModelType.FullName}', " +
                    $"but '{typeof(T).FullName}' was requested.");
            }

            GridActionRegistry<T> registry =
                actionRegistry as GridActionRegistry<T>;

            if (registry == null)
            {
                throw new InvalidOperationException(
                    "Invalid GridActionRegistry type.");
            }

            registry.Register(actionKey);
        }
        private void RemoveButtonActions()
        {
            if (buttonClickHandler == null)
                return;

            grid.CellContentClick -=
                buttonClickHandler;

            buttonClickHandler = null;
        }

        private string GetFormat(
            GridViewAttribute attribute,
            string defaultFormat)
        {
            return string.IsNullOrWhiteSpace(attribute.Format)
                ? defaultFormat
                : attribute.Format;
        }

        private bool IsNumericType(Type type)
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
        private void ConfigureGrid()
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
        private DataGridViewColumn CreateColumn(
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
        private void ConfigureColumn(
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
                    if (property.PropertyType == typeof(TimeSpan) ||
                        Nullable.GetUnderlyingType(property.PropertyType) == typeof(TimeSpan))
                    {
                        column.DefaultCellStyle.Format =
                            GetFormat(attribute, @"hh\:mm\:ss");
                    }
                    else
                    {
                        column.DefaultCellStyle.Format =
                            GetFormat(attribute, "HH:mm:ss");
                    }
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
        private void ConfigureToolTips(DataGridView grid)
        {
            grid.CellToolTipTextNeeded -= Grid_CellToolTipTextNeeded;
            grid.CellToolTipTextNeeded += Grid_CellToolTipTextNeeded;
        }
        private void Grid_CellToolTipTextNeeded(
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
        private GridViewAttribute GetAttribute(
            DataGridViewColumn column)
        {
            return column?.Tag as GridViewAttribute;
        }
        private DataGridViewColumn CreateCustomColumn(
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
