using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace Krizaljka.WebApi.Controllers;

public abstract class BaseController : Controller
{
    protected IActionResult MapResult<T>(IServiceResult? serviceResult) =>
        serviceResult switch
        {
            Success<T> successData => Ok(successData.Data),
            SuccessInsert<T> successInsert => Created(successInsert.Id?.ToString(), null),
            UpdateSuccessChangestamp<T> updateSuccessChangestamp => Ok(new
                { changestamp = updateSuccessChangestamp.Changestamp }),
            InvalidRequestWithReason invalidRequestWithReason => BadRequest(new
                { error = invalidRequestWithReason.Error }),
            InvalidChangestamp => StatusCode(428, null),
            RecordExists => Conflict(new ErrorDto("record exists")),
            NoData => NotFound(null),
            ValidationErrors ve => BadRequest(ve),
            Error er => StatusCode(500, er.Message),
            _ => StatusCode(500)
        };

    protected IActionResult MapResult(IServiceResult? serviceResult) => MapResult<object>(serviceResult);

    internal static IActionResult BadRequestBodyMissing() => new BadRequestObjectResult("Request body is missing");

    internal static IActionResult BadRequestMissingParameters() =>
        new BadRequestObjectResult("Missing parameters in request.");

}
