using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MC.Data.DataGrid.Filter
{
    public static class DataSourceAdapterFactory
    {
        public static I.IDataSourceAdapter Create(
            object dataSource)
        {
            if (dataSource == null)
                throw new ArgumentNullException("dataSource");

            var queryable =
                dataSource as IQueryable;

            if (queryable != null)
            {
                return CreateQueryableAdapter(
                    queryable);
            }

            var enumerable =
                dataSource as IEnumerable;

            if (enumerable != null)
            {
                return CreateEnumerableAdapter(
                    enumerable);
            }

            throw new NotSupportedException(
                string.Format(
                    "Data source type '{0}' is not supported.",
                    dataSource.GetType().FullName));
        }

        private static I.IDataSourceAdapter CreateQueryableAdapter(
            IQueryable source)
        {
            var itemType =
                source.ElementType;

            var adapterType =
                typeof(QueryableDataSourceAdapter<>)
                    .MakeGenericType(itemType);

            return (I.IDataSourceAdapter)
                Activator.CreateInstance(
                    adapterType,
                    source);
        }

        private static I.IDataSourceAdapter CreateEnumerableAdapter(
            IEnumerable source)
        {
            var itemType =
                GetEnumerableItemType(
                    source.GetType());

            if (itemType == null)
            {
                throw new NotSupportedException(
                    string.Format(
                        "Cannot determine item type for data source '{0}'.",
                        source.GetType().FullName));
            }

            var adapterType =
                typeof(EnumerableDataSourceAdapter<>)
                    .MakeGenericType(itemType);

            return (I.IDataSourceAdapter)
                Activator.CreateInstance(
                    adapterType,
                    source);
        }

        private static Type GetEnumerableItemType(
            Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            var enumerableInterface =
                type.GetInterfaces()
                    .FirstOrDefault(
                        x =>
                            x.IsGenericType &&
                            x.GetGenericTypeDefinition() ==
                            typeof(IEnumerable<>));

            if (enumerableInterface != null)
            {
                return enumerableInterface.GetGenericArguments()[0];
            }

            return null;
        }
    }
}
