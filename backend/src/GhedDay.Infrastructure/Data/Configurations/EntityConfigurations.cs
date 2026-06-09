using GhedDay.Domain.Entities;
using GhedDay.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GhedDay.Infrastructure.Data.Configurations;

public sealed class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> b)
    {
        b.ToTable("businesses");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired();
        b.Property(x => x.Slug).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.TwilioNumber).IsUnique();
        b.Property(x => x.Timezone).IsRequired();
        b.Property(x => x.BusinessType).HasConversion<string>().IsRequired();
        b.Property(x => x.VerticalConfigJson).HasColumnName("vertical_config").HasColumnType("jsonb").IsRequired();
        b.Property(x => x.SettingsJson).HasColumnName("settings").HasColumnType("jsonb");

        b.HasMany(x => x.Resources).WithOne(r => r.Business!).HasForeignKey(r => r.BusinessId);
        b.HasMany(x => x.Offerings).WithOne(o => o.Business!).HasForeignKey(o => o.BusinessId);
        b.HasMany(x => x.Hours).WithOne(h => h.Business!).HasForeignKey(h => h.BusinessId);
    }
}

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.PasswordHash).IsRequired();
        b.Property(x => x.Role).HasConversion<string>().IsRequired();
    }
}

public sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> b)
    {
        b.ToTable("resources");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired();
        b.Property(x => x.ResourceType).HasConversion<string>().IsRequired();
        b.Property(x => x.Capacity).HasDefaultValue(1);
        b.HasIndex(x => x.BusinessId);
    }
}

public sealed class OfferingConfiguration : IEntityTypeConfiguration<Offering>
{
    public void Configure(EntityTypeBuilder<Offering> b)
    {
        b.ToTable("offerings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired();
        b.HasIndex(x => x.BusinessId);
    }
}

public sealed class BusinessHoursConfiguration : IEntityTypeConfiguration<BusinessHours>
{
    public void Configure(EntityTypeBuilder<BusinessHours> b)
    {
        b.ToTable("business_hours");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.BusinessId, x.DayOfWeek });
    }
}

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("customers");
        b.HasKey(x => x.Id);
        b.Property(x => x.PhoneE164).IsRequired();
        b.Property(x => x.LanguagePref).IsRequired().HasDefaultValue("en");
        b.HasIndex(x => new { x.BusinessId, x.PhoneE164 }).IsUnique();
    }
}

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> b)
    {
        b.ToTable("conversations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<string>().IsRequired();
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
        b.HasMany(x => x.Messages).WithOne(m => m.Conversation!).HasForeignKey(m => m.ConversationId);
        b.HasIndex(x => x.BusinessId);
    }
}

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.ToTable("messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Direction).HasConversion<string>().IsRequired();
        b.Property(x => x.Body).IsRequired();
        b.HasIndex(x => x.ConversationId);
    }
}

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> b)
    {
        b.ToTable("bookings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<string>().IsRequired();
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
        b.HasOne(x => x.Offering).WithMany().HasForeignKey(x => x.OfferingId);
        b.HasOne(x => x.Resource).WithMany().HasForeignKey(x => x.ResourceId);

        // Availability overlap query path.
        b.HasIndex(x => new { x.BusinessId, x.StartTime, x.Status });
        b.HasIndex(x => x.ResourceId);
    }
}

public sealed class WaitlistEntryConfiguration : IEntityTypeConfiguration<WaitlistEntry>
{
    public void Configure(EntityTypeBuilder<WaitlistEntry> b)
    {
        b.ToTable("waitlist_entries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<string>().IsRequired();
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
        b.HasIndex(x => new { x.BusinessId, x.PreferredDate, x.Status });
    }
}

public sealed class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> b)
    {
        b.ToTable("reminders");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<string>().IsRequired();
        b.HasOne(x => x.Booking).WithMany().HasForeignKey(x => x.BookingId);
        b.HasIndex(x => x.ScheduledFor);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).IsRequired();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.UserId);
    }
}

public sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> b)
    {
        b.ToTable("processed_events");
        b.HasKey(x => new { x.Id, x.Source });
    }
}
