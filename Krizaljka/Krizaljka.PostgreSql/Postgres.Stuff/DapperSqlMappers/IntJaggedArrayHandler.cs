using System.Data;
using System.Text.Json;
using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace Krizaljka.PostgreSql.Postgres.Stuff.DapperSqlMappers;

public class IntJaggedArrayHandler : SqlMapper.TypeHandler<int[][]>
{
    public override void SetValue(IDbDataParameter parameter, int[][]? value)
    {
        parameter.Value = value;
        if (parameter is NpgsqlParameter npgsqlParameter)
        {
            npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;
        }
    }

    public override int[][] Parse(object value) =>
        value switch
        {
            int[][] arr => arr,
            string jsonb => JsonSerializer.Deserialize<int[][]>(jsonb) ?? [],
            _ => throw new DataException($"Cannot convert {value.GetType()} to int[][]")
        };
}

