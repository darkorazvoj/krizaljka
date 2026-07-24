using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.Extensions.Logging;

namespace Krizaljka.Domain.Idea.Handlers;

public record GetKrizaljkaIdeaTemplatesPaginatedListServiceRequest(
    string? KrizaljkaIdeaId, 
    IPaginationCore PaginationCore) : IServiceRequest;


internal class GetKrizaljkaIdeaTemplatesPaginatedListHandler(
    IKrizaljkaIdeaRepo repo,
    ILogger<GetKrizaljkaIdeaTemplatesPaginatedListHandler> logger)
    : IAppRequestHandler<GetKrizaljkaIdeaTemplatesPaginatedListServiceRequest>
{
    public async Task<IServiceResult> HandleAsync(GetKrizaljkaIdeaTemplatesPaginatedListServiceRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.KrizaljkaIdeaId))
        {
            return new ValidationErrors(["missing_krizaljka_idea_id"]);
        }

        try
        {
            var list = await repo.GetTemplatesListAsync(request.PaginationCore, [("ideaId", request.KrizaljkaIdeaId)], ct);
            return new Success<PaginatedResult<List<KrizaljkaIdeaTemplateListItem>>>(list);
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e, "Get idea templates failed");
            }

            return new Error(string.Empty);
        }
    }
}
