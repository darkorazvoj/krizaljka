
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Dummies;
using Krizaljka.Domain.Core.Stuff.Extensions;
using Krizaljka.Domain.Core.Stuff.Hashers;
using Krizaljka.Domain.Core.Stuff.Utils;
using Krizaljka.Domain.User.Services;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();

        // Dummies
        services.AddSingleton<IDatabaseUtils, DummyDatabaseUtils>();

        return services;
    }
}
