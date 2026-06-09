using GhedDay.Api.Auth;
using GhedDay.Domain.Entities;
using GhedDay.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Api.Controllers;

/// <summary>
/// JWT auth (PLAN §2.6). Login issues a short-lived access token (with the <c>business_id</c>
/// claim the tenant middleware reads) plus a rotating refresh token. The Next.js BFF stores the
/// refresh token in an httpOnly cookie and only keeps the access token in memory.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly GhedDayDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        GhedDayDbContext db,
        ITokenService tokens,
        IPasswordHasher<User> passwordHasher,
        ILogger<AuthController> logger)
    {
        _db = db;
        _tokens = tokens;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public sealed record LoginRequest(string Email, string Password);
    public sealed record RefreshRequest(string RefreshToken);
    public sealed record LogoutRequest(string RefreshToken);
    public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (user is null)
            return Unauthorized(new { error = "Invalid credentials." });

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { error = "Invalid credentials." });

        var response = await IssueTokensAsync(user, ct);
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Unauthorized(new { error = "Missing refresh token." });

        var hash = _tokens.HashToken(request.RefreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null || !stored.IsActive)
            return Unauthorized(new { error = "Invalid or expired refresh token." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == stored.UserId, ct);
        if (user is null)
            return Unauthorized(new { error = "Invalid refresh token." });

        // Rotate: revoke the presented token and issue a fresh pair.
        stored.RevokedAt = DateTimeOffset.UtcNow;
        var response = await IssueTokensAsync(user, ct);
        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var hash = _tokens.HashToken(request.RefreshToken);
            var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
            if (stored is not null && stored.RevokedAt is null)
            {
                stored.RevokedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }

        return Ok(new { ok = true });
    }

    private async Task<TokenResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var access = _tokens.CreateAccessToken(user);
        var (raw, hash) = _tokens.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = DateTimeOffset.UtcNow.Add(_tokens.RefreshTokenLifetime),
        });
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Issued tokens for user {UserId}", user.Id);
        return new TokenResponse(access.Value, raw, access.ExpiresAt);
    }
}
