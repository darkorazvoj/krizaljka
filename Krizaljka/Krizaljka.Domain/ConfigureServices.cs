
using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Dummies;
using Krizaljka.Domain.Core.Stuff.Extensions;
using Krizaljka.Domain.Core.Stuff.Hashers;
using Krizaljka.Domain.Template.Services;
using Krizaljka.Domain.TermDescription;
using Krizaljka.Domain.Terms;
using Krizaljka.Domain.User.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Krizaljka.Domain;

public static class ConfigureServices
{
    public static IServiceCollection AddKrizaljkaDomain(this IServiceCollection services,
        Action<KrizaljkaDomainOptions> options)
    {
        KrizaljkaDomainOptions opts = new();
        options.Invoke(opts);

        services.AddSingleton(opts);

        services.RegisterHandlersForAssembly(typeof(KrizaljkaDomainOptions).Assembly);

        services.AddScoped<AppDispatcher>();

        services.AddScoped<GetUserByCredentialsService>();
        services.AddScoped<InsertTemplateService>();
        services.AddScoped<InsertTermDescriptionService>();
        services.AddScoped<InsertTermService>();

        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();

        // Dummies
        services.TryAddSingleton<IDatabaseUtils, DummyDatabaseUtils>();
        services.TryAddScoped(typeof(IDbSession<>), typeof(DummyDbSession<>));

        return services;
    }
}
