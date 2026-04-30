
namespace Krizaljka.PostgreSql.Postgres.Stuff.Utils;

internal static class TypesConverter
{
    internal static object? CastToType(string columnValue, Type columnType) =>
        Type.GetTypeCode(columnType) switch
        {
            TypeCode.String => columnValue,
            TypeCode.Int32 => int.TryParse(columnValue, out var i) ? i : null,
            TypeCode.Int64 => long.TryParse(columnValue, out var i) ? i : null,
            TypeCode.Boolean => bool.TryParse(columnValue, out var i) ? i : null,
            TypeCode.DateTime => DateTimeOffset.TryParse(columnValue, out var i) ? i : null,
            _ => null
        };
}
