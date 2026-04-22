using System.Data;

namespace Krizaljka.PostgreSql.Postgres.Stuff.Helpers;

public static class TypesMappings
{
    public static DbType InferDbType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string))
        {
            return DbType.String;
        }

        if (type == typeof(bool))
        {
            return DbType.Boolean;
        }

        if (type == typeof(byte))
        {
            return DbType.Byte;
        }

        if (type == typeof(short))
        {
            return DbType.Int16;
        }

        if (type == typeof(int))
        {
            return DbType.Int32;
        }

        if (type == typeof(long))
        {
            return DbType.Int64;
        }

        if (type == typeof(float))
        {
            return DbType.Single;
        }

        if (type == typeof(double))
        {
            return DbType.Double;
        }

        if (type == typeof(decimal))
        {
            return DbType.Decimal;
        }

        if (type == typeof(Guid))
        {
            return DbType.Guid;
        }

        if (type == typeof(DateTime))
        {
            return DbType.DateTime;
        }

        if (type == typeof(DateTimeOffset))
        {
            return DbType.DateTimeOffset;
        }

        if (type == typeof(TimeSpan))
        {
            return DbType.Time;
        }

        if (type == typeof(byte[]))
        {
            return DbType.Binary; // PostgreSQL bytea
        }

        throw new NotSupportedException(
            $"Type '{type.FullName}' is not supported for automatic DbType inference. " +
            "Specify DbType or NpgsqlDbType explicitly."
        );
    }
}
