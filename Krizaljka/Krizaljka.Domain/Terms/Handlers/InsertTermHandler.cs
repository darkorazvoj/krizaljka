using Krizaljka.Domain.Core.Stuff;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;

namespace Krizaljka.Domain.Terms.Handlers;


public record InsertTermServiceRequest(
    TermLanguage? Language,
    string? Description,
    string? Term,
    bool? IsPrivate) : IServiceRequest;

internal class InsertTermHandler(
    IAuthUser authUser,
    InsertTermService insertTermService) : IAppRequestHandler<InsertTermServiceRequest>
{
    public Task<IServiceResult> HandleAsync(InsertTermServiceRequest request, CancellationToken ct) =>
        insertTermService.InvokeAsync(request.Language, request.Description, request.Term, request.IsPrivate ?? false, authUser.Id, ct);

}
