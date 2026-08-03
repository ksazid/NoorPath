using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NoorPath.FamilyBooking.Infrastructure;

public sealed class FamilyBookingDbContextFactory : IDesignTimeDbContextFactory<FamilyBookingDbContext>
{
    public FamilyBookingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("NOORPATH_FAMILY_BOOKING_TEST_DB")
            ?? "Host=localhost;Port=5432;Database=noorpath_family_booking_test;Username=noorpath;Password=noorpath";

        var options = new DbContextOptionsBuilder<FamilyBookingDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new FamilyBookingDbContext(options);
    }
}
