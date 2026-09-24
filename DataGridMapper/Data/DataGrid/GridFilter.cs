using MC.Data.DataGrid.Filter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MC.Data.DataGrid
{
    public class GridFilter
    {
        public int FilterPanelHeight { get; set; } = 30;
        private readonly DataGridView _grid;
        private FilterContext _context;

            public GridFilter(
                DataGridView grid)
            {
                if (grid == null)
                    throw new ArgumentNullException("grid");

                _grid = grid;
            }

        public bool Enable()
        {
            if (_context != null)
                return true;

            object originalDataSource =
                _grid.DataSource;

            if (originalDataSource == null)
            {
                MessageBox.Show(
                    "DataGridView nie posiada źródła danych.",
                    "GridFilter",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return false;
            }

            bool ownsBindingSource;

            BindingSource bindingSource =
                GetOrCreateBindingSource(
                    out ownsBindingSource);

            try
            {
                FilterContext context =
                    CreateContext(
                        _grid,
                        bindingSource,
                        ownsBindingSource);

                CreateFilterPanel(context);

                AttachEvents(context);

                RefreshFilterControls(context);

                _context = context;

                return true;
            }
            catch (Exception ex)
            {
                if (ownsBindingSource)
                {
                    _grid.DataSource =
                        originalDataSource;
                }

                System.Diagnostics.Debug.WriteLine(ex);

                MessageBox.Show(
                    "Nie udało się uruchomić filtrowania.\r\n\r\n" +
                    ex.Message,
                    "GridFilter",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
        }


        private void RefreshFilterControls(
            FilterContext context)
        {
            UpdateFilterControlBounds(context);
        }

        private void AttachEvents(
        FilterContext context)
            {
                DataGridView grid = context.Grid;

                grid.Scroll += Grid_Scroll;
                grid.ColumnWidthChanged += Grid_ColumnWidthChanged;
                grid.ColumnDisplayIndexChanged +=
                    Grid_ColumnDisplayIndexChanged;
                grid.Resize += Grid_Resize;
        }

        private void Grid_Scroll(
            object sender,
            ScrollEventArgs e)
        {
            if (_context?.Grid != null)
                UpdateFilterControlBounds(_context);
        }

        private void Grid_ColumnWidthChanged(
            object sender,
            DataGridViewColumnEventArgs e)
        {
            if (_context != null)
            {
                UpdateFilterControlBounds(_context);
            }
        }

        private void Grid_ColumnDisplayIndexChanged(
            object sender,
            DataGridViewColumnEventArgs e)
        {
            if (_context != null)
            {
                UpdateFilterControlBounds(_context);
            }
        }

        private void Grid_Resize(
            object sender,
            EventArgs e)
        {
            if (_context != null)
            {
                UpdateFilterControlBounds(_context);
            }
        }

        private void UpdateFilterControlBounds(
    FilterContext context)
        {
            DataGridView grid =
                context.Grid;

            Panel panel =
                context.FilterPanel;

            foreach (DataGridViewColumn column
                in grid.Columns)
            {
                if (!column.Visible)
                    continue;

                FilterEditor editor;

                if (!context.Controls.TryGetValue(
                    column.Index,
                    out editor))
                    continue;

                Rectangle rectangle =
                    grid.GetColumnDisplayRectangle(
                        column.Index,
                        true);

                int width =
                    Math.Max(
                        0,
                        rectangle.Width - 1);

                int height =
                    panel.Height - 6;

                if (editor.IsBoolean)
                {
                    editor.BooleanComboBox.SetBounds(
                        rectangle.X,
                        3,
                        width,
                        height);

                    continue;
                }

                int operatorWidth =
                    Math.Min(
                        70,
                        Math.Max(
                            45,
                            width / 3));

                int valueWidth =
                    Math.Max(
                        0,
                        width - operatorWidth - 2);

                editor.OperatorComboBox.SetBounds(
                    rectangle.X,
                    3,
                    operatorWidth,
                    height);

                editor.ValueTextBox.SetBounds(
                    rectangle.X +
                        operatorWidth +
                        2,
                    3,
                    valueWidth,
                    height);
            }
        }



        private void CreateFilterPanel(
                FilterContext context)
        {
            DataGridView grid =
                context.Grid;

            if (grid.Parent == null)
            {
                throw new InvalidOperationException(
                    "DataGridView musi posiadać Parent.");
            }

            Panel panel =
                new Panel
                {
                    Height = FilterPanelHeight,
                    BackColor = System.Drawing.SystemColors.Control,
                    BorderStyle =
                        BorderStyle.FixedSingle
                };

            Control parent =
                grid.Parent;

            parent.Controls.Add(panel);

            panel.Dock =
                DockStyle.Top;

            grid.BringToFront();

            panel.BringToFront();

            context.FilterPanel =
                panel;

            CreateControls(context);
        }

        private FilterEditor CreateFilterEditor(
    FilterContext context,
    string propertyName)
        {
            PropertyDescriptor property =
                GetPropertyDescriptor(
                    context.BindingSource,
                    propertyName);

            if (property == null)
                return null;

            Type type =
                Nullable.GetUnderlyingType(
                    property.PropertyType)
                ?? property.PropertyType;

            // BOOL
            if (type == typeof(bool))
            {
                ComboBox comboBox =
                    new ComboBox
                    {
                        DropDownStyle =
                            ComboBoxStyle.DropDownList
                    };

                comboBox.Items.Add("Wszystkie");
                comboBox.Items.Add("Tak");
                comboBox.Items.Add("Nie");

                comboBox.SelectedIndex = 0;

                FilterEditor editor =
                    new FilterEditor(
                        propertyName,
                        comboBox);

                comboBox.Tag = editor;

                comboBox.SelectedIndexChanged +=
                    FilterControlChanged;

                return editor;
            }

            // POZOSTAŁE TYPY

            ComboBox operatorComboBox =
                CreateOperatorComboBox(
                    property.PropertyType);

            TextBox valueTextBox =
                new TextBox
                {
                    BorderStyle =
                        BorderStyle.FixedSingle
                };

            FilterEditor filterEditor =
                new FilterEditor(
                    propertyName,
                    operatorComboBox,
                    valueTextBox);

            operatorComboBox.Tag =
                filterEditor;

            valueTextBox.Tag =
                filterEditor;

            operatorComboBox.SelectedIndexChanged +=
                FilterControlChanged;

            valueTextBox.TextChanged +=
                FilterControlChanged;

            return filterEditor;
        }

        private void CreateControls(
    FilterContext context)
        {
            DataGridView grid =
                context.Grid;

            Panel panel =
                context.FilterPanel;

            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (!column.Visible)
                    continue;

                if (string.IsNullOrWhiteSpace(
                    column.DataPropertyName))
                    continue;

                FilterEditor editor =
                    CreateFilterEditor(
                        context,
                        column.DataPropertyName);

                if (editor == null)
                    continue;

                if (editor.IsBoolean)
                {
                    panel.Controls.Add(
                        editor.BooleanComboBox);
                }
                else
                {
                    panel.Controls.Add(
                        editor.OperatorComboBox);

                    panel.Controls.Add(
                        editor.ValueTextBox);
                }

                context.Controls[
                    column.Index] =
                    editor;
            }

            UpdateFilterControlBounds(context);
        }




        private PropertyDescriptor GetPropertyDescriptor(
           BindingSource source,
           string propertyName)
        {
            if (source == null)
                return null;

            PropertyDescriptorCollection properties =
                source.GetItemProperties(null);

            if (properties == null)
                return null;

            return properties.Find(
                propertyName,
                true);
        }


        private void FilterControlChanged(
    object sender,
    EventArgs e)
        {
            Control control =
                sender as Control;

            if (control == null)
                return;

            FilterEditor editor =
                control.Tag as FilterEditor;

            if (editor == null)
                return;

            FilterContext context =
                _context;

            if (context == null)
                return;

            string propertyName =
                editor.PropertyName;

            FilterDefinition filter =
                CreateFilterDefinition(
                    context,
                    editor,
                    propertyName);

            if (filter == null)
            {
                context.Filters.Remove(
                    propertyName);
            }
            else
            {
                context.Filters[propertyName] =
                    filter;
            }

            ApplyFilter(context);
        }



        private FilterDefinition CreateFilterDefinition(
    FilterContext context,
    FilterEditor editor,
    string propertyName)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (editor == null)
                throw new ArgumentNullException("editor");

            if (string.IsNullOrWhiteSpace(propertyName))
                return null;

            // BOOL
            if (editor.IsBoolean)
            {
                if (editor.BooleanComboBox == null)
                    return null;

                if (editor.BooleanComboBox.SelectedIndex <= 0)
                    return null;

                bool boolValue =
                    editor.BooleanComboBox.SelectedIndex == 1;

                return new FilterDefinition(
                    propertyName,
                    FilterOperator.Equals,
                    boolValue);
            }

            // POZOSTAŁE TYPY

            if (editor.OperatorComboBox == null)
                return null;

            if (editor.OperatorComboBox.SelectedItem == null)
                return null;

            FilterOperatorItem operatorItem =
                editor.OperatorComboBox.SelectedItem
                    as FilterOperatorItem;

            if (operatorItem == null)
                return null;

            FilterOperator filterOperator =
                operatorItem.Operator;

            // IS NULL / IS NOT NULL
            if (filterOperator == FilterOperator.IsNull ||
                filterOperator == FilterOperator.IsNotNull)
            {
                return new FilterDefinition(
                    propertyName,
                    filterOperator,
                    null);
            }

            string textValue =
                editor.ValueTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(textValue))
                return null;

            return new FilterDefinition(
                propertyName,
                filterOperator,
                textValue);
        }



        public void Disable()
        {
            if (_context == null)
                return;

            FilterContext context =
                _context;

            DetachEvents(context);

            if (context.FilterPanel != null)
            {
                Control parent =
                    context.FilterPanel.Parent;

                if (parent != null)
                {
                    parent.Controls.Remove(
                        context.FilterPanel);
                }

                context.FilterPanel.Dispose();
            }

            if (context.OwnsBindingSource)
            {
                _grid.DataSource =
                    context.OriginalDataSource;
            }
            else
            {
                context.BindingSource.DataSource =
                    context.OriginalDataSource;

                context.BindingSource.ResetBindings(false);
            }

            _context = null;
        }

        private sealed class FilterOperatorItem
        {
            public FilterOperator Operator { get; private set; }

            public string DisplayName { get; private set; }

            public FilterOperatorItem(
                FilterOperator @operator,
                string displayName)
            {
                Operator = @operator;
                DisplayName = displayName;
            }

            public override string ToString()
            {
                return DisplayName;
            }
        }



        private ComboBox CreateOperatorComboBox(
    Type propertyType)
        {
            ComboBox comboBox =
                new ComboBox
                {
                    DropDownStyle =
                        ComboBoxStyle.DropDownList
                };

            Type type =
                Nullable.GetUnderlyingType(propertyType)
                ?? propertyType;

            if (type == typeof(string))
            {
                AddOperator(
                    comboBox,
                    FilterOperator.Contains,
                    "zawiera");

                AddOperator(
                    comboBox,
                    FilterOperator.StartsWith,
                    "zaczyna się od");

                AddOperator(
                    comboBox,
                    FilterOperator.EndsWith,
                    "kończy się na");

                AddOperator(
                    comboBox,
                    FilterOperator.Equals,
                    "==");

                AddOperator(
                    comboBox,
                    FilterOperator.NotEquals,
                    "!=");

                AddOperator(
                    comboBox,
                    FilterOperator.IsNull,
                    "is null");

                AddOperator(
                    comboBox,
                    FilterOperator.IsNotNull,
                    "is not null");
            }
            else if (type == typeof(bool))
            {
                AddOperator(
                    comboBox,
                    FilterOperator.Equals,
                    "==");

                AddOperator(
                    comboBox,
                    FilterOperator.NotEquals,
                    "!=");

                AddOperator(
                    comboBox,
                    FilterOperator.IsNull,
                    "is null");

                AddOperator(
                    comboBox,
                    FilterOperator.IsNotNull,
                    "is not null");
            }
            else if (IsNumericType(type) ||
                     type == typeof(DateTime))
            {
                AddOperator(
                    comboBox,
                    FilterOperator.Equals,
                    "==");

                AddOperator(
                    comboBox,
                    FilterOperator.NotEquals,
                    "!=");

                AddOperator(
                    comboBox,
                    FilterOperator.GreaterThan,
                    ">");

                AddOperator(
                    comboBox,
                    FilterOperator.GreaterThanOrEqual,
                    ">=");

                AddOperator(
                    comboBox,
                    FilterOperator.LessThan,
                    "<");

                AddOperator(
                    comboBox,
                    FilterOperator.LessThanOrEqual,
                    "<=");

                AddOperator(
                    comboBox,
                    FilterOperator.IsNull,
                    "is null");

                AddOperator(
                    comboBox,
                    FilterOperator.IsNotNull,
                    "is not null");
            }
            else
            {
                AddOperator(
                    comboBox,
                    FilterOperator.Equals,
                    "==");

                AddOperator(
                    comboBox,
                    FilterOperator.NotEquals,
                    "!=");
            }

            FilterOperator defaultOperator =
                GetDefaultOperator(type);

            foreach (FilterOperatorItem item
                in comboBox.Items)
            {
                if (item.Operator == defaultOperator)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }

            return comboBox;
        }

        private void AddOperator(
            ComboBox comboBox,
            FilterOperator filterOperator,
            string displayName)
        {
            comboBox.Items.Add(
                new FilterOperatorItem(
                    filterOperator,
                    displayName));
        }



        private FilterOperator GetDefaultOperator(Type type)
        {
            type =
                Nullable.GetUnderlyingType(type)
                ?? type;

            if (type == typeof(string))
                return FilterOperator.Contains;

            if (type == typeof(bool))
                return FilterOperator.Equals;

            if (IsNumericType(type))
                return FilterOperator.Equals;

            if (type == typeof(DateTime))
                return FilterOperator.Equals;

            if (type.IsEnum)
                return FilterOperator.Equals;

            return FilterOperator.Equals;
        }
        private bool IsNumericType(Type type)
        {
            type =
                Nullable.GetUnderlyingType(type)
                ?? type;

            return type == typeof(byte)
                || type == typeof(short)
                || type == typeof(int)
                || type == typeof(long)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal);
        }


        private void DetachEvents(
            FilterContext context)
        {
            DataGridView grid =
                context.Grid;

            grid.Scroll -=
                Grid_Scroll;

            grid.ColumnWidthChanged -=
                Grid_ColumnWidthChanged;

            grid.ColumnDisplayIndexChanged -=
                Grid_ColumnDisplayIndexChanged;

            grid.Resize -=
                Grid_Resize;
        }


        public void SetFilter(
            string propertyName,
            FilterOperator filterOperator,
            object value)
        {
            if (_context == null)
            {
                throw new InvalidOperationException(
                    "GridFilter must be enabled before setting filters.");
            }

            var filter =
                new FilterDefinition(
                    propertyName,
                    filterOperator,
                    value);

            _context.Filters[propertyName] =
                filter;

            ApplyFilter(_context);
        }


        private FilterContext CreateContext(
            DataGridView grid,
            BindingSource bindingSource,
            bool ownsBindingSource)
        {
            var context =
                new FilterContext();

            context.Grid = grid;
            context.BindingSource =
                bindingSource;

            context.OriginalDataSource =
                bindingSource.DataSource;

            context.OwnsBindingSource =
                ownsBindingSource;

            var source =
                BindingSourceResolver.Resolve(
                    bindingSource);

            context.DataSource =
                DataSourceAdapterFactory.Create(
                    source);

            return context;
        }


        private BindingSource GetOrCreateBindingSource(
            out bool ownsBindingSource)
        {
            var bindingSource =
                _grid.DataSource as BindingSource;

            if (bindingSource != null)
            {
                ownsBindingSource = false;
                return bindingSource;
            }

            var source =
                _grid.DataSource;

            if (source == null)
            {
                throw new InvalidOperationException(
                    "DataGridView.DataSource cannot be null.");
            }

            bindingSource =
                new BindingSource();

            bindingSource.DataSource =
                source;

            _grid.DataSource =
                bindingSource;

            ownsBindingSource = true;

            return bindingSource;
        }


        private void ApplyFilter(
                FilterContext context)
            {
                if (context == null)
                    throw new ArgumentNullException(
                        "context");

                var filters =
                    context.Filters.Values.ToList();

                var result =
                    context.DataSource.ApplyFilters(
                        filters);

                context.BindingSource.DataSource =
                    result;

                context.BindingSource.ResetBindings(false);
            }
        }
    }
