using System.Net.Http.Headers;
using System.Net.Http.Json;
using GhedDay.Infrastructure.AI.Models;
using GhedDay.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace GhedDay.Infrastructure.AI;

/// <summary>
/// Typed HttpClient for the Anthropic Messages API. Sets the required headers
/// (<c>x-api-key</c>, <c>anthropic-version</c>) and (de)serializes the wire format.
/// </summary>
public sealed class ClaudeHttpClient : IClaudeClient
{
    private readonly HttpClient _http;
    private readonly AnthropicOptions _options;

    public ClaudeHttpClient(HttpClient http, IOptions<AnthropicOptions> options)
    {
        _options = options.Value;
        _http = http;
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(_options.BaseUrl);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            _http.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", _options.ApiKey);
        _http.DefaultRequestHeaders.TryAddWithoutValidation("anthropic-version", _options.ApiVersion);
    }

    public async Task<ClaudeResponse> CreateMessageAsync(ClaudeRequest request, CancellationToken ct = default)
    {
        request.Model = string.IsNullOrWhiteSpace(request.Model) ? _options.Model : request.Model;

        using var response = await _http.PostAsJsonAsync("/v1/messages", request, ClaudeJson.Options, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ClaudeResponse>(ClaudeJson.Options, ct);
        return result ?? throw new InvalidOperationException("Anthropic returned an empty response.");
    }
}
