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
            request.LanguageId,
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
                    x.CreatedOn,
                    x.Changestamp))
                .ToList();

            return Ok(new PaginationOffsetResponse<List<KrizaljkaIdeaListItemResponse>>(
                list,
                successResult.Data.TotalRows));
        }

        return MapResult(result);
    }

    [Route(BaseRute + "/{id}/terms")]
    [HttpGet]
    public async Task<IActionResult> GetIdeaTermsPaginatedListAsync(
        [FromRoute] string? id, 
        [FromQuery] string? pg,
        CancellationToken ct)
    {
        var paginationCore = PaginationParser.Parse(pg);
        var result =
            await dispatcher.DispatchAsync(new GetKrizaljkaIdeaTermsPaginatedListServiceRequest(id, paginationCore), ct);

        if (result is Success<PaginatedResult<List<KrizaljkaIdeaTermListItem>>> successResult)
        {
            var list = successResult.Data.List
                .Select(x => new KrizaljkaIdeaTermListItemResponse(
                    x.TermType,
                    x.Id,
                    x.TermId,
                    x.TermRawValue,
                    x.TermLength,
                    x.TermIsActive))
                .ToList();

            return Ok(new PaginationOffsetResponse<List<KrizaljkaIdeaTermListItemResponse>>(
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
                (int)idea.LanguageId,
                (int)idea.Status,
                idea.ThemeName,
                idea.TemplateRows,
                idea.TemplateCols,
                idea.TemplateZeroBlocksNum,
                idea.MinutesPerTemplate,
                idea.MaxNumOfCompletedTemplates,
                idea.ThemeTermsCount,
                idea.OtherTermsCount,
                idea.TemplateIdsOnlyCount,
                idea.TemplateIdsExcludedCount,
                idea.CreatedById,
                idea.CreatedOn,
                idea.Changestamp));
        }

        return MapResult(result);
    }

    [Route(BaseRute + "/{id}/config")]
    [HttpPut]
    public async Task<IActionResult> UpdateIdeaConfigAsync(
        [FromRoute] string id,
        [FromBody] UpdateIdeaRequest? request,
        CancellationToken ct)
    {
        if (request is null)
        {
            return BadRequestBodyMissing();
        }

        return MapResult<string>(await dispatcher.DispatchAsync(new UpdateKrizaljkaIdeaConfigServiceRequest(
                id,
                request.LanguageId,
                request.ThemeName,
                request.TemplateRows,
                request.TemplateCols,
                request.TemplateZeroBlocksNum,
                request.MinutesPerTemplate,
                request.MaxNumOfCompletedTemplates,
                request.Changestamp),
            ct));
    }

    [Route(BaseRute + "/{id}/item-id")]
    [HttpPut]
    public async Task<IActionResult> AddItemIdAsync(
        [FromRoute] string id,
        [FromBody] AddIdeaItemIdRequest? request,
        CancellationToken ct)
    {
        if (request is null)
        {
            return BadRequestBodyMissing();
        }

        return MapResult<string>(await dispatcher.DispatchAsync(new AddIdKrizaljkaIdeaServiceRequest(
                id,
                request.ColumnName,
                request.NewId,
                request.Changestamp),
            ct));
    }

    [Route(BaseRute + "/{id}/item-id")]
    [HttpDelete]
    public async Task<IActionResult> RemoveItemIdAsync(
        [FromRoute] string id,
        [FromBody] RemoveIdeaItemIdRequest? request,
        CancellationToken ct)
    {
        if (request is null)
        {
            return BadRequestBodyMissing();
        }

        return MapResult<string>(await dispatcher.DispatchAsync(new RemoveIdKrizaljkaIdeaServiceRequest(
                id,
                request.ColumnName,
                request.ItemId,
                request.Changestamp),
            ct));
    }
}
