using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.Tests.TestHelpers
{
    /*
     * Provides fresh in-memory RaceDayDbContext instances for unit testing.
     */
    public static class TestDbContextFactory
    {
        public static RaceDayDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var db = new RaceDayDbContext(options);
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // Seed required fixed data like EventTypes if empty
            if (!db.EventTypes.Any())
            {
                db.EventTypes.AddRange(
                    new EventType { EventTypeId = 1, Name = "Run" },
                    new EventType { EventTypeId = 2, Name = "Walk" },
                    new EventType { EventTypeId = 3, Name = "Cycle" }
                );
                db.SaveChanges();
            }

            return db;
        }
    }
}
