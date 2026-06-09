using GhedDay.Domain.Entities;
using GhedDay.Domain.Enums;
using GhedDay.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace GhedDay.Infrastructure.Data;

/// <summary>
/// Local dev seed: two businesses — one nail salon and one restaurant — proving vertical
/// isolation from day one. Uses fixed GUIDs so the seed is idempotent.
/// </summary>
public static class DbSeeder
{
    private static readonly Guid NailSalonId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RestaurantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task SeedAsync(GhedDayDbContext db, CancellationToken ct = default)
    {
        // Seeding touches multiple tenants; bypass the global filter for the existence check.
        db.IgnoreTenantFilter = true;
        try
        {
            if (await db.Businesses.AnyAsync(ct))
                return;

            SeedNailSalon(db);
            SeedRestaurant(db);
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            db.IgnoreTenantFilter = false;
        }
    }

    private static void SeedNailSalon(GhedDayDbContext db)
    {
        var business = new Business
        {
            Id = NailSalonId,
            Name = "Lotus Nails",
            Slug = "lotus-nails",
            Timezone = "America/Los_Angeles",
            BusinessType = BusinessType.NailSalon,
            TwilioNumber = "+15550000001"
        };
        business.SetVerticalConfig(new VerticalConfig
        {
            ResourceLabel = "Chair",
            ResourceLabelPlural = "Chairs",
            DepositRequired = true,
            HoldMinutes = 15
        });
        db.Businesses.Add(business);

        for (var i = 1; i <= 4; i++)
        {
            db.Resources.Add(new Resource
            {
                BusinessId = NailSalonId,
                Name = $"Chair {i}",
                ResourceType = ResourceType.Chair,
                Capacity = 1,
                SortOrder = i
            });
        }

        db.Offerings.Add(new Offering
        {
            BusinessId = NailSalonId,
            Name = "Gel Manicure",
            NameVi = "Sơn gel tay",
            DurationMinutes = 45,
            PriceCents = 4000
        });
        db.Offerings.Add(new Offering
        {
            BusinessId = NailSalonId,
            Name = "Pedicure",
            NameVi = "Làm móng chân",
            DurationMinutes = 60,
            PriceCents = 5500
        });

        AddWeekdayHours(db, NailSalonId, new TimeOnly(9, 0), new TimeOnly(19, 0));
    }

    private static void SeedRestaurant(GhedDayDbContext db)
    {
        var business = new Business
        {
            Id = RestaurantId,
            Name = "Phở Sài Gòn",
            Slug = "pho-sai-gon",
            Timezone = "America/Los_Angeles",
            BusinessType = BusinessType.Restaurant,
            TwilioNumber = "+15550000002"
        };
        business.SetVerticalConfig(new VerticalConfig
        {
            ResourceLabel = "Table",
            ResourceLabelPlural = "Tables",
            DepositRequired = false,
            DepositThresholdPartySize = 6,
            DepositPerHeadCents = 1000,
            DefaultDurationMinutes = 90,
            PartySizeMin = 1,
            PartySizeMax = 20
        });
        db.Businesses.Add(business);

        var tableSizes = new[] { 2, 2, 4, 4, 6, 8 };
        for (var i = 0; i < tableSizes.Length; i++)
        {
            db.Resources.Add(new Resource
            {
                BusinessId = RestaurantId,
                Name = $"Table {i + 1}",
                ResourceType = ResourceType.Table,
                Capacity = tableSizes[i],
                SortOrder = i + 1
            });
        }

        db.Offerings.Add(new Offering
        {
            BusinessId = RestaurantId,
            Name = "Table Reservation",
            NameVi = "Đặt bàn",
            DurationMinutes = 90,
            PriceCents = 0,
            IsResourceOnly = true
        });

        AddWeekdayHours(db, RestaurantId, new TimeOnly(11, 0), new TimeOnly(22, 0));
    }

    private static void AddWeekdayHours(GhedDayDbContext db, Guid businessId, TimeOnly open, TimeOnly close)
    {
        for (var day = 0; day <= 6; day++)
        {
            db.BusinessHours.Add(new BusinessHours
            {
                BusinessId = businessId,
                DayOfWeek = day,
                OpenTime = open,
                CloseTime = close
            });
        }
    }
}
