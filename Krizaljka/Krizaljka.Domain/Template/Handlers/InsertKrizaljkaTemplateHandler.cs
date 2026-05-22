using Krizaljka.Domain.Core.Stuff;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.Template.Services;

namespace Krizaljka.Domain.Template.Handlers;

public record InsertKrizaljkaTemplateServiceRequest(
    int[][]? Matrix,
    string? Name) : IServiceRequest;

internal class InsertKrizaljkaTemplateHandler(
    IAuthUser authUser,
    InsertTemplateService insertTemplateService)
    : IAppRequestHandler<InsertKrizaljkaTemplateServiceRequest>
{
    public Task<IServiceResult> HandleAsync(InsertKrizaljkaTemplateServiceRequest request, CancellationToken ct) =>
        insertTemplateService.InvokeAsync(request.Matrix, request.Name, authUser.Id, ct);
}

