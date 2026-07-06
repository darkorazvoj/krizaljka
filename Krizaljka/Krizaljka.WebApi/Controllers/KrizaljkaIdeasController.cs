using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.Idea;
using Krizaljka.Domain.Idea.Handlers;
using Krizaljka.WebApi.Models;
using Krizaljka.WebApi.Models.KrizaljkaIdea;
using Krizaljka.WebApi.PaginationUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Krizaljka.WebApi.Controllers;

[Authorize]
[ApiController]
public class KrizaljkaIdeasController(AppDispatcher dispatcher) : BaseController
{
    private const string BaseRute = "ideas";

    [Route(BaseRute)]
    [HttpPost]
    public async Task<IActionResult> InsertAsync(
        [FromBody] InsertKrizaljkaIdeaRequest? request,
        CancellationToken ct)
    {
        if (request is null)
        {
            return BadRequestBodyMissing();
        }

        var result = await dispatcher.DispatchAsync(new InsertKrizaljkaIdeaServiceRequest(
            request.ThemeName,
            request.TemplateRows,
            request.TemplateCols, request.TemplateZeroBlocksNum,
            request.MaxSolveMinutesPerTemplate,
            request.MaxNumOfCompletedTemplates), ct);

        return MapResult<string>(result);
    }

    [Route(BaseRute)]
    [HttpGet]
    public async Task<IActionResult> GetPaginatedListAsync([FromQuery] string? pg, CancellationToken ct)
    {
        var paginationCore = PaginationParser.Parse(pg);
        var result =
            await dispatcher.DispatchAsync(new GetKrizaljkaIdeaPaginatedListServiceRequest(paginationCore), ct);

        if (result is Success<PaginatedResult<List<KrizaljkaIdeaListItem>>> successResult)
        {
            var list = successResult.Data.List
                .Select(x => new KrizaljkaIdeaListItemResponse(
                    x.Id,
                    x.Status,
                    x.ThemeName,
                    x.CreatedById,
                    x.Changestamp))
                .ToList();

            return Ok(new PaginationOffsetResponse<List<KrizaljkaIdeaListItemResponse>>(
                list,
                successResult.Data.TotalRows));
        }

        return MapResult(result);
    }

    [Route(BaseRute + "/{id}")]
    [HttpGet]
    public async Task<IActionResult> GetAsync([FromRoute] string id, CancellationToken ct)
    {
        var result = await dispatcher.DispatchAsync(new GetKrizaljkaIdeaConfigServiceRequest(id), ct);

        if (result is Success<KrizaljkaIdeaConfig> successResult)
        {
            var idea = successResult.Data;
            return Ok(new KrizaljkaIdeaConfigResponse(
                idea.Id,
                (int)idea.Status,
                idea.ThemeName,
                idea.TemplateRows,
                idea.TemplateCols,
                idea.TemplateZeroBlocksNum,
                idea.MinutesPerTemplate,
                idea.MaxNumOfCompletedTemplates,
                idea.CreatedById,
                idea.CreatedOn,
                idea.Changestamp));
        }

        return MapResult(result);
    }
}
