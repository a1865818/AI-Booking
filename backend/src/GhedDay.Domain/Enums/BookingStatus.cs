namespace GhedDay.Domain.Enums;

/// <summary>
/// Booking lifecycle. Transitions are guarded; pending_deposit → confirmed happens
/// via the Stripe webhook only.
/// </summary>
public enum BookingStatus
{
    PendingDeposit,
    Confirmed,
    Completed,
    Cancelled,
    NoShow
}
