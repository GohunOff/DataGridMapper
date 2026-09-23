using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Forms;

namespace MC.Data.DataGrid
{
    public class GridFilter
    {
        public enum FilterOperator
        {
            Contains,
            Equals,
            StartsWith,
            EndsWith,
            GreaterThan,
            GreaterThanOrEqual,
            LessThan,
            LessThanOrEqual
        }
        public sealed class FilterDefinition
        {
            public string PropertyName { get; }

            public FilterOperator Operator { get; }

            public object Value { get; }

            public FilterDefinition(
                string propertyName,
                FilterOperator @operator,
                object value)
            {
                PropertyName = propertyName;
                Operator = @operator;
                Value = value;
            }
        }

        internal interface IFilterEngine
        {
            object Apply(
                object source,
                IReadOnlyCollection<FilterDefinition> filters);
        }
        internal sealed class EnumerableFilterEngine<T>
                    : IFilterEngine
        {
            public object Apply(
                object source,
                IReadOnlyCollection<FilterDefinition> filters)
            {
                IEnumerable<T> query =
                    (IEnumerable<T>)source;

                // ...
                return query;
            }
        }

        internal sealed class QueryableFilterEngine<T>
            : IFilterEngine
        {
            public object Apply(
                object source,
                IReadOnlyCollection<FilterDefinition> filters)
            {
                IQueryable<T> query =
                    (IQueryable<T>)source;

                // ...
                return query;
            }
        }

        private IEnumerable<T> ApplyEnumerableFilter<T>(
            IEnumerable<T> source,
            IReadOnlyCollection<FilterDefinition> filters)
        {
            IEnumerable<T> result = source;

            foreach (FilterDefinition filter in filters)
            {
                //result = ApplyFilter(
                //    result,
                //    filter);
            }

            return result;
        }

        internal static class FilterExpressionBuilder
        {
            public static Expression<Func<T, bool>>
                Build<T>(FilterDefinition filter)
            {
                ParameterExpression parameter =
                    Expression.Parameter(
                        typeof(T),
                        "x");

                Expression property =
                    Expression.PropertyOrField(
                        parameter,
                        filter.PropertyName);

                Expression body =
                    BuildOperation(
                        property,
                        filter);

                return Expression.Lambda<Func<T, bool>>(
                    body,
                    parameter);
            }

        }

        internal interface IDataSourceAdapter
        {
            Type ItemType { get; }

            object GetSource();

            object ApplyFilters(
                IReadOnlyCollection<FilterDefinition> filters);
        }

        internal sealed class EnumerableDataSourceAdapter<T>
            : IDataSourceAdapter
                {
                    private readonly IEnumerable<T> source;

                    public EnumerableDataSourceAdapter(
                        IEnumerable<T> source)
                    {
                        this.source = source;
                    }

                    public Type ItemType => typeof(T);

                    public object GetSource()
                        => source;

                    public object ApplyFilters(
                        IReadOnlyCollection<FilterDefinition> filters)
                    {
                        IEnumerable<T> result = source;

                        foreach (FilterDefinition filter in filters)
                        {
                            Expression<Func<T, bool>> expression =
                                FilterExpressionBuilder.Build<T>(filter);

                            result = result.Where(
                                expression.Compile());
                        }

                        return result;
                    }
        }

        internal sealed class QueryableDataSourceAdapter<T>
    : IDataSourceAdapter
        {
            private readonly IQueryable<T> source;

            public QueryableDataSourceAdapter(
                IQueryable<T> source)
            {
                this.source = source;
            }

            public Type ItemType => typeof(T);

            public object GetSource()
                => source;

            public object ApplyFilters(
                IReadOnlyCollection<FilterDefinition> filters)
            {
                IQueryable<T> result = source;

                foreach (FilterDefinition filter in filters)
                {
                    Expression<Func<T, bool>> expression =
                        FilterExpressionBuilder.Build<T>(filter);

                    result = result.Where(expression);
                }

                return result;
            }
        }

        private static Expression BuildOperation(
    Expression property,
    FilterDefinition filter)
        {
            // Equals
            // Contains
            // StartsWith
            // EndsWith
            // >
            // >=
            // <
            // <=
            return default(Expression);
        }

        public int FilterPanelHeight { get; set; } = 30;

        private sealed class FilterContext
        {
            public DataGridView Grid { get; set; }

            public BindingSource Source { get; set; }

            public object OriginalDataSource { get; set; }

            public Panel FilterPanel { get; set; }

            public Dictionary<int, Control> Controls { get; } =
                new Dictionary<int, Control>();

            public Dictionary<string, string> Values { get; } =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

            List<FilterDefinition> Values2 { get; set; }
        }

        private FilterContext _context;
        private readonly DataGridView grid;

        public GridFilter(DataGridView grid)
        {
            this.grid = grid;
        }

        public bool Enable()
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));

            if (_context != null)
                return true;

            BindingSource source =
                GetOrCreateBindingSource(
                    grid,
                    out object originalDataSource,
                    out bool ownsBindingSource);

            if (source == null)
            {
                MessageBox.Show(
                    "DataGridView nie posiada źródła danych.",
                    "GridFilter",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return false;
            }

            try
            {
                FilterContext context = new FilterContext
                {
                    Grid = grid,
                    Source = source,
                    OriginalDataSource = originalDataSource,
                };

                CreateFilterPanel(context);

                _context = context;

                AttachEvents(context);

                RefreshFilterControls(context);

                return true;
            }
            catch (Exception ex)
            {
                // Jeżeli utworzyliśmy BindingSource,
                // przywracamy oryginalny DataSource.
                if (ownsBindingSource)
                    grid.DataSource = originalDataSource;

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

        public void Disable()
        {
            if (grid == null || _context == null)
                return;

            FilterContext context = _context;

            DetachEvents(context);

            if (context.FilterPanel != null)
            {
                Control parent = context.FilterPanel.Parent;

                parent?.Controls.Remove(context.FilterPanel);

                context.FilterPanel.Dispose();
            }

            // Przywracamy oryginalny DataSource.
            grid.DataSource = context.OriginalDataSource;

            _context = null;
        }

        private BindingSource GetOrCreateBindingSource(
                    DataGridView grid,
                    out object originalDataSource,
                    out bool ownsBindingSource)
        {
            originalDataSource = grid.DataSource;
            ownsBindingSource = false;

            if (originalDataSource == null)
                return null;

            BindingSource source = new BindingSource
            {
                DataSource = originalDataSource
            };

            grid.DataSource = source;

            ownsBindingSource = true;

            return source;
        }

        private void CreateFilterPanel(
            FilterContext context)
        {
            DataGridView grid = context.Grid;

            if (grid.Parent == null)
                throw new InvalidOperationException(
                    "DataGridView musi posiadać Parent.");

            Panel panel = new Panel
            {
                Height = FilterPanelHeight,
                BackColor = SystemColors.Control,
                BorderStyle = BorderStyle.FixedSingle
            };

            Control parent = grid.Parent;

            parent.Controls.Add(panel);

            panel.Dock = DockStyle.Top;

            // Grid powinien pozostać pod panelem.
            grid.BringToFront();

            // Panel ponownie na wierzch.
            panel.BringToFront();

            context.FilterPanel = panel;

            CreateControls(context);
        }

        private void CreateControls(
            FilterContext context)
        {
            DataGridView grid = context.Grid;
            Panel panel = context.FilterPanel;

            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (!column.Visible)
                    continue;

                if (string.IsNullOrWhiteSpace(
                    column.DataPropertyName))
                    continue;

                Control filterControl =
                    CreateFilterControl(
                        context.Source,
                        column.DataPropertyName);

                if (filterControl == null)
                    continue;

                filterControl.Tag =
                    column.DataPropertyName;

                panel.Controls.Add(filterControl);

                context.Controls.Add(
                    column.Index,
                    filterControl);
            }

            UpdateFilterControlBounds(context);
        }

        private Control CreateFilterControl(
            BindingSource source,
            string propertyName)
        {
            PropertyDescriptor property =
                GetPropertyDescriptor(
                    source,
                    propertyName);

            if (property == null)
                return null;

            Type type = Nullable.GetUnderlyingType(
                property.PropertyType)
                ?? property.PropertyType;

            if (type == typeof(bool))
            {
                ComboBox comboBox = new ComboBox
                {
                    DropDownStyle =
                        ComboBoxStyle.DropDownList
                };

                comboBox.Items.Add("Wszystkie");
                comboBox.Items.Add("Tak");
                comboBox.Items.Add("Nie");

                comboBox.SelectedIndex = 0;

                comboBox.SelectedIndexChanged +=
                    FilterControlChanged;

                return comboBox;
            }

            TextBox textBox = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle
            };

            textBox.TextChanged +=
                FilterControlChanged;

            return textBox;
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
            var control = sender as Control;

            var context = _context;

            DataGridView grid = _context.Grid;

            if (grid == null)
                return;

            string propertyName =
                control?.Tag as string;

            if (string.IsNullOrWhiteSpace(propertyName))
                return;

            string value = GetFilterValue(control);

            if (string.IsNullOrWhiteSpace(value))
            {
                context.Values.Remove(propertyName);
            }
            else
            {
                context.Values[propertyName] = value;
            }

            ApplyFilter(context);
        }

        private string GetFilterValue(
            Control control)
        {
            if (control is TextBox textBox)
                return textBox.Text.Trim();

            if (control is ComboBox comboBox)
            {
                if (comboBox.SelectedIndex <= 0)
                    return string.Empty;

                return comboBox.SelectedIndex == 1
                    ? "true"
                    : "false";
            }

            return string.Empty;
        }

        private void ApplyFilter(
                FilterContext context)
        {
            if (context == null)
                return;

            if (context.Source == null)
                return;

            if (context.OriginalDataSource == null)
                return;

            if (!(context.OriginalDataSource is System.Collections.IEnumerable source))
                return;

            List<object> items =
                source.Cast<object>().ToList();

            IEnumerable<object> result = items;

            foreach (KeyValuePair<string, string> filter
                in context.Values)
            {
                string propertyName = filter.Key;
                string filterValue = filter.Value;

                if (string.IsNullOrWhiteSpace(filterValue))
                    continue;

                PropertyDescriptor property =
                    GetPropertyDescriptor(
                        context.Source,
                        propertyName);

                if (property == null)
                    continue;

                result = result.Where(item =>
                {
                    object value =
                        property.GetValue(item);

                    if (value == null)
                        return false;

                    string text =
                        Convert.ToString(
                            value,
                            CultureInfo.CurrentCulture);

                    return text.IndexOf(
                        filterValue,
                        StringComparison.CurrentCultureIgnoreCase) >= 0;
                });
            }

            List<object> filtered =
                result.ToList();

            context.Source.DataSource = filtered;

            context.Source.ResetBindings(false);
        }

        private void UpdateFilterControlBounds(
            FilterContext context)
        {
            DataGridView grid = context.Grid;
            Panel panel = context.FilterPanel;

            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (!column.Visible)
                    continue;

                if (!context.Controls.TryGetValue(
                    column.Index,
                    out Control control))
                    continue;

                Rectangle rectangle =
                    grid.GetColumnDisplayRectangle(
                        column.Index,
                        true);

                control.SetBounds(
                    rectangle.X,
                    3,
                    Math.Max(0, rectangle.Width - 1),
                    panel.Height - 6);
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

        private void DetachEvents(
            FilterContext context)
        {
            DataGridView grid = context.Grid;

            grid.Scroll -= Grid_Scroll;
            grid.ColumnWidthChanged -=
                Grid_ColumnWidthChanged;
            grid.ColumnDisplayIndexChanged -=
                Grid_ColumnDisplayIndexChanged;
            grid.Resize -= Grid_Resize;
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
    }
}
