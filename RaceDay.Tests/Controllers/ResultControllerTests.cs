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
     * Unit tests for ResultsController covering finish time capture, position recording, time format validation, and personal result history.
     */
    public class ResultControllerTests
    {
        [Fact]
        public async Task CaptureResult_PastEvent_Returns201Created()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(CaptureResult_PastEvent_Returns201Created));
            var ev = new Event { OrganiserId = 1, EventTypeId = 1, Name = "Past Marathon", Description = "Desc", EventDate = DateTime.Now.AddDays(-2), Location = "Loc", DistanceKm = 42.2m };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            var category = new Category { EventId = ev.EventId, Name = "Open", Description = "Desc", EntryFee = 200m, MinAge = 18, MaxAge = 50 };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            var enrolment = new Enrolment { EventId = ev.EventId, CategoryId = category.CategoryId, ParticipantId = 10, EnrolmentStatus = EnrolmentStatuses.Confirmed };
            db.Enrolments.Add(enrolment);
            await db.SaveChangesAsync();

            var controller = new ResultsController(db).WithSession(userId: 1, role: Roles.Organiser);
            var resultDto = new ResultRequestDto
            {
                FinishTime = "03:45:30",
                Position = 1
            };

            // Act
            var result = await controller.CaptureResult(enrolment.EnrolmentId, resultDto);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, created.StatusCode);
            Assert.Equal(1, db.Results.Count(r => r.EnrolmentId == enrolment.EnrolmentId));
        }

        [Fact]
        public async Task CaptureResult_FutureEvent_Returns409Conflict()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(CaptureResult_FutureEvent_Returns409Conflict));
            var ev = new Event { OrganiserId = 1, EventTypeId = 1, Name = "Future Race", Description = "Desc", EventDate = DateTime.Now.AddDays(5), Location = "Loc", DistanceKm = 10.0m };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            var category = new Category { EventId = ev.EventId, Name = "Open", Description = "Desc", EntryFee = 100m, MinAge = 18, MaxAge = 50 };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            var enrolment = new Enrolment { EventId = ev.EventId, CategoryId = category.CategoryId, ParticipantId = 10, EnrolmentStatus = EnrolmentStatuses.Confirmed };
            db.Enrolments.Add(enrolment);
            await db.SaveChangesAsync();

            var controller = new ResultsController(db).WithSession(userId: 1, role: Roles.Organiser);
            var resultDto = new ResultRequestDto
            {
                FinishTime = "00:45:10",
                Position = 1
            };

            // Act
            var result = await controller.CaptureResult(enrolment.EnrolmentId, resultDto);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        [Fact]
        public async Task CaptureResult_InvalidTimeFormat_Returns400BadRequest()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(CaptureResult_InvalidTimeFormat_Returns400BadRequest));
            var ev = new Event { OrganiserId = 1, EventTypeId = 1, Name = "Past 10k", Description = "Desc", EventDate = DateTime.Now.AddDays(-1), Location = "Loc", DistanceKm = 10.0m };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            var category = new Category { EventId = ev.EventId, Name = "Open", Description = "Desc", EntryFee = 100m, MinAge = 18, MaxAge = 50 };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            var enrolment = new Enrolment { EventId = ev.EventId, CategoryId = category.CategoryId, ParticipantId = 10, EnrolmentStatus = EnrolmentStatuses.Confirmed };
            db.Enrolments.Add(enrolment);
            await db.SaveChangesAsync();

            var controller = new ResultsController(db).WithSession(userId: 1, role: Roles.Organiser);
            var resultDto = new ResultRequestDto
            {
                FinishTime = "45 mins",
                Position = 1
            };

            // Act
            var result = await controller.CaptureResult(enrolment.EnrolmentId, resultDto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task CaptureResult_DuplicateResult_Returns409Conflict()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(CaptureResult_DuplicateResult_Returns409Conflict));
            var ev = new Event { OrganiserId = 1, EventTypeId = 1, Name = "Completed Race", Description = "Desc", EventDate = DateTime.Now.AddDays(-3), Location = "Loc", DistanceKm = 10.0m };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            var category = new Category { EventId = ev.EventId, Name = "Open", Description = "Desc", EntryFee = 100m, MinAge = 18, MaxAge = 50 };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            var enrolment = new Enrolment { EventId = ev.EventId, CategoryId = category.CategoryId, ParticipantId = 10, EnrolmentStatus = EnrolmentStatuses.Confirmed };
            db.Enrolments.Add(enrolment);
            await db.SaveChangesAsync();

            db.Results.Add(new Result { EnrolmentId = enrolment.EnrolmentId, FinishTime = TimeSpan.FromMinutes(42), FinishingPosition = 1, PublishedAt = DateTime.Now });
            await db.SaveChangesAsync();

            var controller = new ResultsController(db).WithSession(userId: 1, role: Roles.Organiser);
            var resultDto = new ResultRequestDto { FinishTime = "00:41:30", Position = 1 };

            // Act
            var result = await controller.CaptureResult(enrolment.EnrolmentId, resultDto);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        [Fact]
        public async Task GetEventResults_PublicAccess_Returns200OK()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(GetEventResults_PublicAccess_Returns200OK));
            var ev = new Event { OrganiserId = 1, EventTypeId = 1, Name = "Public Race", Description = "Desc", EventDate = DateTime.Now.AddDays(-5), Location = "Loc", DistanceKm = 21.1m };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            var controller = new ResultsController(db).WithSession();

            // Act
            var result = await controller.GetEventResults(ev.EventId, categoryId: null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetMyResults_ParticipantRole_Returns200OK()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(GetMyResults_ParticipantRole_Returns200OK));
            var participant = new User { FirstName = "Lerato", LastName = "Smit", Email = "lerato@raceday.co.za", PasswordHash = "hash", Role = Roles.Participant };
            db.Users.Add(participant);
            await db.SaveChangesAsync();

            var controller = new ResultsController(db).WithSession(userId: participant.UserId, role: Roles.Participant);

            // Act
            var result = await controller.GetMyResults();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }
    }
}
