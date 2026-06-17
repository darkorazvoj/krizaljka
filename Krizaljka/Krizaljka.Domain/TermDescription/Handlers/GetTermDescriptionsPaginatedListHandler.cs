using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.TermDescription.Handlers;

public record GetTermsDescriptionsPaginatedListServiceRequest(IPaginationCore PaginationCore) : IServiceRequest;


internal class GetTermDescriptionsPaginatedListHandler(
    ITermDescriptionRepo repo,
    ILogger<GetTermDescriptionsPaginatedListHandler> logger)
    : IAppRequestHandler<GetTermsDescriptionsPaginatedListServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(GetTermsDescriptionsPaginatedListServiceRequest request, CancellationToken ct)
    {
        try
        {
            var list = await repo.GetListAsync(request.PaginationCore, ct);
            return new Success<PaginatedResult<List<TermDescriptionListItem>>>(list);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Get term descriptions failed");
            }

            return new Error(string.Empty);
        }
    }
}
