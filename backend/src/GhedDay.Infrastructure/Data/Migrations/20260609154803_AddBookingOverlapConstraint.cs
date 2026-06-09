using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GhedDay.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingOverlapConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Database-level backstop against double-booking (non-negotiable rule 2). Even if
            // application logic regresses, Postgres physically cannot store two overlapping
            // active bookings for the same resource. Requires btree_gist for the "=" operator
            // on ResourceId inside a GiST exclusion constraint.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.Sql(
                """
                ALTER TABLE bookings
                ADD CONSTRAINT "ck_bookings_no_overlap"
                EXCLUDE USING gist (
                    "ResourceId" WITH =,
                    tstzrange("StartTime", "EndTime") WITH &&
                )
                WHERE ("ResourceId" IS NOT NULL AND "Status" IN ('PendingDeposit', 'Confirmed'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE bookings DROP CONSTRAINT IF EXISTS \"ck_bookings_no_overlap\";");
        }
    }
}
