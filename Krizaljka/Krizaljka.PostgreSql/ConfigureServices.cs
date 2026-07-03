
using Dapper;
using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.Domain.Idea;
using Krizaljka.Domain.Template;
using Krizaljka.Domain.TermDescription;
using Krizaljka.Domain.Terms;
using Krizaljka.Domain.User.Repo;
using Krizaljka.PostgreSql.KrizaljkaIdeaPostgreSql;
using Krizaljka.PostgreSql.KrizaljkaTemplatePostgreSql;
using Krizaljka.PostgreSql.Postgres.Stuff;
using Krizaljka.PostgreSql.Postgres.Stuff.DapperSqlMappers;
using Krizaljka.PostgreSql.Postgres.Stuff.DataProtection;
using Krizaljka.PostgreSql.TermDescriptionPostgreSql;
using Krizaljka.PostgreSql.TermPostgreSql;
using Krizaljka.PostgreSql.User;
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
        SqlMapper.AddTypeHandler(new JsonbTypeHandler<List<TemplateBlock>>());
        SqlMapper.AddTypeHandler(new JsonbTypeHandler<List<string>>());
        SqlMapper.AddTypeHandler(new JsonbTypeHandler<List<int>>());

        KrizaljkaPostgresOptions opts = new();
        options.Invoke(opts);

        GuardAgainstInvalidOptionsValues(opts);

        services.AddSingleton(opts);

        services.AddSingleton<IXmlRepository, DbXmlRepo>();
        services.AddSingleton<IDatabaseUtils, DatabaseUtils>();

        services.AddScoped<IDbSession<ConnStrings>, DbSession>();

        services.AddScoped<IKrizaljkaTemplateRepo, KrizaljkaTemplateRepo>();
        services.AddScoped<ITermDescriptionRepo, TermDescriptionRepo>();
        services.AddScoped<ITermRepo, TermRepo>();
        services.AddScoped<ITermImportBatchRepo, TermImportBatchRepo>();
        services.AddScoped<IAppUserRepo, AppUserRepo>();
        services.AddScoped<IKrizaljkaIdeaRepo, KrizaljkaIdeaRepo>();

        services.AddSingleton<ConnectionStrings>(_ => new ConnectionStrings(new Dictionary<ConnStrings, string>
        {
            { ConnStrings.Core, opts.ConnectionStringCore },
            { ConnStrings.Au, opts.ConnectionStringAu },

        }));

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
