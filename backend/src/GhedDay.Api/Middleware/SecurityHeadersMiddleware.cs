namespace GhedDay.Api.Middleware;

/// <summary>
/// Baseline security headers for JSON API responses. Frontend CSP/HSTS live in next.config.ts.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _env;

    public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        // Swagger UI needs scripts/styles from its own origin; keep API CSP minimal.
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        if (!_env.IsDevelopment())
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        await _next(context);
    }
}
