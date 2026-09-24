using System;

namespace MC.Data.DataGrid
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

        public string ActionName { get; set; }

        public GridViewAttribute(
            string name,
            bool ignore = false,
            bool visibility = true,
            bool allowEdit = false,
            string columnName = "",
            string format = "",
            eColumnType columnType = eColumnType.None,
           string actionName = "")
        {
            Name = name;
            Ignore = ignore;
            Visibility = visibility;
            AllowEdit = allowEdit;
            ColumnName = columnName;
            ColumnType = columnType;
            Format = format;
            ActionName = actionName.Trim();
        }
    }
}
