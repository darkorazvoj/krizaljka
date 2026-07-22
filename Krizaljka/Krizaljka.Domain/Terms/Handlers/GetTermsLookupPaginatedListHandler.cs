using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Terms.Handlers;

public record GetTermsLookupPaginatedListServiceRequest(IPaginationCore PaginationCore) : IServiceRequest;


internal class GetTermsLookupPaginatedListHandler(
    ITermRepo repo,
    ILogger<GetTermsLookupPaginatedListHandler> logger)
    : IAppRequestHandler<GetTermsLookupPaginatedListServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(GetTermsLookupPaginatedListServiceRequest request, CancellationToken ct)
    {
        try
        {
            var list = await repo.GetLookupListAsync(request.PaginationCore, ct);
            return new Success<PaginatedResult<List<TermLookupItem>>>(list);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Get terms failed");
            }

            return new Error(string.Empty);
        }
    }
}
