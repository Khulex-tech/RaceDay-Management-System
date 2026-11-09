using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Controllers;
using RaceDay.API.Helpers;
using RaceDay.API.Models;
using RaceDay.Tests.TestHelpers;

namespace RaceDay.Tests.Controllers
{
    public class RacePrepTests
    {
        [Fact]
        public void KnownStart_FindsSoweto()
        {
            var place = RaceLocations.Find("Soweto, Johannesburg");
            Assert.NotNull(place);
            Assert.Contains("Soweto", place.Value.Label);
        }

        [Fact]
        public void WeatherCode_DescribesRain()
        {
            Assert.Equal("Rain", OpenMeteoWeatherClient.Describe(61));
            Assert.Equal("Clear", OpenMeteoWeatherClient.Describe(0));
        }

        [Fact]
        public async Task GetRacePrep_ReturnsRoute_WhenWeatherIsUnavailable()
        {
            var db = TestDbContextFactory.CreateDbContext(nameof(GetRacePrep_ReturnsRoute_WhenWeatherIsUnavailable));
            var ev = new Event
            {
                OrganiserId = 1,
                EventTypeId = 1,
                Name = "Soweto Marathon",
                Description = "Road marathon",
                EventDate = DateTime.Now.AddDays(30),
                Location = "Soweto, Johannesburg",
                DistanceKm = 42.2m
            };
            db.Events.Add(ev);
            await db.SaveChangesAsync();
            db.Categories.Add(new Category
            {
                EventId = ev.EventId,
                Name = "42.2km Marathon",
                Description = "Full marathon distance.",
                EntryFee = 450,
                MinAge = 18,
                MaxAge = 59
            });
            await db.SaveChangesAsync();

            var controller = new EventsController(db, new FakeImageStore(), new UnavailableWeather())
                .WithSession();

            var result = await controller.GetRacePrep(ev.EventId);
            var ok = Assert.IsType<OkObjectResult>(result);
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
            string? summary = document.RootElement.GetProperty("route").GetProperty("summary").GetString();
            Assert.Contains("42.2", summary);
            Assert.Contains("Soweto", summary);
            Assert.False(document.RootElement.GetProperty("weather").GetProperty("available").GetBoolean());
        }

        private sealed class UnavailableWeather : IWeatherClient
        {
            public Task<WeatherSnapshot?> GetCurrentAsync(string location, CancellationToken cancellationToken)
            {
                return Task.FromResult<WeatherSnapshot?>(null);
            }
        }
    }
}
