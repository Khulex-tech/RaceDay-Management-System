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
     * Unit tests for EnrolmentsController covering event entry, category selection, status updates, and withdrawals.
     */
    public class EnrolmentControllerTests
    {
        [Fact]
        public async Task EnterEvent_ParticipantRole_Returns201Created()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(EnterEvent_ParticipantRole_Returns201Created));
            var ev = new Event { OrganiserId = 1, EventTypeId = 1, Name = "Spring Fun Run", Description = "Desc", EventDate = DateTime.Now.AddDays(15), Location = "Pretoria", DistanceKm = 5.0m };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            var category = new Category { EventId = ev.EventId, Name = "Fun Run Category", Description = "Desc", EntryFee = 80m, MinAge = 10, MaxAge = 70 };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            var controller = new EnrolmentsController(db).WithSession(userId: 10, role: Roles.Participant);
            var dto = new CategoryChoiceDto { CategoryId = category.CategoryId };

            // Act
            var result = await controller.EnterEvent(ev.EventId, dto);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, created.StatusCode);
            Assert.Equal(1, db.Enrolments.Count(en => en.EventId == ev.EventId && en.ParticipantId == 10));
        }

        [Fact]
        public async Task EnterEvent_OrganiserRole_Returns403Forbidden()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(EnterEvent_OrganiserRole_Returns403Forbidden));
            var ev = new Event { OrganiserId = 1, EventTypeId = 1, Name = "Organiser Race", Description = "Desc", EventDate = DateTime.Now.AddDays(10), Location = "Loc", DistanceKm = 10.0m };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            var category = new Category { EventId = ev.EventId, Name = "Cat 1", Description = "Desc", EntryFee = 100m, MinAge = 18, MaxAge = 40 };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            var controller = new EnrolmentsController(db).WithSession(userId: 1, role: Roles.Organiser);
            var dto = new CategoryChoiceDto { CategoryId = category.CategoryId };

            // Act
            var result = await controller.EnterEvent(ev.EventId, dto);

            // Assert
            var forbidden = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, forbidden.StatusCode);
        }

        [Fact]
        public async Task EnterEvent_PastEvent_Returns409Conflict()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(EnterEvent_PastEvent_Returns409Conflict));
            var ev = new Event { OrganiserId = 1, EventTypeId = 1, Name = "Completed Event", Description = "Desc", EventDate = DateTime.Now.AddDays(-2), Location = "Loc", DistanceKm = 10.0m };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            var category = new Category { EventId = ev.EventId, Name = "Cat 1", Description = "Desc", EntryFee = 100m, MinAge = 18, MaxAge = 40 };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            var controller = new EnrolmentsController(db).WithSession(userId: 10, role: Roles.Participant);
            var dto = new CategoryChoiceDto { CategoryId = category.CategoryId };

            // Act
            var result = await controller.EnterEvent(ev.EventId, dto);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        [Fact]
        public async Task EnterEvent_AlreadyEnrolled_Returns409Conflict()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(EnterEvent_AlreadyEnrolled_Returns409Conflict));
            var ev = new Event { OrganiserId = 1, EventTypeId = 1, Name = "Marathon", Description = "Desc", EventDate = DateTime.Now.AddDays(20), Location = "Loc", DistanceKm = 42.2m };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            var category = new Category { EventId = ev.EventId, Name = "Open", Description = "Desc", EntryFee = 250m, MinAge = 20, MaxAge = 50 };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            db.Enrolments.Add(new Enrolment { EventId = ev.EventId, CategoryId = category.CategoryId, ParticipantId = 10, EnrolmentStatus = EnrolmentStatuses.Pending });
            await db.SaveChangesAsync();

            var controller = new EnrolmentsController(db).WithSession(userId: 10, role: Roles.Participant);
            var dto = new CategoryChoiceDto { CategoryId = category.CategoryId };

            // Act
            var result = await controller.EnterEvent(ev.EventId, dto);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        [Fact]
        public async Task GetMyEnrolments_Participant_Returns200OK()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(GetMyEnrolments_Participant_Returns200OK));
            var ev = new Event { OrganiserId = 1, EventTypeId = 1, Name = "Night Run", Description = "Desc", EventDate = DateTime.Now.AddDays(10), Location = "Loc", DistanceKm = 10.0m };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            var category = new Category { EventId = ev.EventId, Name = "Open", Description = "Desc", EntryFee = 100m, MinAge = 18, MaxAge = 40 };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            db.Enrolments.Add(new Enrolment { EventId = ev.EventId, CategoryId = category.CategoryId, ParticipantId = 10, EnrolmentStatus = EnrolmentStatuses.Confirmed });
            await db.SaveChangesAsync();

            var controller = new EnrolmentsController(db).WithSession(userId: 10, role: Roles.Participant);

            // Act
            var result = await controller.GetMyEnrolments();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_OrganiserOwner_Returns200OK()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(UpdateStatus_OrganiserOwner_Returns200OK));
            var ev = new Event { OrganiserId = 1, EventTypeId = 1, Name = "Cycle Challenge", Description = "Desc", EventDate = DateTime.Now.AddDays(30), Location = "Loc", DistanceKm = 95.0m };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            var category = new Category { EventId = ev.EventId, Name = "Open Cycle", Description = "Desc", EntryFee = 300m, MinAge = 18, MaxAge = 60 };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            var enrolment = new Enrolment { EventId = ev.EventId, CategoryId = category.CategoryId, ParticipantId = 10, EnrolmentStatus = EnrolmentStatuses.Pending };
            db.Enrolments.Add(enrolment);
            await db.SaveChangesAsync();

            var controller = new EnrolmentsController(db).WithSession(userId: 1, role: Roles.Organiser);
            var statusDto = new EnrolmentStatusDto { Status = EnrolmentStatuses.Confirmed };

            // Act
            var result = await controller.UpdateStatus(enrolment.EnrolmentId, statusDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var updatedInDb = await db.Enrolments.FindAsync(enrolment.EnrolmentId);
            Assert.Equal(EnrolmentStatuses.Confirmed, updatedInDb!.EnrolmentStatus);
        }

        [Fact]
        public async Task Withdraw_ParticipantBeforeEvent_Returns204NoContent()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(Withdraw_ParticipantBeforeEvent_Returns204NoContent));
            var ev = new Event { OrganiserId = 1, EventTypeId = 1, Name = "Future Walk", Description = "Desc", EventDate = DateTime.Now.AddDays(15), Location = "Loc", DistanceKm = 5.0m };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            var category = new Category { EventId = ev.EventId, Name = "Walk Category", Description = "Desc", EntryFee = 50m, MinAge = 5, MaxAge = 80 };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            var enrolment = new Enrolment { EventId = ev.EventId, CategoryId = category.CategoryId, ParticipantId = 10, EnrolmentStatus = EnrolmentStatuses.Pending };
            db.Enrolments.Add(enrolment);
            await db.SaveChangesAsync();

            var controller = new EnrolmentsController(db).WithSession(userId: 10, role: Roles.Participant);

            // Act
            var result = await controller.Withdraw(enrolment.EnrolmentId);

            // Assert
            var noContent = Assert.IsType<NoContentResult>(result);
            Assert.Equal(204, noContent.StatusCode);
            Assert.Equal(0, db.Enrolments.Count(en => en.EnrolmentId == enrolment.EnrolmentId));
        }
    }
}
