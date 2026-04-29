using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Template.Handlers;

public record GetKrizaljkaTemplatesPaginatedListServiceRequest(IPaginationCore PaginationCore) : IServiceRequest;

internal class GetKrizaljkaTemplatesPaginatedListHandler(
    IKrizaljkaTemplateRepo repo,
    ILogger<GetKrizaljkaTemplatesPaginatedListHandler> logger)
    : IAppRequestHandler<GetKrizaljkaTemplatesPaginatedListServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(
        GetKrizaljkaTemplatesPaginatedListServiceRequest request,
        CancellationToken ct)
    {
        try
        {
            var list = await repo.GetListAsync(request.PaginationCore, ct);
            return new Success<PaginatedResult<List<KrizaljkaTemplateListItem>>>(list);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Get Krizaljka template failed");
            }

            return new Error(string.Empty);
        }
    }
}
