using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.TermDescription;
using Krizaljka.Domain.TermDescription.Handlers;
using Krizaljka.WebApi.Models.TermDescription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Krizaljka.WebApi.Controllers;

[Authorize]
[ApiController]
public class TermDescriptionsController(AppDispatcher dispatcher) : BaseController
{
    private const string BaseRute = "term-descriptions";

    [Route(BaseRute)]
    [HttpPost]
    public async Task<IActionResult> InsertAsync(
        [FromBody] InsertTermDescriptionRequest? request,
        CancellationToken ct)
    {
        if (request is null)
        {
            return BadRequestBodyMissing();
        }

        if (!request.TermId.HasValue)
        {
            return BadRequest("missing_term_id");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest("missing_description");
        }

        var result =
            await dispatcher.DispatchAsync(new InsertTermDescriptionServiceRequest(request.TermId, request.Description),
                ct);

        return MapResult<long>(result);
    }

    [Route(BaseRute + "/{id:long}")]
    [HttpGet]
    public async Task<IActionResult> GetAsync([FromRoute] long id, CancellationToken ct)
    {
        var result = await dispatcher.DispatchAsync(new GetTermDescriptionServiceRequest(id), ct);

        if (result is Success<TermDescription> successResult)
        {
            var serviceObj = successResult.Data;
            return Ok(new TermDescriptionResponse(
                serviceObj.Id,
                serviceObj.TermId,
                serviceObj.Description,
                serviceObj.BatchId,
                serviceObj.CreatedById,
                serviceObj.CreatedOn,
                serviceObj.Changestamp));
        }

        return MapResult(result);
    }
}