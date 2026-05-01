using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.User.Models;
using Krizaljka.Domain.User.Repo;

namespace Krizaljka.Domain.User.Handlers;

public record GetMeServiceRequest(long Id): IServiceRequest;
internal sealed class GetMeHandler(IAppUserRepo repo)
    : IAppRequestHandler<GetMeServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(GetMeServiceRequest request, CancellationToken ct)
    {
        var user = await repo.GetAsync(request.Id, ct);
        return user is null ? new NoData() : new Success<AppUserMe>(user);
    }
}
