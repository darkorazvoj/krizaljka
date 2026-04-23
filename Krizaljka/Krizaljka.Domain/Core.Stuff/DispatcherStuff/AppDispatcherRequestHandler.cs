

using Krizaljka.Domain.Core.Stuff.Services;

namespace Krizaljka.Domain.Core.Stuff.DispatcherStuff;

public interface IAppRequestHandler<in TRequest>
    where TRequest : IServiceRequest
{
    Task<IServiceResult> HandleAsync(TRequest request, CancellationToken ct);
}
