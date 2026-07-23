using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Idea.Handlers;

public record GetKrizaljkaIdeaTermsPaginatedListServiceRequest(string KrizaljkaIdeaId, IPaginationCore PaginationCore) : IServiceRequest;


internal class GetKrizaljkaIdeaTermsPaginatedListHandler(
    IKrizaljkaIdeaRepo repo,
    ILogger<GetKrizaljkaIdeaTermsPaginatedListHandler> logger)
    : IAppRequestHandler<GetKrizaljkaIdeaTermsPaginatedListServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(GetKrizaljkaIdeaTermsPaginatedListServiceRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.KrizaljkaIdeaId))
        {
            return new ValidationErrors(["missing_krizaljka_idea_id"]);
        }

        try
        {
            var list = await repo.GetTermsListAsync(request.PaginationCore, [("ideaId", request.KrizaljkaIdeaId)], ct);
            return new Success<PaginatedResult<List<KrizaljkaIdeaTermListItem>>>(list);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Get idea terms failed");
            }

            return new Error(string.Empty);
        }
    }
}
