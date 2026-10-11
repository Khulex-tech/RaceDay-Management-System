using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RaceDay.API.Controllers;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;
using RaceDay.Tests.TestHelpers;
using Xunit;

namespace RaceDay.Tests.Controllers
{
    /*
     * Unit tests for EventsController covering event creation, modification, filtering, deletion, and role authorization rules.
     */
    public class EventControllerTests
    {
        private readonly Mock<IWebHostEnvironment> _mockEnv = new();

        [Fact]
        public async Task GetEvents_ReturnsFilteredEvents()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(GetEvents_ReturnsFilteredEvents));
            db.Events.AddRange(
                new Event
                {
                    OrganiserId = 1,
                    EventTypeId = 1,
                    Name = "Soweto Marathon",
                    Description = "21km run in Soweto",
                    EventDate = DateTime.Now.AddDays(30),
                    Location = "Soweto",
                    DistanceKm = 21.1m
                },
                new Event
                {
                    OrganiserId = 1,
                    EventTypeId = 3,
                    Name = "Cape Town Cycle Tour",
                    Description = "109km cycle race",
                    EventDate = DateTime.Now.AddDays(60),
                    Location = "Cape Town",
                    DistanceKm = 109.0m
                }
            );
            await db.SaveChangesAsync();

            var controller = new EventsController(db, _mockEnv.Object).WithSession();

            // Act
            var result = await controller.GetEvents(eventType: "Run", fromDate: null, toDate: null, search: "Soweto", page: 1, pageSize: 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetEvent_ExistingId_Returns200OK()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(GetEvent_ExistingId_Returns200OK));
            var organiser = new User { FirstName = "Thabo", LastName = "Mokoena", Email = "thabo@raceday.co.za", PasswordHash = "hash", Role = Roles.Organiser };
            db.Users.Add(organiser);
            await db.SaveChangesAsync();

            var ev = new Event
            {
                OrganiserId = organiser.UserId,
                EventTypeId = 1,
                Name = "Joburg 10k City Run",
                Description = "10km street run",
                EventDate = DateTime.Now.AddDays(15),
                Location = "Johannesburg",
                DistanceKm = 10.0m
            };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            var controller = new EventsController(db, _mockEnv.Object).WithSession();

            // Act
            var result = await controller.GetEvent(ev.EventId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetEvent_NonExistingId_Returns404NotFound()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(GetEvent_NonExistingId_Returns404NotFound));
            var controller = new EventsController(db, _mockEnv.Object).WithSession();

            // Act
            var result = await controller.GetEvent(999);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_OrganiserRole_Returns201Created()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(CreateEvent_OrganiserRole_Returns201Created));
            var controller = new EventsController(db, _mockEnv.Object).WithSession(userId: 1, role: Roles.Organiser);

            var dto = new EventRequestDto
            {
                Name = "Durban City 10km",
                Description = "Coastal road race",
                EventDate = DateTime.Now.AddDays(45),
                Location = "Durban",
                DistanceKm = 10.0m,
                EventType = "Run"
            };

            // Act
            var result = await controller.CreateEvent(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, createdResult.StatusCode);
            Assert.NotNull(db.Events.FirstOrDefault(e => e.Name == "Durban City 10km"));
        }

        [Fact]
        public async Task CreateEvent_ParticipantRole_Returns403Forbidden()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(CreateEvent_ParticipantRole_Returns403Forbidden));
            var controller = new EventsController(db, _mockEnv.Object).WithSession(userId: 2, role: Roles.Participant);

            var dto = new EventRequestDto
            {
                Name = "Unauthorized Event",
                Description = "Should be rejected",
                EventDate = DateTime.Now.AddDays(10),
                Location = "Pretoria",
                DistanceKm = 5.0m,
                EventType = "Walk"
            };

            // Act
            var result = await controller.CreateEvent(dto);

            // Assert
            var forbidden = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, forbidden.StatusCode);
        }

        [Fact]
        public async Task CreateEvent_PastDate_Returns400BadRequest()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(CreateEvent_PastDate_Returns400BadRequest));
            var controller = new EventsController(db, _mockEnv.Object).WithSession(userId: 1, role: Roles.Organiser);

            var dto = new EventRequestDto
            {
                Name = "Past Event",
                Description = "Event in the past",
                EventDate = DateTime.Now.AddDays(-5),
                Location = "Polokwane",
                DistanceKm = 10.0m,
                EventType = "Run"
            };

            // Act
            var result = await controller.CreateEvent(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task UpdateEvent_OwnerOrganiser_Returns200OK()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(UpdateEvent_OwnerOrganiser_Returns200OK));
            var ev = new Event
            {
                OrganiserId = 1,
                EventTypeId = 1,
                Name = "Original Name",
                Description = "Original Description",
                EventDate = DateTime.Now.AddDays(20),
                Location = "Bloemfontein",
                DistanceKm = 15.0m
            };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            var controller = new EventsController(db, _mockEnv.Object).WithSession(userId: 1, role: Roles.Organiser);
            var updateDto = new EventRequestDto
            {
                Name = "Updated Name",
                Description = "Updated Description",
                EventDate = DateTime.Now.AddDays(25),
                Location = "Bloemfontein",
                DistanceKm = 15.0m,
                EventType = "Run"
            };

            // Act
            var result = await controller.UpdateEvent(ev.EventId, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var updatedInDb = await db.Events.FindAsync(ev.EventId);
            Assert.Equal("Updated Name", updatedInDb!.Name);
        }

        [Fact]
        public async Task UpdateEvent_NonOwnerOrganiser_Returns403Forbidden()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(UpdateEvent_NonOwnerOrganiser_Returns403Forbidden));
            var ev = new Event
            {
                OrganiserId = 1,
                EventTypeId = 1,
                Name = "Thabo's Event",
                Description = "Owned by Thabo (UserId 1)",
                EventDate = DateTime.Now.AddDays(20),
                Location = "East London",
                DistanceKm = 10.0m
            };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            // Logged in as Organiser UserId 2 (Ayesha)
            var controller = new EventsController(db, _mockEnv.Object).WithSession(userId: 2, role: Roles.Organiser);
            var updateDto = new EventRequestDto
            {
                Name = "Attempted Hijack",
                Description = "Should fail",
                EventDate = DateTime.Now.AddDays(25),
                Location = "East London",
                DistanceKm = 10.0m,
                EventType = "Run"
            };

            // Act
            var result = await controller.UpdateEvent(ev.EventId, updateDto);

            // Assert
            var forbidden = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, forbidden.StatusCode);
        }

        [Fact]
        public async Task DeleteEvent_WithConfirmedEnrolments_Returns409Conflict()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(DeleteEvent_WithConfirmedEnrolments_Returns409Conflict));
            var ev = new Event
            {
                OrganiserId = 1,
                EventTypeId = 1,
                Name = "Popular Event",
                Description = "Has confirmed participants",
                EventDate = DateTime.Now.AddDays(30),
                Location = "Nelspruit",
                DistanceKm = 21.0m
            };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            db.Enrolments.Add(new Enrolment
            {
                EventId = ev.EventId,
                ParticipantId = 5,
                CategoryId = 1,
                EnrolmentStatus = EnrolmentStatuses.Confirmed
            });
            await db.SaveChangesAsync();

            var controller = new EventsController(db, _mockEnv.Object).WithSession(userId: 1, role: Roles.Organiser);

            // Act
            var result = await controller.DeleteEvent(ev.EventId);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }
    }
}
