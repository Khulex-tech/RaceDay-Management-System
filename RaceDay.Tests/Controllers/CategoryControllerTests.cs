using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Controllers;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;
using RaceDay.Tests.TestHelpers;
using Xunit;

namespace RaceDay.Tests.Controllers
{
    /*
     * Unit tests for CategoriesController covering category creation, queries, validation rules, and enrolment constraints.
     */
    public class CategoryControllerTests
    {
        [Fact]
        public async Task GetCategoriesForEvent_ValidEventId_Returns200OK()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(GetCategoriesForEvent_ValidEventId_Returns200OK));
            var ev = new Event { OrganiserId = 1, EventTypeId = 1, Name = "Trail Run", Description = "Desc", EventDate = DateTime.Now.AddDays(10), Location = "Loc", DistanceKm = 10.0m };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            db.Categories.Add(new Category { EventId = ev.EventId, Name = "Open Male", Description = "Desc", EntryFee = 150m, MinAge = 18, MaxAge = 40 });
            await db.SaveChangesAsync();

            var controller = new CategoriesController(db).WithSession();

            // Act
            var result = await controller.GetCategoriesForEvent(ev.EventId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetCategoriesForEvent_NonExistingEventId_Returns404NotFound()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(GetCategoriesForEvent_NonExistingEventId_Returns404NotFound));
            var controller = new CategoriesController(db).WithSession();

            // Act
            var result = await controller.GetCategoriesForEvent(999);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }

        [Fact]
        public async Task CreateCategory_EventOwnerOrganiser_Returns201Created()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(CreateCategory_EventOwnerOrganiser_Returns201Created));
            var ev = new Event { OrganiserId = 1, EventTypeId = 1, Name = "Night Run", Description = "Desc", EventDate = DateTime.Now.AddDays(15), Location = "Loc", DistanceKm = 5.0m };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            var controller = new CategoriesController(db).WithSession(userId: 1, role: Roles.Organiser);
            var dto = new CategoryRequestDto
            {
                CategoryName = "Veterans",
                Description = "Age 40+",
                EntryFee = 120m,
                MinAge = 40,
                MaxAge = 60
            };

            // Act
            var result = await controller.CreateCategory(ev.EventId, dto);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, created.StatusCode);
            Assert.Equal(1, db.Categories.Count(c => c.EventId == ev.EventId));
        }

        [Fact]
        public async Task CreateCategory_InvalidAgeRange_Returns400BadRequest()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(CreateCategory_InvalidAgeRange_Returns400BadRequest));
            var ev = new Event { OrganiserId = 1, EventTypeId = 1, Name = "City Run", Description = "Desc", EventDate = DateTime.Now.AddDays(20), Location = "Loc", DistanceKm = 10.0m };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            var controller = new CategoriesController(db).WithSession(userId: 1, role: Roles.Organiser);
            var dto = new CategoryRequestDto
            {
                CategoryName = "Invalid Age Category",
                Description = "Max age less than min age",
                EntryFee = 100m,
                MinAge = 50,
                MaxAge = 20
            };

            // Act
            var result = await controller.CreateCategory(ev.EventId, dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task CreateCategory_DuplicateName_Returns409Conflict()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(CreateCategory_DuplicateName_Returns409Conflict));
            var ev = new Event { OrganiserId = 1, EventTypeId = 1, Name = "Marathon", Description = "Desc", EventDate = DateTime.Now.AddDays(25), Location = "Loc", DistanceKm = 42.2m };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            db.Categories.Add(new Category { EventId = ev.EventId, Name = "Senior", Description = "Desc", EntryFee = 200m, MinAge = 20, MaxAge = 39 });
            await db.SaveChangesAsync();

            var controller = new CategoriesController(db).WithSession(userId: 1, role: Roles.Organiser);
            var dto = new CategoryRequestDto
            {
                CategoryName = "Senior",
                Description = "Duplicate Senior Category",
                EntryFee = 200m,
                MinAge = 20,
                MaxAge = 39
            };

            // Act
            var result = await controller.CreateCategory(ev.EventId, dto);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        [Fact]
        public async Task DeleteCategory_WithActiveEnrolments_Returns409Conflict()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(DeleteCategory_WithActiveEnrolments_Returns409Conflict));
            var ev = new Event { OrganiserId = 1, EventTypeId = 1, Name = "Event with Enrolment", Description = "Desc", EventDate = DateTime.Now.AddDays(30), Location = "Loc", DistanceKm = 10.0m };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            var category = new Category { EventId = ev.EventId, Name = "Open", Description = "Desc", EntryFee = 100m, MinAge = 18, MaxAge = 50 };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            db.Enrolments.Add(new Enrolment { EventId = ev.EventId, CategoryId = category.CategoryId, ParticipantId = 10, EnrolmentStatus = EnrolmentStatuses.Pending });
            await db.SaveChangesAsync();

            var controller = new CategoriesController(db).WithSession(userId: 1, role: Roles.Organiser);

            // Act
            var result = await controller.DeleteCategory(category.CategoryId);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }
    }
}
