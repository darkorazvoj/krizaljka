using Krizaljka.Domain.Core.Stuff;
using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.User.Handlers;
using Krizaljka.WebApi.Models;
using Krizaljka.WebApi.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.WebApi.Csrf;

namespace Krizaljka.WebApi.Controllers;

[Authorize]
[Route("auth")]
[ApiController]
public class AuthController(AppDispatcher dispatcher) : BaseController
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] KrLoginRequest? request,
        CancellationToken ct)
    {
        if (request is null)
        {
            return BadRequestBodyMissing();
        }

        // Block logged-in users
        if (User.Identity?.IsAuthenticated == true)
        {
            return StatusCode(StatusCodes.Status403Forbidden, null);
        }

        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ErrorDto("credentials_required"));
        }

        var result =
            await dispatcher.DispatchAsync(new GetUserByUsernameServiceRequest(request.Username, request.Password), ct);
        
        if (result is not Success<long> successResult)
        {
            return BadRequest(new ErrorDto("invalid_credentials"));
        }

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, successResult.Data.ToString())
        ];

        ClaimsIdentity identity = new(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        ClaimsPrincipal principal = new(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        return Ok();
    }
    
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }
    
    [HttpGet("me")]
    public IActionResult Me(IAuthUser user) =>
        Ok(new
        {
            user.Id
        });

    public IActionResult AuthServiceUnavailableResult =>
        StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "service_unavailable" });

    public IActionResult ServerErrorResult =>
        StatusCode(StatusCodes.Status500InternalServerError, new { error = "server_error" });
}
