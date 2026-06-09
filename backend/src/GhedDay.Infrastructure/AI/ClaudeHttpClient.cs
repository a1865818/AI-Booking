using System.Net.Http.Headers;
using GhedDay.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace GhedDay.Infrastructure.AI;

/// <summary>
/// Typed HttpClient for the Anthropic Messages API. Sets the required headers
/// (<c>x-api-key</c>, <c>anthropic-version</c>) on every request.
/// </summary>
public sealed class ClaudeHttpClient
{
    private readonly HttpClient _http;
    private readonly AnthropicOptions _options;

    public ClaudeHttpClient(HttpClient http, IOptions<AnthropicOptions> options)
    {
        _options = options.Value;
        _http = http;
        _http.BaseAddress = new Uri(_options.BaseUrl);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            _http.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", _options.ApiKey);
        _http.DefaultRequestHeaders.TryAddWithoutValidation("anthropic-version", _options.ApiVersion);
    }

    /// <summary>POSTs a raw Messages API request body and returns the response JSON. (Phase 2.)</summary>
    public async Task<string> SendMessagesAsync(string requestJson, CancellationToken ct = default)
    {
        using var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync("/v1/messages", content, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
}
