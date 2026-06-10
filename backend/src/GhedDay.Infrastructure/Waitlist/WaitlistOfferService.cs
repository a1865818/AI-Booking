using GhedDay.Application.Common;
using GhedDay.Application.Bookings;
using GhedDay.Application.DTOs;
using GhedDay.Application.Services;
using GhedDay.Application.Verticals;
using GhedDay.Application.Waitlist;
using GhedDay.Domain.Enums;
using GhedDay.Infrastructure.AI.Tools;
using GhedDay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhedDay.Infrastructure.Waitlist;

public sealed class WaitlistOfferService : IWaitlistOfferService
{
    private static readonly HashSet<string> AcceptKeywords =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "YES", "Y", "OK", "OKAY", "ACCEPT", "CONFIRM", "CÓ", "CO", "VÂNG", "VANG", "ĐỒNG Ý", "DONG Y",
        };

    private static readonly TimeSpan OfferDuration = TimeSpan.FromMinutes(15);

    private readonly GhedDayDbContext _db;
    private readonly IQueryFilterDisabler _filterDisabler;
    private readonly IWaitlistRepository _waitlist;
    private readonly IBookingRepository _bookings;
    private readonly IVerticalConfigService _verticalConfig;
    private readonly ISmsService _sms;
    private readonly INotificationService _notifications;
    private readonly ILogger<WaitlistOfferService> _logger;

    public WaitlistOfferService(
        GhedDayDbContext db,
        IQueryFilterDisabler filterDisabler,
        IWaitlistRepository waitlist,
        IBookingRepository bookings,
        IVerticalConfigService verticalConfig,
        ISmsService sms,
        INotificationService notifications,
        ILogger<WaitlistOfferService> logger)
    {
        _db = db;
        _filterDisabler = filterDisabler;
        _waitlist = waitlist;
        _bookings = bookings;
        _verticalConfig = verticalConfig;
        _sms = sms;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task TryOfferReleasedSlotAsync(
        Guid businessId,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        int? partySize,
        Guid? offeringId,
        CancellationToken ct = default)
    {
        var business = await LoadBusinessAsync(businessId, ct);
        if (business is null)
            return;

        var tz = CheckAvailabilityTool.ResolveTimeZone(business.Timezone);
        var localStart = TimeZoneInfo.ConvertTime(slotStart, tz);
        var preferredDate = DateOnly.FromDateTime(localStart.DateTime);
        var requiredCapacity = partySize is > 0 ? partySize.Value : 1;

        var next = await _waitlist.GetNextWaitingAsync(
            businessId, preferredDate, requiredCapacity, offeringId, ct);
        if (next is null)
            return;

        var expiresAt = DateTimeOffset.UtcNow.Add(OfferDuration);
        if (!await _waitlist.OfferSlotAsync(businessId, next.Id, slotStart, expiresAt, ct))
            return;

        var customer = await LoadCustomerAsync(businessId, next.CustomerId, ct);
        if (customer is null || string.IsNullOrWhiteSpace(business.TwilioNumber))
            return;

        var localTime = TimeZoneInfo.ConvertTime(slotStart, tz);
        var body =
            $"Good news — a slot opened at {localTime:ddd h:mm tt}. Reply YES within 15 min to book it. " +
            $"Tin vui — có chỗ lúc {localTime:ddd HH:mm}. Trả lời YES trong 15 phút để giữ chỗ.";

        try
        {
            await _sms.SendAsync(customer.PhoneE164, business.TwilioNumber, body, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send waitlist offer SMS for entry {EntryId}.", next.Id);
        }
    }

    public async Task<bool> TryAcceptOfferReplyAsync(
        Guid businessId,
        Guid customerId,
        string inboundBody,
        CancellationToken ct = default)
    {
        if (!AcceptKeywords.Contains(inboundBody.Trim()))
            return false;

        var offer = await _waitlist.GetActiveOfferForCustomerAsync(businessId, customerId, ct);
        if (offer?.OfferedSlotTime is not { } slotStart)
            return false;

        var business = await LoadBusinessAsync(businessId, ct);
        if (business is null)
            return false;

        var config = _verticalConfig.GetConfig(business);
        var partySize = offer.PartySize;
        var requiredCapacity = partySize is > 0 ? partySize.Value : 1;
        var durationMinutes = await ResolveDurationAsync(businessId, offer.OfferingId, config, ct);
        var end = slotStart.AddMinutes(durationMinutes);

        var depositRequired = _verticalConfig.RequiresDeposit(business, partySize);
        var initialStatus = depositRequired ? BookingStatus.PendingDeposit : BookingStatus.Confirmed;

        var hold = await _bookings.CreateBookingHoldAsync(new CreateBookingHoldRequest
        {
            BusinessId = businessId,
            CustomerId = customerId,
            OfferingId = offer.OfferingId,
            Start = slotStart,
            End = end,
            RequiredCapacity = requiredCapacity,
            PartySize = partySize,
            HoldDuration = TimeSpan.FromMinutes(config.HoldMinutes),
            InitialStatus = initialStatus,
        }, ct);

        if (!hold.Success)
        {
            await _waitlist.MarkExpiredAsync(businessId, offer.Id, ct);
            await TryOfferReleasedSlotAsync(businessId, slotStart, end, partySize, offer.OfferingId, ct);

            var customer = await LoadCustomerAsync(businessId, customerId, ct);
            if (customer is not null && !string.IsNullOrWhiteSpace(business.TwilioNumber))
            {
                await _sms.SendAsync(
                    customer.PhoneE164,
                    business.TwilioNumber,
                    "Sorry, that slot was just taken. We'll text you if another opens. / Xin lỗi, chỗ vừa hết. Chúng tôi sẽ nhắn nếu có thêm.",
                    ct);
            }

            return true;
        }

        if (!await _waitlist.MarkBookedAsync(businessId, offer.Id, ct))
        {
            _logger.LogWarning("Waitlist entry {EntryId} could not be marked booked.", offer.Id);
        }

        await _notifications.BookingCreatedAsync(businessId, new BookingDto(
            hold.BookingId!.Value, customerId, offer.OfferingId, hold.ResourceId,
            slotStart, end, partySize, initialStatus, hold.HoldExpiresAt), ct);

        if (initialStatus == BookingStatus.Confirmed)
        {
            await _notifications.BookingStatusChangedAsync(
                businessId, hold.BookingId.Value, initialStatus.ToString(), ct);
        }

        var confirmCustomer = await LoadCustomerAsync(businessId, customerId, ct);
        if (confirmCustomer is not null && !string.IsNullOrWhiteSpace(business.TwilioNumber))
        {
            var tz = CheckAvailabilityTool.ResolveTimeZone(business.Timezone);
            var local = TimeZoneInfo.ConvertTime(slotStart, tz);
            var msg = initialStatus == BookingStatus.Confirmed
                ? $"You're booked for {local:ddd h:mm tt}. See you then! / Đã đặt {local:ddd HH:mm}. Hẹn gặp bạn!"
                : $"Slot held for {local:ddd h:mm tt}. Complete your deposit to confirm. / Đã giữ chỗ {local:ddd HH:mm}. Vui lòng thanh toán cọc để xác nhận.";

            await _sms.SendAsync(confirmCustomer.PhoneE164, business.TwilioNumber, msg, ct);
        }

        return true;
    }

    public async Task ProcessExpiredOffersAsync(CancellationToken ct = default)
    {
        var expired = await _waitlist.ExpireTimedOutOffersAsync(ct);
        foreach (var row in expired)
        {
            if (row.OfferedSlotTime is not { } slotStart)
                continue;

            var config = await LoadBusinessConfigAsync(row.BusinessId, ct);
            var duration = config?.DefaultDurationMinutes ?? 60;
            await TryOfferReleasedSlotAsync(
                row.BusinessId,
                slotStart,
                slotStart.AddMinutes(duration),
                row.PartySize,
                row.OfferingId,
                ct);
        }
    }

    private async Task<Domain.Entities.Business?> LoadBusinessAsync(Guid businessId, CancellationToken ct) =>
        await _filterDisabler.RunWithoutTenantFilterAsync(
            () => _db.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == businessId, ct), ct);

    private async Task<Domain.Entities.Customer?> LoadCustomerAsync(Guid businessId, Guid customerId, CancellationToken ct) =>
        await _filterDisabler.RunWithoutTenantFilterAsync(
            () => _db.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == customerId && c.BusinessId == businessId, ct), ct);

    private async Task<Domain.ValueObjects.VerticalConfig?> LoadBusinessConfigAsync(Guid businessId, CancellationToken ct)
    {
        var business = await LoadBusinessAsync(businessId, ct);
        return business?.GetVerticalConfig();
    }

    private async Task<int> ResolveDurationAsync(
        Guid businessId,
        Guid? offeringId,
        Domain.ValueObjects.VerticalConfig config,
        CancellationToken ct)
    {
        if (offeringId is { } id)
        {
            var duration = await _filterDisabler.RunWithoutTenantFilterAsync(
                () => _db.Offerings.AsNoTracking()
                    .Where(o => o.Id == id && o.BusinessId == businessId)
                    .Select(o => (int?)o.DurationMinutes)
                    .FirstOrDefaultAsync(ct), ct);
            if (duration is > 0)
                return duration.Value;
        }

        return config.DefaultDurationMinutes ?? 60;
    }
}
