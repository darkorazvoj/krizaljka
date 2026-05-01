using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Krizaljka.WebApi.Controllers;

[Route("")]
[ApiController]
public class CsrfController(IAntiforgery antiforgery) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("csrf")]
    public IActionResult GetCsrfToken() =>
        Ok(new { csrfToken = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
}
