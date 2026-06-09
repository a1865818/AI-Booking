using Dapper;
using GhedDay.Application.Verticals;
using GhedDay.Domain.Entities;
using GhedDay.Domain.Enums;
using GhedDay.Domain.ValueObjects;
using GhedDay.Infrastructure.AI;
using GhedDay.Infrastructure.AI.Tools;
using GhedDay.Infrastructure.Configuration;
using GhedDay.Infrastructure.Data;
using GhedDay.Infrastructure.Data.Repositories;
using GhedDay.Infrastructure.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace GhedDay.Infrastructure.Tests;

/// <summary>
/// End-to-end SMS → booking acceptance (PLAN §2 "Done when"). A scripted Claude drives the real
/// tool loop against Postgres, proving the nail-salon and restaurant flows create correctly
/// scoped holds with the right party size / offering.
/// </summary>
[Collection("postgres")]
public sealed class SmsBookingAcceptanceTests
{
    private readonly PostgresFixture _pg;
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 9, 0, 0, TimeSpan.Zero);

    public SmsBookingAcceptanceTests(PostgresFixture pg) => _pg = pg;

    [SkippableFact]
    public async Task Restaurant_books_a_table_for_the_requested_party_size()
    {
        Skip.IfNot(_pg.Available, "No Postgres available.");

        var businessId = Guid.NewGuid();
        await using var db = NewDbContext(businessId);

        var config = new VerticalConfig
        {
            ResourceLabel = "Table",
            ResourceLabelPlural = "Tables",
            DepositRequired = false,
            DepositThresholdPartySize = 6,
            DepositPerHeadCents = 1000,
            DefaultDurationMinutes = 90,
        };
        var (customerId, conversationId, fourTopId) = await SeedAsync(
            db, businessId, BusinessType.Restaurant, config, "+15550001111",
            capacities: [(4, "Table 4"), (8, "Table 8")], offering: null, inbound: "Table for 4 tomorrow at 7pm");

        try
        {
            var sms = new CapturingSmsService();
            var claude = new ScriptedClaudeClient(
                ScriptedClaudeClient.ToolUse("check_availability", "t1", new { date = "2030-01-02", party_size = 4 }),
                ScriptedClaudeClient.ToolUse("create_booking_hold", "t2", new { slot_iso = "2030-01-02T19:00:00+00:00", party_size = 4 }),
                ScriptedClaudeClient.EndTurn("Booked! See you then."));

            var orchestrator = BuildOrchestrator(db, claude, sms);
            await orchestrator.HandleInboundMessageAsync(businessId, conversationId, "Table for 4 tomorrow at 7pm");

            var booking = await QueryFirstBookingAsync(businessId)
                ?? throw new Xunit.Sdk.XunitException("No booking created for restaurant.");
            Assert.Equal(4, booking.PartySize);
            Assert.Equal("PendingDeposit", booking.Status);
            Assert.Equal(fourTopId, booking.ResourceId); // smallest sufficient table
            Assert.Contains(sms.Sent, m => m.Body == "Booked! See you then." && m.From == "+15550001111");
        }
        finally
        {
            await _pg.CleanupAsync(businessId);
        }
    }

    [SkippableFact]
    public async Task Nail_salon_books_a_service_with_a_deposit_hold()
    {
        Skip.IfNot(_pg.Available, "No Postgres available.");

        var businessId = Guid.NewGuid();
        await using var db = NewDbContext(businessId);

        var config = new VerticalConfig
        {
            ResourceLabel = "Chair",
            ResourceLabelPlural = "Chairs",
            DepositRequired = true,
            HoldMinutes = 15,
        };
        var offeringId = Guid.NewGuid();
        var (customerId, conversationId, chairId) = await SeedAsync(
            db, businessId, BusinessType.NailSalon, config, "+15550002222",
            capacities: [(1, "Chair 1")],
            offering: (offeringId, "Gel Manicure", 45),
            inbound: "Can I get a gel manicure tomorrow morning?");

        try
        {
            var sms = new CapturingSmsService();
            var claude = new ScriptedClaudeClient(
                ScriptedClaudeClient.ToolUse("check_availability", "t1", new { offering_id = offeringId.ToString(), date = "2030-01-02" }),
                ScriptedClaudeClient.ToolUse("create_booking_hold", "t2", new { offering_id = offeringId.ToString(), slot_iso = "2030-01-02T10:00:00+00:00" }),
                ScriptedClaudeClient.EndTurn("You're booked for 10am — a deposit link will follow."));

            var orchestrator = BuildOrchestrator(db, claude, sms);
            await orchestrator.HandleInboundMessageAsync(businessId, conversationId, "Can I get a gel manicure tomorrow morning?");

            var booking = await QueryFirstBookingAsync(businessId)
                ?? throw new Xunit.Sdk.XunitException("No booking created for nail salon.");
            Assert.Equal("PendingDeposit", booking.Status);
            Assert.Equal(offeringId, booking.OfferingId);
            Assert.Equal(chairId, booking.ResourceId);
            Assert.Null(booking.PartySize);
            Assert.NotEmpty(sms.Sent);
        }
        finally
        {
            await _pg.CleanupAsync(businessId);
        }
    }

    private GhedDayDbContext NewDbContext(Guid businessId)
    {
        var options = new DbContextOptionsBuilder<GhedDayDbContext>()
            .UseNpgsql(_pg.ConnectionString)
            .Options;
        return new GhedDayDbContext(options, new TenantStub(businessId));
    }

    private ClaudeConversationOrchestrator BuildOrchestrator(
        GhedDayDbContext db, ScriptedClaudeClient claude, CapturingSmsService sms)
    {
        var vertical = new VerticalConfigService();
        var clock = new FixedTimeProvider(Now);
        var bookingRepo = new BookingRepository(_pg.ConnectionFactory);
        var notifications = new NullNotificationService();

        var tools = new IClaudeTool[]
        {
            new GetOfferingsTool(db),
            new CheckAvailabilityTool(db, vertical, clock),
            new CreateBookingHoldTool(db, bookingRepo, vertical, notifications),
        };
        var handler = new ClaudeToolHandler(tools, NullLogger<ClaudeToolHandler>.Instance);
        var builder = new ClaudeRequestBuilder(vertical, Options.Create(new AnthropicOptions()), clock);
        var store = new ConversationContextStore(db);

        return new ClaudeConversationOrchestrator(
            claude, builder, handler, store, sms, notifications,
            Options.Create(new AnthropicOptions { MaxIterations = 8 }),
            NullLogger<ClaudeConversationOrchestrator>.Instance);
    }

    private async Task<(Guid CustomerId, Guid ConversationId, Guid FirstResourceId)> SeedAsync(
        GhedDayDbContext db, Guid businessId, BusinessType type, VerticalConfig config, string twilioNumber,
        (int Capacity, string Name)[] capacities, (Guid Id, string Name, int Duration)? offering, string inbound)
    {
        var business = new Business
        {
            Id = businessId,
            Name = $"Test {businessId:N}",
            Slug = businessId.ToString("N"),
            Timezone = "UTC",
            BusinessType = type,
            TwilioNumber = twilioNumber,
        };
        business.SetVerticalConfig(config);
        db.Businesses.Add(business);

        var resourceType = type == BusinessType.Restaurant ? ResourceType.Table : ResourceType.Chair;
        var resourceIds = new List<Guid>();
        for (var i = 0; i < capacities.Length; i++)
        {
            var r = new Resource
            {
                BusinessId = businessId,
                Name = capacities[i].Name,
                ResourceType = resourceType,
                Capacity = capacities[i].Capacity,
                SortOrder = i,
            };
            db.Resources.Add(r);
            resourceIds.Add(r.Id);
        }

        if (offering is { } off)
        {
            db.Offerings.Add(new Offering
            {
                Id = off.Id,
                BusinessId = businessId,
                Name = off.Name,
                DurationMinutes = off.Duration,
            });
        }

        for (var day = 0; day <= 6; day++)
        {
            db.BusinessHours.Add(new BusinessHours
            {
                BusinessId = businessId,
                DayOfWeek = day,
                OpenTime = new TimeOnly(9, 0),
                CloseTime = new TimeOnly(22, 0),
            });
        }

        var customer = new Customer { BusinessId = businessId, PhoneE164 = "+14155550123" };
        db.Customers.Add(customer);

        var conversation = new Conversation { BusinessId = businessId, CustomerId = customer.Id };
        db.Conversations.Add(conversation);

        db.Messages.Add(new Message
        {
            ConversationId = conversation.Id,
            BusinessId = businessId,
            Direction = MessageDirection.Inbound,
            Body = inbound,
        });

        await db.SaveChangesAsync();
        return (customer.Id, conversation.Id, resourceIds[0]);
    }

    private async Task<BookingRow?> QueryFirstBookingAsync(Guid businessId)
    {
        await using var conn = new NpgsqlConnection(_pg.ConnectionString);
        await conn.OpenAsync();
        return await conn.QueryFirstOrDefaultAsync<BookingRow>(
            """
            SELECT "PartySize" AS PartySize, "Status" AS Status, "ResourceId" AS ResourceId, "OfferingId" AS OfferingId
            FROM bookings WHERE "BusinessId" = @businessId;
            """,
            new { businessId });
    }

    private sealed record BookingRow(int? PartySize, string Status, Guid? ResourceId, Guid? OfferingId);
}
