using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Idea.Handlers;

public record GetKrizaljkaIdeaPaginatedListServiceRequest(IPaginationCore PaginationCore) : IServiceRequest;


internal class GetKrizaljkaIdeaPaginatedListHandler(
    IKrizaljkaIdeaRepo repo,
    ILogger<GetKrizaljkaIdeaPaginatedListHandler> logger)
    : IAppRequestHandler<GetKrizaljkaIdeaPaginatedListServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(GetKrizaljkaIdeaPaginatedListServiceRequest request, CancellationToken ct)
    {
        try
        {
            var list = await repo.GetListAsync(request.PaginationCore, ct);
            return new Success<PaginatedResult<List<KrizaljkaIdeaListItem>>>(list);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Get ideas failed");
            }

            return new Error(string.Empty);
        }
    }
}
