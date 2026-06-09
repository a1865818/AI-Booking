using GhedDay.Domain.Enums;

namespace GhedDay.Application.DTOs;

public sealed record BookingDto(
    Guid Id,
    Guid CustomerId,
    Guid? OfferingId,
    Guid? ResourceId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int? PartySize,
    BookingStatus Status,
    DateTimeOffset? HoldExpiresAt);

public sealed record SlotDto(
    Guid ResourceId,
    string ResourceName,
    int Capacity,
    DateTimeOffset Start,
    DateTimeOffset End);

public sealed record ConversationDto(
    Guid Id,
    Guid CustomerId,
    string? CustomerName,
    ConversationStatus Status,
    bool AiEnabled,
    DateTimeOffset UpdatedAt);

public sealed record MessageDto(
    Guid Id,
    Guid ConversationId,
    MessageDirection Direction,
    string Body,
    DateTimeOffset CreatedAt);

public sealed record ResourceDto(
    Guid Id,
    string Name,
    ResourceType ResourceType,
    int Capacity,
    bool IsActive,
    int SortOrder);

public sealed record OfferingDto(
    Guid Id,
    string Name,
    string? NameVi,
    int DurationMinutes,
    int PriceCents,
    bool IsResourceOnly,
    bool IsActive);
