using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Terms.Handlers;

public record GetTermsPaginatedListServiceRequest(IPaginationCore PaginationCore) : IServiceRequest;


internal class GetTermsPaginatedListHandler(
    ITermRepo repo,
    ILogger<GetTermsPaginatedListHandler> logger)
    : IAppRequestHandler<GetTermsPaginatedListServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(GetTermsPaginatedListServiceRequest request, CancellationToken ct)
    {
        try
        {
            var list = await repo.GetListAsync(request.PaginationCore, ct);
            return new Success<PaginatedResult<List<TermListItem>>>(list);
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
