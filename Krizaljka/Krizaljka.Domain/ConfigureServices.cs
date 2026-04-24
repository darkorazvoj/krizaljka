
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Extensions;
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

        return services;
    }
}
