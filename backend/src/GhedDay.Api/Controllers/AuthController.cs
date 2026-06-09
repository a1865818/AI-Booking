using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GhedDay.Api.Controllers;

/// <summary>
/// ASP.NET Core Identity auth (Phase 5): login issues a 15-min JWT + 7-day hashed refresh
/// token (httpOnly Secure SameSite=Strict cookie). Endpoints scaffolded here.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    public sealed record LoginRequest(string Email, string Password);

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request) =>
        StatusCode(StatusCodes.Status501NotImplemented, new { message = "Auth is implemented in Phase 5." });

    [HttpPost("refresh")]
    public IActionResult Refresh() =>
        StatusCode(StatusCodes.Status501NotImplemented, new { message = "Auth is implemented in Phase 5." });

    [HttpPost("logout")]
    public IActionResult Logout() =>
        StatusCode(StatusCodes.Status501NotImplemented, new { message = "Auth is implemented in Phase 5." });
}
