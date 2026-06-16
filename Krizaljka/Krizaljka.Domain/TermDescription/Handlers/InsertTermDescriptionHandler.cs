using Krizaljka.Domain.Core.Stuff;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;

namespace Krizaljka.Domain.TermDescription.Handlers;


public record InsertTermDescriptionServiceRequest(
    long? TermId,
    string? Description) : IServiceRequest;

internal class InsertTermDescriptionHandler(
    IAuthUser authUser,
    InsertTermDescriptionService insertTermDescriptionService) : IAppRequestHandler<InsertTermDescriptionServiceRequest>
{
    public Task<IServiceResult> HandleAsync(InsertTermDescriptionServiceRequest request, CancellationToken ct) =>
        insertTermDescriptionService.InvokeAsync(request.TermId, request.Description, null, authUser.Id, ct);

}
