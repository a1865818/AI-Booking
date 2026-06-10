using GhedDay.Application.Bookings;
using GhedDay.Application.Common;
using GhedDay.Application.Services;
using GhedDay.Application.Waitlist;
using GhedDay.Infrastructure.AI;
using GhedDay.Infrastructure.AI.Models;
using GhedDay.Infrastructure.AI.Tools;
using GhedDay.Infrastructure.Configuration;
using GhedDay.Infrastructure.Data;
using GhedDay.Infrastructure.Data.QueryFilters;
using GhedDay.Infrastructure.Data.Repositories;
using GhedDay.Infrastructure.Jobs;
using GhedDay.Infrastructure.Messaging;
using GhedDay.Infrastructure.Payments;
using GhedDay.Infrastructure.Waitlist;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GhedDay.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

        services.AddDbContext<GhedDayDbContext>(options => options.UseNpgsql(connectionString));

        services.AddSingleton<IDbConnectionFactory>(_ => new NpgsqlConnectionFactory(connectionString));
        services.AddScoped<IQueryFilterDisabler, QueryFilterDisabler>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IWaitlistRepository, WaitlistRepository>();
        services.AddScoped<IWaitlistOfferService, WaitlistOfferService>();

        services.Configure<AnthropicOptions>(configuration.GetSection(AnthropicOptions.SectionName));
        services.Configure<TwilioOptions>(configuration.GetSection(TwilioOptions.SectionName));
        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));

        services.AddScoped<ISmsService, TwilioSmsService>();
        services.AddScoped<StripeService>();
        services.AddScoped<StripeConnectService>();

        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient<IClaudeClient, ClaudeHttpClient>();
        services.AddScoped<ClaudeRequestBuilder>();
        services.AddScoped<IClaudeTool, GetOfferingsTool>();
        services.AddScoped<IClaudeTool, GetBusinessHoursTool>();
        services.AddScoped<IClaudeTool, CheckAvailabilityTool>();
        services.AddScoped<IClaudeTool, CreateBookingHoldTool>();
        services.AddScoped<IClaudeTool, HandleCancellationTool>();
        services.AddScoped<IClaudeTool, JoinWaitlistTool>();
        services.AddScoped<IClaudeToolHandler, ClaudeToolHandler>();
        services.AddScoped<IConversationContextStore, ConversationContextStore>();
        services.AddScoped<IConversationOrchestrator, ClaudeConversationOrchestrator>();

        services.AddScoped<ProcessedEventCleanupJob>();
        services.AddScoped<HoldExpiryJob>();
        services.AddScoped<ReminderJob>();
        services.AddScoped<WaitlistOfferTimeoutJob>();
        services.AddScoped<NoShowJob>();

        return services;
    }
}
