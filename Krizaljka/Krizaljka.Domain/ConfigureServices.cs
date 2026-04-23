
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

        return services;
    }
}
