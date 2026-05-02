using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.Template;
using Krizaljka.Domain.Template.Handlers;
using Krizaljka.WebApi.Models;
using Krizaljka.WebApi.Models.KrizaljkaTemplate;
using Krizaljka.WebApi.PaginationUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Krizaljka.WebApi.Controllers;

[Authorize]
[ApiController]
public sealed class KrizaljkaTemplatesController(AppDispatcher dispatcher) : BaseController
{
    private const string BaseRute = "templates";

    [Route(BaseRute)]
    [HttpPost]
    public async Task<IActionResult> InsertAsync(
        [FromBody] KrizaljkaTemplatePostRequest? request,
        CancellationToken ct)
    {
        if (request is null)
        {
            return BadRequestBodyMissing();
        }

        var result = await dispatcher.DispatchAsync(new InsertKrizaljkaTemplateServiceRequest(
            request.Matrix,
            request.Name), ct);

        return MapResult<long>(result);
    }

    [Route(BaseRute + "/{id:long}")]
    [HttpGet]
    public async Task<IActionResult> GetAsync([FromRoute] long id, CancellationToken ct)
    {
        var result = await dispatcher.DispatchAsync(new GetKrizaljkaTemplateServiceRequest(id), ct);
        
        if (result is Success<KrizaljkaTemplate> successResult)
        {
            return Ok(new KrizaljkaTemplateResponse(
                successResult.Data.Id,
                successResult.Data.Name,
                successResult.Data.Matrix,
                successResult.Data.RowsCount,
                successResult.Data.ColumnsCount,
                successResult.Data.IsActive,
                successResult.Data.CreatedById,
                successResult.Data.CreatedOn,
                successResult.Data.Changestamp));
        }

        return MapResult(result);
    }

    [Route(BaseRute)]
    [HttpGet]
    public async Task<IActionResult> GetPaginatedListAsync([FromQuery]string? pg, CancellationToken ct)
    {
        var paginationCore = PaginationParser.Parse(pg);
        var result =
            await dispatcher.DispatchAsync(new GetKrizaljkaTemplatesPaginatedListServiceRequest(paginationCore), ct);

        if (result is Success<PaginatedResult<List<KrizaljkaTemplateListItem>>> successResult)
        {
            var list = successResult.Data.List
                .Select(x => new KrizaljkaTemplateListItemResponse(
                    x.Id,
                    x.Name,
                    x.RowsCount,
                    x.ColumnsCount, x.IsActive))
                .ToList();

            return Ok(new PaginationOffsetResponse<List<KrizaljkaTemplateListItemResponse>>(
                list,
                successResult.Data.TotalRows));
        }
        
        return MapResult(result);
    }

    [Route(BaseRute + "/{id:long}/active")]
    [HttpPut]
    public async Task<IActionResult> UpdateActiveAsync(
        [FromRoute] long id, 
        [FromBody] UpdateActiveKrizaljkaTemplateRequest? request,
        CancellationToken ct)
    {
        if (request is null)
        {
            return BadRequestBodyMissing();
        }

        return MapResult<string>(await dispatcher.DispatchAsync(new UpdateIsActiveKrizaljkaTemplateServiceRequest(
                id,
                request.IsActive,
                request.Changestamp),
            ct));
    }
}
