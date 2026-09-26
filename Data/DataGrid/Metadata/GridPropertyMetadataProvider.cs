using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MC.Data.DataGrid
{
    public sealed class GridPropertyMetadataProvider
    {
        public IReadOnlyCollection<GridPropertyMetadata> GetMetadata<T>()
        {
            return GetMetadata(typeof(T));
        }

        public IReadOnlyCollection<GridPropertyMetadata> GetMetadata(
            Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            var properties =
                type.GetProperties(
                    BindingFlags.Public |
                    BindingFlags.Instance);

            var result =
                new List<GridPropertyMetadata>();

            foreach (var property in properties)
            {
                var attribute =
                    property.GetCustomAttribute<GridViewAttribute>();

                if (attribute == null)
                    continue;

                result.Add(
                    new GridPropertyMetadata(
                        property,
                        attribute));
            }

            return result;
        }
    }
}
