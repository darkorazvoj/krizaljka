using System.Data;
using Dapper;

namespace Krizaljka.PostgreSql.Postgres.Stuff.DapperSqlMappers;

public class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
        parameter.Value = value;
    }

    public override DateTimeOffset Parse(object value)
    {
        if (value is DateTime dt)
        {
            // PostgreSQL timestamptz is typically returned as UTC. 
            // If it's Unspecified, treat it as UTC to create the offset.
            return dt.Kind == DateTimeKind.Unspecified 
                ? new DateTimeOffset(dt, TimeSpan.Zero) 
                : new DateTimeOffset(dt);
        }

        if (value is DateTimeOffset dto) return dto;

        throw new InvalidCastException($"Cannot convert {value.GetType()} to DateTimeOffset");
    }
}

