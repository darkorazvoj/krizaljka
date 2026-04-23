using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Template.Handlers;
using Krizaljka.WebApi.Models.KrizaljkaTemplate;
using Microsoft.AspNetCore.Mvc;

namespace Krizaljka.WebApi.Controllers;

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
}
