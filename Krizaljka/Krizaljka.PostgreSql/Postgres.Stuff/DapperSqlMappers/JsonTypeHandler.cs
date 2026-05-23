using System.Data;
using System.Text.Json;
using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace Krizaljka.PostgreSql.Postgres.Stuff.DapperSqlMappers;

public class JsonbTypeHandler<T> : SqlMapper.TypeHandler<T>
{
    // Fixes the "Writing values of List<T> is not supported" error
    public override void SetValue(IDbDataParameter parameter, T? value)
    {
        // 1. Force serialization to a string so Npgsql doesn't treat it as a native array
        parameter.Value = value is null 
            ? DBNull.Value 
            : JsonSerializer.Serialize(value);

        // 2. Explicitly tell Npgsql that this text string belongs in a jsonb column
        if (parameter is NpgsqlParameter npgsqlParameter)
        {
            npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;
        }
    }

    public override T? Parse(object? value)
    {
        if (value is null or DBNull)
        {
            return default;
        }

        var json = value.ToString();
        
        return string.IsNullOrWhiteSpace(json) 
            ? default 
            : JsonSerializer.Deserialize<T>(json);
    }
}