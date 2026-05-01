using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Krizaljka.WebApi.Controllers;

[Route("")]
[ApiController]
public class CsrfController(IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("csrf")]
    public IActionResult GetCsrfToken() =>
        Ok(new { csrfToken = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
}
