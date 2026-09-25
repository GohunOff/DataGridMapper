using MC.Data.DataGrid.I;
using MC.Data.DataGrid.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MC.Data.DataGrid.Metadata
{
    public sealed class GridMetadataProvider
    : IGridMetadataProvider
    {
        public GridMetadata GetMetadata(Type modelType)
        {
            if (modelType == null)
                throw new ArgumentNullException("modelType");

            var properties =
                modelType
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Select(CreatePropertyMetadata)
                    .ToList();

            return new GridMetadata(
                modelType,
                properties);
        }

        private static GridPropertyMetadata CreatePropertyMetadata(
            PropertyInfo property)
        {
            var attribute =
                property.GetCustomAttribute<GridViewAttribute>();

            return new GridPropertyMetadata(
                property,
                attribute);
        }
    }

}
