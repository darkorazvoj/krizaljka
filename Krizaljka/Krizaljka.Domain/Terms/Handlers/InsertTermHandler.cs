using Krizaljka.Domain.Core.Stuff;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;

namespace Krizaljka.Domain.Terms.Handlers;


public record InsertTermServiceRequest(
    TermLanguage? Language,
    string? Description,
    string? Term) : IServiceRequest;

internal class InsertTermHandler(IAuthUser authUser) : IAppRequestHandler<InsertTermServiceRequest>
{
    public Task<IServiceResult> HandleAsync(InsertTermServiceRequest request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
