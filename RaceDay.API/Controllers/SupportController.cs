using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Helpers;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Provides supporting endpoints for UI badge lookup, organiser dashboard totals, and system health checks.
    /// </summary>
    [Route("api")]
    [Produces("application/json")]
    public class SupportController : BaseApiController
    {
        private readonly RaceDayDbContext _db;

        public SupportController(RaceDayDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Returns enrolment status definitions with badge color codes for the user interface.
        /// </summary>
        /// <response code="200">List of enrolment statuses and hex color codes.</response>
        /// <response code="401">Unauthorized, no active session.</response>
        [HttpGet("enrolment-statuses")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

        /// <summary>
        /// Returns aggregated dashboard metrics for the authenticated Organiser.
        /// </summary>
        /// <response code="200">Organiser dashboard summary returned.</response>
        /// <response code="401">Unauthorized, no active session.</response>
        /// <response code="403">Forbidden, caller is not an Organiser.</response>
        [HttpGet("dashboard/organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        /// <summary>
        /// Verifies API system health and database connectivity.
        /// </summary>
        /// <response code="200">System healthy and database connected.</response>
        /// <response code="503">Service unavailable, database unreachable.</response>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
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
