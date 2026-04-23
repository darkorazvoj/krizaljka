using System.Reflection;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Microsoft.Extensions.DependencyInjection;

namespace Krizaljka.Domain.Core.Stuff.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterHandlersForAssembly(this IServiceCollection services, Assembly assembly)
    {

        // Find all non-abstract classes that implement IAppRequestHandler<TRequest>
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces(), (t, i) => new { Implementation = t, Interface = i })
            .Where(x => x.Interface.IsGenericType &&
                        x.Interface.GetGenericTypeDefinition() == typeof(IAppRequestHandler<>));

        //  Register each one as Scoped
        foreach (var handler in handlerTypes)
        {
            services.AddScoped(handler.Interface, handler.Implementation);
        }
    }
}
