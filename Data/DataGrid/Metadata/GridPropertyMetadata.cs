using MC.Data.DataGrid;
using System;
using System.Reflection;

public sealed class GridPropertyMetadata
{
    public PropertyInfo Property { get; private set; }

    public string PropertyName
    {
        get { return Property.Name; }
    }

    public Type PropertyType
    {
        get { return Property.PropertyType; }
    }

    public string Name { get; private set; }

    public string ColumnName { get; private set; }

    public GridViewAttribute.EColumnType ColumnType { get; private set; }

    public string Format { get; private set; }

    public bool Visibility { get; private set; }

    public bool AllowEdit { get; private set; }

    public bool Ignore { get; private set; }

    public string ActionName { get; private set; }

    public GridViewAttribute Attribute { get; private set; }

    public GridPropertyMetadata(
        PropertyInfo property,
        GridViewAttribute attribute)
    {
        if (property == null)
            throw new ArgumentNullException("property");

        Property = property;
        Attribute = attribute;

        if (attribute == null)
        {
            Name = property.Name;
            ColumnName = property.Name;
            ColumnType =
                GridViewAttribute.EColumnType.None;

            Format = string.Empty;
            Visibility = true;
            AllowEdit = false;
            Ignore = false;
            ActionName = string.Empty;

            return;
        }

        Name = attribute.Name;
        ColumnName = attribute.ColumnName;
        ColumnType = attribute.ColumnType;
        Format = attribute.Format;
        Visibility = attribute.Visibility;
        AllowEdit = attribute.AllowEdit;
        Ignore = attribute.Ignore;
        ActionName = attribute.ActionName;
    }
}
