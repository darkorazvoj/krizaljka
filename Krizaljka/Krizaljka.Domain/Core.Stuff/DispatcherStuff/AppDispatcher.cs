using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Krizaljka.Domain.Core.Stuff.DispatcherStuff;

public sealed class AppDispatcher(
    IServiceProvider serviceProvider,
    IAuthUser authUser)
{
    public async Task<IServiceResult> DispatchAsync<TServiceRequest>(
        TServiceRequest request,
        CancellationToken ct = default)
        where TServiceRequest : IServiceRequest
    {
        if (!authUser.IsAuthenticatedAppUser)
        {
            return new NoAuthUser();
        }

        return await InvokeAsync(request, ct);
    }

    private Task<IServiceResult> InvokeAsync<TServiceRequest>(TServiceRequest request, CancellationToken ct)
        where TServiceRequest : IServiceRequest
        =>
            serviceProvider
                .GetRequiredService<IAppRequestHandler<TServiceRequest>>()
                .HandleAsync(request, ct);
}
