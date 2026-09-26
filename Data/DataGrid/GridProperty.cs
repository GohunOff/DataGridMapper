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
        private readonly GridPropertyMetadataProvider _metadataProvider;
        private readonly Dictionary<Type, IReadOnlyCollection<GridPropertyMetadata>>
            _metadataCache;

        private readonly DataGridView grid;
        private readonly ControlRenderHost ancherRenderHost;

        private Type configuredModelType;
        private IGridActionRegistry actionRegistry;

        private DataGridViewCellEventHandler buttonClickHandler;

        public GridProperty(DataGridView grid, ControlRenderHost ancherRenderHost = null)
        {
            this.grid = grid
                ?? throw new ArgumentNullException(nameof(grid));

            _metadataProvider =
                new GridPropertyMetadataProvider();

            _metadataCache =
                new Dictionary<Type, IReadOnlyCollection<GridPropertyMetadata>>();

            this.ancherRenderHost = ancherRenderHost;
        }

        private IReadOnlyCollection<GridPropertyMetadata>
            GetMetadata<T>()
        {
            IReadOnlyCollection<GridPropertyMetadata> metadata;

            if (_metadataCache.TryGetValue(typeof(T), out metadata))
                return metadata;

            metadata =
                _metadataProvider.GetMetadata(typeof(T));

            _metadataCache.Add(
                typeof(T),
                metadata);

            return metadata;
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
        private void ValidateConfiguration<T>()
    where T : class
        {
            Type modelType = typeof(T);

            IReadOnlyCollection<GridPropertyMetadata> metadata =
                GetMetadata<T>();

            bool hasGridColumns = false;

            HashSet<string> columnNames =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (GridPropertyMetadata item in metadata)
            {
                if (item.Attribute == null)
                    continue;

                if (item.Attribute.Ignore)
                    continue;

                hasGridColumns = true;

                ValidateColumnType(
                    item.Property,
                    item.Attribute);

                string columnName =
                    string.IsNullOrWhiteSpace(item.ColumnName)
                        ? item.PropertyName
                        : item.ColumnName.Trim();

                if (!columnNames.Add(columnName))
                {
                    throw new InvalidOperationException(
                        $"Model '{modelType.FullName}' contains duplicate " +
                        $"grid column name '{columnName}'. " +
                        $"Property '{item.PropertyName}' cannot use this name.");
                }

                if (item.ColumnType ==
                    GridViewAttribute.EColumnType.Button)
                {
                    ValidateButtonConfiguration(
                        item.Property,
                        item.Attribute);
                }
            }

            if (!hasGridColumns)
            {
                throw new InvalidOperationException(
                    $"Type '{modelType.FullName}' does not contain any " +
                    "active properties marked with the GridViewAttribute.");
            }
        }


        private void ValidateButtonConfiguration(
            PropertyInfo property,
            GridViewAttribute attribute)
        {
            if (attribute.ColumnType !=
                GridViewAttribute.EColumnType.Button)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(attribute.ActionName))
            {
                throw new InvalidOperationException(
                    $"Property '{property.DeclaringType?.FullName}.{property.Name}' " +
                    "is configured as Button but ActionName is empty.");
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

            try
            {
                ValidateConfiguration<T>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "DataGridView Configuration Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                Debug.WriteLine(ex);

                return false;
            }

            if (configuredModelType != null &&
                configuredModelType != typeof(T))
            {
                RemoveButtonActions();

                actionRegistry = null;
            }

            grid.Columns.Clear();

            IReadOnlyCollection<GridPropertyMetadata> metadata = GetMetadata<T>();

            bool hasButtonColumns = false;
            grid.SuspendLayout();
            try
            {
                ConfigureGrid();

                int visibleIndex = 0;

                foreach (GridPropertyMetadata item in metadata)
                {
                    if (item.Attribute == null)
                        continue;

                    if (item.Ignore)
                        continue;

                    if (item.ColumnType ==
                        GridViewAttribute.EColumnType.Button)
                    {
                        RegisterAction<T>(
                            item.Attribute.ActionName);
                        
                        hasButtonColumns = true;
                    }

                    string propertyName = item.PropertyName;

                    DataGridViewColumn column =
                        grid.Columns.Cast<DataGridViewColumn>()
                            .FirstOrDefault(c =>
                                string.Equals(
                                    c.DataPropertyName,
                                    propertyName,
                                    StringComparison.OrdinalIgnoreCase));

                    if (column == null)
                    {
                        column = CreateColumn(item, ancherRenderHost);

                        if (column == null)
                            continue;

                        grid.Columns.Add(column);
                    }

                    column.Name = string.IsNullOrEmpty(item.ColumnName)
                        ? propertyName
                        : item.ColumnName;

                    column.DataPropertyName = propertyName;

                    column.HeaderText = string.IsNullOrEmpty(item.Name)
                        ? propertyName
                        : item.Name;

                    column.Tag = item;

                    column.Visible = item.Attribute.Visibility;
                    if (item.Attribute.Visibility)
                    {
                        column.DisplayIndex = visibleIndex;
                        visibleIndex++;
                    }

                    column.ReadOnly = !item.Attribute.AllowEdit;

                    column.SortMode =
                        DataGridViewColumnSortMode.Automatic;

                    ConfigureColumn(
                        column,
                        item);
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
            return ValidateAction<T>(key)
                .Get(key.Trim());
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

            switch (attribute.ColumnType)
            {
                case GridViewAttribute.EColumnType.None:
                case GridViewAttribute.EColumnType.Text:
                case GridViewAttribute.EColumnType.Tooltip:
                case GridViewAttribute.EColumnType.MemoEdit:
                    break;
                case GridViewAttribute.EColumnType.Button:
                    break;
                case GridViewAttribute.EColumnType.CustomColumn:
                    ValidateCustomColumnType(property, attribute);
                    break;

                case GridViewAttribute.EColumnType.Boolean:

                    if (propertyType != typeof(bool))
                    {
                        throw new InvalidOperationException(
                            $"Property '{property.DeclaringType?.FullName}.{property.Name}' " +
                            $"has type '{property.PropertyType.FullName}', " +
                            "but ColumnType.Boolean requires type bool.");
                    }

                    break;

                case GridViewAttribute.EColumnType.Number:

                    if (!IsNumericType(propertyType))
                    {
                        throw new InvalidOperationException(
                            $"Property '{property.DeclaringType?.FullName}.{property.Name}' " +
                            $"has type '{property.PropertyType.FullName}', " +
                            "but ColumnType.Number requires a numeric type.");
                    }

                    break;

                case GridViewAttribute.EColumnType.Date:

                    if (propertyType != typeof(DateTime))
                    {
                        throw new InvalidOperationException(
                            $"Property '{property.DeclaringType?.FullName}.{property.Name}' " +
                            $"has type '{property.PropertyType.FullName}', " +
                            "but ColumnType.Date requires type DateTime.");
                    }

                    break;

                case GridViewAttribute.EColumnType.Time:

                    if (propertyType != typeof(TimeSpan) &&
                        propertyType != typeof(DateTime))
                    {
                        throw new InvalidOperationException(
                            $"Property '{property.DeclaringType?.FullName}.{property.Name}' " +
                            $"has type '{property.PropertyType.FullName}', " +
                            "but ColumnType.Time requires type TimeSpan or DateTime.");
                    }

                    break;

                case GridViewAttribute.EColumnType.DateTime:

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

            GridPropertyMetadata metadata =
                column.Tag as GridPropertyMetadata;

            if (metadata == null)
                return;

            if (metadata.ColumnType !=
                GridViewAttribute.EColumnType.Button)
                return;

            if (string.IsNullOrWhiteSpace(
                    metadata.ActionName))
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
                    metadata.ActionName);

            GridAction<T> action =
                registry.Get(
                    metadata.ActionName);
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
            GridPropertyMetadata metadata,
            string defaultFormat)
        {
            return string.IsNullOrWhiteSpace(metadata.Format)
                ? defaultFormat
                : metadata.Format;
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
           GridPropertyMetadata metadata,
                ControlRenderHost renderHost = null)
        {
            switch (metadata.ColumnType)
            {
                case GridViewAttribute.EColumnType.Button:
                    return new DataGridViewButtonColumn();

                case GridViewAttribute.EColumnType.Boolean:
                    return new DataGridViewCheckBoxColumn();

                case GridViewAttribute.EColumnType.Number:
                    return new DataGridViewTextBoxColumn();

                case GridViewAttribute.EColumnType.Date:
                case GridViewAttribute.EColumnType.Time:
                case GridViewAttribute.EColumnType.DateTime:
                    return new DataGridViewTextBoxColumn();

                case GridViewAttribute.EColumnType.MemoEdit:
                    return new DataGridViewTextBoxColumn();
                case GridViewAttribute.EColumnType.CustomColumn:
                    {
                        return CreateCustomColumn(
                            metadata,
                            renderHost);
                    }

                case GridViewAttribute.EColumnType.Tooltip:
                case GridViewAttribute.EColumnType.Text:
                case GridViewAttribute.EColumnType.None:
                default:
                    return new DataGridViewTextBoxColumn();
            }
        }
        /// <summary>
        /// Configures a specific column.
        /// </summary>
        private void ConfigureColumn(
            DataGridViewColumn column,
            GridPropertyMetadata metadata)
        {
            column.ReadOnly = !metadata.AllowEdit;

            column.DefaultCellStyle.WrapMode =
                DataGridViewTriState.True;

            switch (metadata.ColumnType)
            {
                case GridViewAttribute.EColumnType.Date:
                    column.DefaultCellStyle.Format =
                        GetFormat(metadata, "dd.MM.yyyy");
                    break;

                case GridViewAttribute.EColumnType.Time:
                    if (metadata.PropertyType == typeof(TimeSpan) ||
                        Nullable.GetUnderlyingType(metadata.PropertyType) == typeof(TimeSpan))
                    {
                        column.DefaultCellStyle.Format =
                            GetFormat(metadata, @"hh\:mm\:ss");
                    }
                    else
                    {
                        column.DefaultCellStyle.Format =
                            GetFormat(metadata, "HH:mm:ss");
                    }
                    break;

                case GridViewAttribute.EColumnType.DateTime:
                    column.DefaultCellStyle.Format =
                        GetFormat(metadata, "dd.MM.yyyy HH:mm:ss");
                    break;

                case GridViewAttribute.EColumnType.Number:
                    column.DefaultCellStyle.Format =
                        GetFormat(metadata, "N2");

                    column.DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleRight;

                    break;

                case GridViewAttribute.EColumnType.Boolean:
                    column.DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleCenter;

                    break;

                case GridViewAttribute.EColumnType.MemoEdit:
                    column.DefaultCellStyle.WrapMode =
                        DataGridViewTriState.True;

                    break;

                case GridViewAttribute.EColumnType.Button:
                    DataGridViewButtonColumn buttonColumn =
                        column as DataGridViewButtonColumn;

                    if (buttonColumn != null)
                    {
                        buttonColumn.UseColumnTextForButtonValue = true;
                        buttonColumn.Text =
                            string.IsNullOrEmpty(metadata.Name)
                                ? metadata.PropertyName
                                : metadata.Name;
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

            GridPropertyMetadata metadata =
                GetMetadata(column);

            if (metadata == null)
                return;

            if (metadata.ColumnType !=
                GridViewAttribute.EColumnType.Tooltip)
                return;
        }


        /// <summary>
        /// Gets the attribute assigned to the column.
        /// </summary>
        private GridPropertyMetadata GetMetadata(
         DataGridViewColumn column)
        {
            return column?.Tag as GridPropertyMetadata;
        }

        private DataGridViewColumn CreateCustomColumn(
          GridPropertyMetadata metadata,
          ControlRenderHost renderHost)
        {
            if (renderHost == null)
            {
                throw new InvalidOperationException(
                    "RenderHost is required for CustomColumn.");
            }

            Type dataType =
                Nullable.GetUnderlyingType(metadata.Property.PropertyType)
                ?? metadata.Property.PropertyType;

            Type viewType =
                metadata.Attribute.CustomColumnType;

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
