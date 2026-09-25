using System;
using System.Reflection;

namespace MC.Data.DataGrid.Metadata
{
    public sealed class PropertyMetadata
    {
        public PropertyInfo PropertyInfo { get; private set; }

        public string PropertyName
        {
            get { return PropertyInfo.Name; }
        }

        public Type PropertyType
        {
            get { return PropertyInfo.PropertyType; }
        }

        public Type UnderlyingType
        {
            get
            {
                return Nullable.GetUnderlyingType(PropertyType)
                       ?? PropertyType;
            }
        }

        public bool IsNullable
        {
            get
            {
                return !PropertyType.IsValueType ||
                       Nullable.GetUnderlyingType(PropertyType) != null;
            }
        }

        public bool IsEnum
        {
            get { return UnderlyingType.IsEnum; }
        }

        public bool IsBoolean
        {
            get { return UnderlyingType == typeof(bool); }
        }

        public bool IsString
        {
            get { return UnderlyingType == typeof(string); }
        }

        public bool IsNumeric
        {
            get
            {
                Type type = UnderlyingType;

                return
                    type == typeof(byte) ||
                    type == typeof(sbyte) ||
                    type == typeof(short) ||
                    type == typeof(ushort) ||
                    type == typeof(int) ||
                    type == typeof(uint) ||
                    type == typeof(long) ||
                    type == typeof(ulong) ||
                    type == typeof(float) ||
                    type == typeof(double) ||
                    type == typeof(decimal);
            }
        }

        public PropertyMetadata(
            PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException(
                    nameof(propertyInfo));

            PropertyInfo = propertyInfo;
        }
    }
}
