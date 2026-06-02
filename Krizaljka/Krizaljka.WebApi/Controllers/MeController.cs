using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.User.Handlers;
using Krizaljka.Domain.User.Models;
using Krizaljka.WebApi.Auth;
using Krizaljka.WebApi.Models.Me;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Krizaljka.WebApi.Controllers;

[Authorize]
[ApiController]
public sealed class MeController(
    AppDispatcher dispatcher,
    ILogger<MeController> logger): BaseController
{
    private const string BaseRoute = "me";

    [HttpGet]
    [Route(BaseRoute)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var id = GetAuthUserIdFromRequestService.Invoke(User);
        if (!id.HasValue)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.LogCritical("ClaimsPrincipal (HttpContext.User) ID parse failed!");
            }

            return new UnauthorizedObjectResult(null); 
        }

        var result = await dispatcher.DispatchAsync(new GetMeServiceRequest(id.Value), ct);
        
        if (result is Success<AppUserMe> successResult)
        {
            return Ok(new MeResponse(
                successResult.Data.Id,
                successResult.Data.Email,
                successResult.Data.FirstName,
                successResult.Data.LastName,
                successResult.Data.DisplayName));
        }

        return MapResult(result);
    }
}
