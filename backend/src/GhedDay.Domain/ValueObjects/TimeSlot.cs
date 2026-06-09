namespace GhedDay.Domain.ValueObjects;

/// <summary>
/// A half-open time interval [Start, End) in UTC.
/// </summary>
public readonly record struct TimeSlot
{
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    public TimeSlot(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
            throw new ArgumentException("Slot end must be after start.", nameof(end));
        Start = start;
        End = end;
    }

    public TimeSpan Duration => End - Start;

    /// <summary>True when this slot and <paramref name="other"/> share any instant.</summary>
    public bool Overlaps(TimeSlot other) => Start < other.End && other.Start < End;

    public static TimeSlot FromDuration(DateTimeOffset start, int durationMinutes) =>
        new(start, start.AddMinutes(durationMinutes));
}
