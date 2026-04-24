
using Dapper;
using Krizaljka.Domain.Template;
using Krizaljka.PostgreSql.KrizaljkaTemplatePostgreSql;
using Krizaljka.PostgreSql.Postgres.Stuff.DapperSqlMappers;
using Krizaljka.PostgreSql.Postgres.Stuff.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Krizaljka.PostgreSql;

public static class ConfigureServices
{
    public static IServiceCollection AddKrizaljkaPostgreSql(
        this IServiceCollection services,
        Action<KrizaljkaPostgresOptions> options)
    {
        // Postgres's timestampz maps to DateTime. This handler maps it to DateTimeOffset.
        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new IntJaggedArrayHandler());

        KrizaljkaPostgresOptions opts = new();
        options.Invoke(opts);

        GuardAgainstInvalidOptionsValues(opts);

        services.AddSingleton(opts);

        services.AddSingleton<IXmlRepository, DbXmlRepo>();

        services.AddScoped<IKrizaljkaTemplateRepo, KrizaljkaTemplateRepo>();

        services.AddSingleton<IReadOnlyDictionary<ConnStrings, string>>(
            _ => new Dictionary<ConnStrings, string>
            {
                { ConnStrings.Core , opts.ConnectionStringCore},
                { ConnStrings.Au , opts.ConnectionStringAu},
          
            });

        return services;
    }

    private static void GuardAgainstInvalidOptionsValues(KrizaljkaPostgresOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionStringCore))
        {
            throw new ArgumentException("Missing PostgreSql Core connection string.");
        }
    }
}
