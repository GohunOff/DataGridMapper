using System;
using System.Collections.Generic;
using System.Linq;

namespace MC.Data.DataGrid.Metadata
{
    public sealed class GridMetadata
    {
        private readonly Dictionary<string, GridPropertyMetadata>
            _propertiesByName;

        public Type ModelType { get; private set; }

        public IReadOnlyList<GridPropertyMetadata> Properties
        {
            get;
            private set;
        }

        public GridMetadata(
            Type modelType,
            IReadOnlyList<GridPropertyMetadata> properties)
        {
            if (modelType == null)
                throw new ArgumentNullException(
                    "modelType");

            if (properties == null)
                throw new ArgumentNullException(
                    "properties");

            ModelType = modelType;
            Properties = properties;

            _propertiesByName =
                properties.ToDictionary(
                    x => x.PropertyName,
                    StringComparer.OrdinalIgnoreCase);
        }

        public bool TryGetProperty(
            string propertyName,
            out GridPropertyMetadata property)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                property = null;
                return false;
            }

            return _propertiesByName.TryGetValue(
                propertyName,
                out property);
        }

        public GridPropertyMetadata GetProperty(
            string propertyName)
        {
            GridPropertyMetadata property;

            if (!TryGetProperty(
                propertyName,
                out property))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Property '{0}' was not found on type '{1}'.",
                        propertyName,
                        ModelType.FullName));
            }

            return property;
        }
    }
}
