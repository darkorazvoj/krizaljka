using System.Data;
using System.Text.Json;
using Dapper;
using Krizaljka.PostgreSql.Postgres.Stuff.Helpers;
using Npgsql;
using NpgsqlTypes;

namespace Krizaljka.PostgreSql.Postgres.Stuff.Models;

public sealed class SqlParams : SqlMapper.IDynamicParameters
{
    private readonly List<NpgsqlParameter> _npgsqlParams = [];

    public SqlParams Add<T>(string name, T? value)
    {
        var p = new NpgsqlParameter
        {
            ParameterName = name,
            Value = value is null ? DBNull.Value : value
        };

        if (value is null)
        {
            p.DbType = TypesMappings.InferDbType(typeof(T));
        }

        _npgsqlParams.Add(p);
        return this;
    }

    public SqlParams Add(string name, object? value, DbType dbType)
    {
        var p = new NpgsqlParameter(name, value ?? DBNull.Value)
        {
            DbType = dbType
        };

        _npgsqlParams.Add(p);
        return this;
    }

    public SqlParams AddBytea(string name, byte[] value)
    {
        _npgsqlParams.Add(new NpgsqlParameter<byte[]>(name, value)
        {
            NpgsqlDbType = NpgsqlDbType.Bytea
        });

        return this;
    }

    public SqlParams AddByteaArray(string name, IEnumerable<byte[]> values)
    {
        _npgsqlParams.Add(new NpgsqlParameter<byte[][]>(name, values.ToArray())
        {
            // This is ok, npgsql says that.
            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
            NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Bytea
        });
        return this;
    }

    public SqlParams AddOutput(string name, DbType dbType)
    {
        _npgsqlParams.Add(new NpgsqlParameter
        {
            ParameterName = name,
            DbType = dbType,
            Direction = ParameterDirection.Output
        });
        return this;
    }

    public SqlParams AddJsonb(string name, int[][] value)
    {
        _npgsqlParams.Add(new NpgsqlParameter<string>(name, JsonSerializer.Serialize(value))
        {
            NpgsqlDbType = NpgsqlDbType.Jsonb
        });

        return this;
    }

    public T? GetOutput<T>(string name)
    {
        var param = _npgsqlParams.Single(p => p.ParameterName == name);

        if (param.Value is null or DBNull)
        {
            return default;
        }

        return (T)param.Value;
    }


    void SqlMapper.IDynamicParameters.AddParameters(
        IDbCommand command,
        SqlMapper.Identity identity)
    {
        foreach (var p in _npgsqlParams)
        {
            command.Parameters.Add(p);
        }
    }
}