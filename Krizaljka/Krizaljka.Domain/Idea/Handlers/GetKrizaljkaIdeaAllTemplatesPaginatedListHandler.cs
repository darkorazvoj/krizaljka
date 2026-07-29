using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Idea.Handlers;

public record GetKrizaljkaIdeaAllTemplatesPaginatedListServiceRequest(
    string? KrizaljkaIdeaId, 
    IPaginationCore PaginationCore) : IServiceRequest;


internal class GetKrizaljkaIdeaAllTemplatesPaginatedListHandler(
    IKrizaljkaIdeaRepo repo,
    ILogger<GetKrizaljkaIdeaAllTemplatesPaginatedListHandler> logger)
    : IAppRequestHandler<GetKrizaljkaIdeaAllTemplatesPaginatedListServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(GetKrizaljkaIdeaAllTemplatesPaginatedListServiceRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.KrizaljkaIdeaId))
        {
            return new ValidationErrors(["missing_krizaljka_idea_id"]);
        }

        try
        {
            var list = await repo.GetAllTemplatesListAsync(request.PaginationCore, [("ideaId", request.KrizaljkaIdeaId)], ct);
            return new Success<PaginatedResult<List<KrizaljkaIdeaAllTemplatesListItem>>>(list);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Get idea ALL templates failed");
            }

            return new Error(string.Empty);
        }
    }
}
