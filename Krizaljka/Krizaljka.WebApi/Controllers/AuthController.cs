using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Krizaljka.Domain.Core.Stuff;
using Krizaljka.WebApi.Models.Auth;
using Microsoft.AspNetCore.Authorization;

namespace Krizaljka.WebApi.Controllers;

[Authorize]
[Route("auth")]
[ApiController]
public class AuthController : BaseController
{
    [AllowAnonymous]
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

        // TODO: validate username/password against DB.
        long userId = 7;

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, userId.ToString())
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
}
