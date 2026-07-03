using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Idea.Handlers;
using Krizaljka.WebApi.Models.KrizaljkaIdea;
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
}
