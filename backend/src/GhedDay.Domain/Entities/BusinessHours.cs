namespace GhedDay.Domain.Entities;

public class BusinessHours
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }

    /// <summary>0 = Sunday … 6 = Saturday.</summary>
    public int DayOfWeek { get; set; }

    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }

    public Business? Business { get; set; }
}
