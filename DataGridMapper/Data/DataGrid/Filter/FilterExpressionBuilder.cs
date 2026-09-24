using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MC.Data.DataGrid.Filter
{
    public sealed class FilterExpressionBuilder
    {
        public Expression<Func<T, bool>> Build<T>(
            FilterDefinition filter)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");

            var parameter = Expression.Parameter(
                typeof(T),
                "x");

            var property = Expression.PropertyOrField(
                parameter,
                filter.PropertyName);

            var value = CreateValueExpression(
                property.Type,
                filter.Value);

            var body = BuildComparison(
                property,
                filter.Operator,
                value);

            return Expression.Lambda<Func<T, bool>>(
                body,
                parameter);
        }

        public Expression<Func<T, bool>> Build<T>(
            IReadOnlyCollection<FilterDefinition> filters)
        {
            if (filters == null)
                throw new ArgumentNullException("filters");

            if (filters.Count == 0)
            {
                return x => true;
            }

            var parameter = Expression.Parameter(
                typeof(T),
                "x");

            Expression body = null;

            foreach (var filter in filters)
            {
                if (filter == null)
                    throw new ArgumentException(
                        "Filter collection contains a null filter.",
                        "filters");

                var property = Expression.PropertyOrField(
                    parameter,
                    filter.PropertyName);

                var value = CreateValueExpression(
                    property.Type,
                    filter.Value);

                var current = BuildComparison(
                    property,
                    filter.Operator,
                    value);

                body = body == null
                    ? current
                    : Expression.AndAlso(body, current);
            }

            return Expression.Lambda<Func<T, bool>>(
                body,
                parameter);
        }

        private static Expression BuildComparison(
    Expression property,
    FilterOperator filterOperator,
    Expression value)
        {
            switch (filterOperator)
            {
                case FilterOperator.Equals:
                    return Expression.Equal(
                        property,
                        value);

                case FilterOperator.NotEquals:
                    return Expression.NotEqual(
                        property,
                        value);

                case FilterOperator.GreaterThan:
                    return Expression.GreaterThan(
                        property,
                        value);

                case FilterOperator.GreaterThanOrEqual:
                    return Expression.GreaterThanOrEqual(
                        property,
                        value);

                case FilterOperator.LessThan:
                    return Expression.LessThan(
                        property,
                        value);

                case FilterOperator.LessThanOrEqual:
                    return Expression.LessThanOrEqual(
                        property,
                        value);

                case FilterOperator.Contains:
                    return BuildStringMethodCall(
                        property,
                        value,
                        nameof(string.Contains));

                case FilterOperator.StartsWith:
                    return BuildStringMethodCall(
                        property,
                        value,
                        nameof(string.StartsWith));

                case FilterOperator.EndsWith:
                    return BuildStringMethodCall(
                        property,
                        value,
                        nameof(string.EndsWith));

                case FilterOperator.IsNull:
                    return BuildIsNull(property);

                case FilterOperator.IsNotNull:
                    return BuildIsNotNull(property);

                default:
                    throw new NotSupportedException(
                        string.Format(
                            "Operator '{0}' is not supported.",
                            filterOperator));
            }
        }

        private static Expression BuildIsNull(
            Expression property)
        {
            if (!IsNullable(property.Type))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Property '{0}' cannot be compared with null.",
                        property.Type.Name));
            }

            return Expression.Equal(
                property,
                Expression.Constant(
                    null,
                    property.Type));
        }

        private static Expression BuildIsNotNull(
            Expression property)
        {
            if (!IsNullable(property.Type))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Property '{0}' cannot be compared with null.",
                        property.Type.Name));
            }

            return Expression.NotEqual(
                property,
                Expression.Constant(
                    null,
                    property.Type));
        }

        private static Expression BuildStringMethodCall(
             Expression property,
             Expression value,
             string methodName)
        {
            if (property.Type != typeof(string))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Operator '{0}' can only be used with string properties.",
                        methodName));
            }

            var method = typeof(string).GetMethod(
                methodName,
                new[] { typeof(string) });

            if (method == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "String method '{0}' was not found.",
                        methodName));
            }

            var notNull =
                Expression.NotEqual(
                    property,
                    Expression.Constant(
                        null,
                        typeof(string)));

            var methodCall =
                Expression.Call(
                    property,
                    method,
                    value);

            return Expression.AndAlso(
                notNull,
                methodCall);
        }


        private static Expression CreateValueExpression(
    Type targetType,
    object value)
        {
            if (value == null)
            {
                if (!IsNullable(targetType))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            "Cannot compare non-nullable type '{0}' with null.",
                            targetType.Name));
                }

                return Expression.Constant(
                    null,
                    targetType);
            }

            var underlyingType =
                Nullable.GetUnderlyingType(targetType);

            var valueType =
                underlyingType ?? targetType;

            object convertedValue;

            if (valueType.IsInstanceOfType(value))
            {
                convertedValue = value;
            }
            else if (valueType.IsEnum)
            {
                convertedValue = Enum.Parse(
                    valueType,
                    value.ToString(),
                    true);
            }
            else if (valueType == typeof(Guid))
            {
                convertedValue = Guid.Parse(
                    value.ToString());
            }
            else
            {
                convertedValue = Convert.ChangeType(
                    value,
                    valueType);
            }

            var expression =
                Expression.Constant(
                    convertedValue,
                    valueType);

            if (underlyingType != null)
            {
                return Expression.Convert(
                    expression,
                    targetType);
            }

            return expression;
        }

        private static bool IsNullable(Type type)
        {
            return !type.IsValueType ||
                   Nullable.GetUnderlyingType(type) != null;
        }
    }


}
