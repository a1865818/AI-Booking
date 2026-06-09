namespace GhedDay.Application.Services;

/// <summary>Outbound SMS. Implemented by <c>TwilioSmsService</c> in Infrastructure.</summary>
public interface ISmsService
{
    Task SendAsync(string toE164, string fromE164, string body, CancellationToken ct = default);
}
