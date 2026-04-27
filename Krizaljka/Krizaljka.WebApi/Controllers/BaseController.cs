using Krizaljka.Domain.Core.Stuff.Services;
using Microsoft.AspNetCore.Mvc;

namespace Krizaljka.WebApi.Controllers;

public abstract class BaseController : Controller
{
    protected IActionResult MapResult<T>(IServiceResult? serviceResult) =>
        serviceResult switch
        {
            Success<T> successData => Ok(successData.Data),
            SuccessInsert<T> successInsert => Created(successInsert.Id?.ToString(), null),
            InvalidRequestWithReason invalidRequestWithReason => BadRequest(new { error = invalidRequestWithReason.Error }),
            NoData => NotFound(null),
            Error er => StatusCode(500, er.Message),
            _ => StatusCode(500)
        };

    protected IActionResult MapResult(IServiceResult? serviceResult) => MapResult<object>(serviceResult);


    internal static IActionResult BadRequestBodyMissing() => new BadRequestObjectResult("Request body is missing");

    internal static IActionResult BadRequestMissingParameters() =>
        new BadRequestObjectResult("Missing parameters in request.");

}
