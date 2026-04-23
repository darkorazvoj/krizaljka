using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.AspNetCore.Mvc;

namespace Krizaljka.WebApi.Controllers;

public abstract class BaseController : Controller
{
    protected IActionResult MapResult<T>(IServiceResult? serviceResult) =>
        serviceResult switch
        {
            SuccessInsert<T> successInsert => Created(successInsert.Id?.ToString(), null),
            InvalidRequestWithReason invalidRequestWithReason => BadRequest(new { error = invalidRequestWithReason.Error }),
            Error er => StatusCode(500, er.Message),
            _ => StatusCode(500)
        };

    internal static IActionResult BadRequestBodyMissing() => new BadRequestObjectResult("Request body is missing");

}
