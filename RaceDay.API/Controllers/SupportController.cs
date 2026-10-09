using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Helpers;

namespace RaceDay.API.Controllers
{
    // Supporting endpoints (section 8 of the endpoint plan)
    [Route("api")]
    public class SupportController : BaseApiController
    {
        private readonly RaceDayDbContext _db;

        public SupportController(RaceDayDbContext db)
        {
            _db = db;
        }

        // GET /api/enrolment-statuses - status list with badge colours for the front end
        [HttpGet("enrolment-statuses")]
        public IActionResult GetEnrolmentStatuses()
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            /*
             * The database stores the status as text with a CHECK constraint,
             * so the list and colours are kept here in one place.
             */
            var statuses = new[]
            {
                new { statusId = 1, statusName = EnrolmentStatuses.Pending, colourCode = "#F0AD4E" },
                new { statusId = 2, statusName = EnrolmentStatuses.Confirmed, colourCode = "#5CB85C" },
                new { statusId = 3, statusName = EnrolmentStatuses.Cancelled, colourCode = "#D9534F" }
            };

            return Ok(statuses);
        }

        // GET /api/dashboard/organiser
        [HttpGet("dashboard/organiser")]
        public async Task<IActionResult> GetOrganiserDashboard()
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Organiser)
            {
                return NoAccess("Only Organisers have a dashboard.");
            }

            var myEvents = _db.Events.Where(e => e.OrganiserId == CurrentUserId);

            int totalEvents = await myEvents.CountAsync();

            int totalEnrolments = await _db.Enrolments.CountAsync(en =>
                en.Event!.OrganiserId == CurrentUserId && en.EnrolmentStatus != EnrolmentStatuses.Cancelled);

            var upcomingEvents = await myEvents
                .Where(e => e.EventDate > DateTime.Now)
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    eventId = e.EventId,
                    name = e.Name,
                    eventDate = e.EventDate,
                    enrolmentCount = e.Enrolments.Count(en => en.EnrolmentStatus != EnrolmentStatuses.Cancelled)
                })
                .ToListAsync();

            return Ok(new { totalEvents, totalEnrolments, upcomingEvents });
        }

        // GET /api/health - checks that the API can reach the database
        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            bool connected;
            try
            {
                connected = await _db.Database.CanConnectAsync();
            }
            catch
            {
                connected = false;
            }

            if (!connected)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { status = "Unhealthy", database = "Disconnected" });
            }

            return Ok(new { status = "Healthy", database = "Connected" });
        }
    }
}
