using GhedDay.Application.Services;
using GhedDay.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace GhedDay.Infrastructure.Messaging;

public sealed class TwilioSmsService : ISmsService
{
    private readonly TwilioOptions _options;

    public TwilioSmsService(IOptions<TwilioOptions> options)
    {
        _options = options.Value;
        if (!string.IsNullOrWhiteSpace(_options.AccountSid) && !string.IsNullOrWhiteSpace(_options.AuthToken))
            TwilioClient.Init(_options.AccountSid, _options.AuthToken);
    }

    public async Task SendAsync(string toE164, string fromE164, string body, CancellationToken ct = default)
    {
        await MessageResource.CreateAsync(
            to: new PhoneNumber(toE164),
            from: new PhoneNumber(fromE164),
            body: body);
    }
}
