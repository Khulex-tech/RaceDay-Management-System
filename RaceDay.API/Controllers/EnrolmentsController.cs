using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Handles event enrolments, participant registration into categories, organiser status updates, and withdrawals.
    /// </summary>
    [Route("api")]
    [Produces("application/json")]
    public class EnrolmentsController : BaseApiController
    {
        private readonly RaceDayDbContext _db;

        public EnrolmentsController(RaceDayDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Enters the logged-in Participant into an event under a chosen category.
        /// </summary>
        /// <param name="eventId">Event ID.</param>
        /// <param name="dto">Chosen category ID.</param>
        /// <response code="201">Enrolment created.</response>
        /// <response code="400">Bad Request, category does not belong to event.</response>
        /// <response code="401">Unauthorized, no active session.</response>
        /// <response code="403">Forbidden, caller is not a Participant.</response>
        /// <response code="404">Not Found, event or category does not exist.</response>
        /// <response code="409">Conflict, already enrolled or event has passed.</response>
        [HttpPost("events/{eventId:int}/enter")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> EnterEvent(int eventId, CategoryChoiceDto dto)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Participant)
            {
                return NoAccess("Only Participants can enter events.");
            }

            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            var category = await _db.Categories.FindAsync(dto.CategoryId);
            if (category == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            if (category.EventId != eventId)
            {
                return BadRequest(new { message = "This category does not belong to this event." });
            }

            if (ev.EventDate <= DateTime.Now)
            {
                return Conflict(new { message = "This event has already taken place." });
            }

            var enrolment = await _db.Enrolments
                .FirstOrDefaultAsync(en => en.EventId == eventId && en.ParticipantId == CurrentUserId);

            if (enrolment != null && enrolment.EnrolmentStatus != EnrolmentStatuses.Cancelled)
            {
                return Conflict(new { message = "You are already enrolled in this event." });
            }

            if (enrolment == null)
            {
                // First time entering this event
                enrolment = new Enrolment
                {
                    ParticipantId = CurrentUserId.Value,
                    EventId = eventId,
                    CategoryId = category.CategoryId,
                    EnrolmentStatus = EnrolmentStatuses.Pending
                };
                _db.Enrolments.Add(enrolment);
            }
            else
            {
                /*
                 * The Participant cancelled before, and the database only allows one
                 * enrolment per event, so we re-open the old enrolment instead.
                 */
                enrolment.CategoryId = category.CategoryId;
                enrolment.EnrolmentStatus = EnrolmentStatuses.Pending;
                enrolment.EnrolledAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEnrolment), new { id = enrolment.EnrolmentId }, new
            {
                enrolmentId = enrolment.EnrolmentId,
                eventId = ev.EventId,
                eventName = ev.Name,
                categoryName = category.Name,
                status = enrolment.EnrolmentStatus,
                enrolledAt = enrolment.EnrolledAt
            });
        }

        /// <summary>
        /// Returns all enrolments for the currently authenticated Participant.
        /// </summary>
        /// <response code="200">List of enrolments returned.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is not a Participant.</response>
        [HttpGet("enrolments/my-enrolments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyEnrolments()
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Participant)
            {
                return NoAccess("Only Participants have enrolments.");
            }

            var enrolments = await _db.Enrolments
                .Where(en => en.ParticipantId == CurrentUserId)
                .OrderBy(en => en.Event!.EventDate)
                .Select(en => new
                {
                    enrolmentId = en.EnrolmentId,
                    eventId = en.EventId,
                    eventName = en.Event!.Name,
                    eventDate = en.Event.EventDate,
                    location = en.Event.Location,
                    categoryName = en.Category!.Name,
                    status = en.EnrolmentStatus,
                    enrolledAt = en.EnrolledAt
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        /// <summary>
        /// Returns a single enrolment by ID (visible to Participant owner or event Organiser).
        /// </summary>
        /// <param name="id">Enrolment ID.</param>
        /// <response code="200">Enrolment details returned.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is neither owner nor event Organiser.</response>
        /// <response code="404">Not Found, enrolment does not exist.</response>
        [HttpGet("enrolments/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEnrolment(int id)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            var enrolment = await _db.Enrolments
                .Include(en => en.Event)
                .Include(en => en.Category)
                .Include(en => en.Participant)
                .FirstOrDefaultAsync(en => en.EnrolmentId == id);

            if (enrolment == null)
            {
                return NotFound(new { message = "Enrolment not found." });
            }

            bool isOwner = enrolment.ParticipantId == CurrentUserId;
            bool isEventOrganiser = enrolment.Event!.OrganiserId == CurrentUserId;
            if (!isOwner && !isEventOrganiser)
            {
                return NoAccess("You do not have access to this enrolment.");
            }

            return Ok(new
            {
                enrolmentId = enrolment.EnrolmentId,
                eventId = enrolment.EventId,
                eventName = enrolment.Event.Name,
                eventDate = enrolment.Event.EventDate,
                categoryId = enrolment.CategoryId,
                categoryName = enrolment.Category!.Name,
                participant = new
                {
                    userId = enrolment.ParticipantId,
                    fullName = enrolment.Participant!.FirstName + " " + enrolment.Participant.LastName
                },
                status = enrolment.EnrolmentStatus,
                enrolledAt = enrolment.EnrolledAt
            });
        }

        /// <summary>
        /// Returns all participant enrolments for an event owned by the authenticated Organiser.
        /// </summary>
        /// <param name="eventId">Event ID.</param>
        /// <param name="status">Optional status filter (Pending, Confirmed, Cancelled).</param>
        /// <param name="categoryId">Optional category filter.</param>
        /// <response code="200">List of enrolments returned.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is not the event Organiser.</response>
        /// <response code="404">Not Found, event does not exist.</response>
        [HttpGet("events/{eventId:int}/enrolments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventEnrolments(int eventId, string? status, int? categoryId)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Organiser)
            {
                return NoAccess("Only Organisers can view event enrolments.");
            }

            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (ev.OrganiserId != CurrentUserId)
            {
                return NoAccess("You can only view enrolments for your own events.");
            }

            IQueryable<Enrolment> query = _db.Enrolments.Where(en => en.EventId == eventId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(en => en.EnrolmentStatus == status);
            }

            if (categoryId != null)
            {
                query = query.Where(en => en.CategoryId == categoryId);
            }

            var enrolments = await query
                .OrderBy(en => en.EnrolledAt)
                .Select(en => new
                {
                    enrolmentId = en.EnrolmentId,
                    participant = new
                    {
                        userId = en.ParticipantId,
                        fullName = en.Participant!.FirstName + " " + en.Participant.LastName
                    },
                    categoryName = en.Category!.Name,
                    status = en.EnrolmentStatus,
                    enrolledAt = en.EnrolledAt
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        /// <summary>
        /// Updates the status of an enrolment (Organiser confirms or cancels).
        /// </summary>
        /// <param name="id">Enrolment ID.</param>
        /// <param name="dto">Updated status (Pending, Confirmed, Cancelled).</param>
        /// <response code="200">Enrolment status updated.</response>
        /// <response code="400">Bad Request, invalid status value.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is not the event Organiser.</response>
        /// <response code="404">Not Found, enrolment does not exist.</response>
        [HttpPut("enrolments/{id:int}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(int id, EnrolmentStatusDto dto)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Organiser)
            {
                return NoAccess("Only Organisers can change enrolment status.");
            }

            if (!EnrolmentStatuses.All.Contains(dto.Status))
            {
                return BadRequest(new { message = "Status must be Pending, Confirmed or Cancelled." });
            }

            var enrolment = await _db.Enrolments
                .Include(en => en.Event)
                .FirstOrDefaultAsync(en => en.EnrolmentId == id);

            if (enrolment == null)
            {
                return NotFound(new { message = "Enrolment not found." });
            }

            if (enrolment.Event!.OrganiserId != CurrentUserId)
            {
                return NoAccess("You can only manage enrolments for your own events.");
            }

            enrolment.EnrolmentStatus = dto.Status;
            await _db.SaveChangesAsync();

            return Ok(new { enrolmentId = enrolment.EnrolmentId, status = enrolment.EnrolmentStatus });
        }

        /// <summary>
        /// Switches the category of an existing enrolment before race day.
        /// </summary>
        /// <param name="id">Enrolment ID.</param>
        /// <param name="dto">Target category ID.</param>
        /// <response code="200">Enrolment category updated.</response>
        /// <response code="400">Bad Request, category does not belong to event.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is not the Participant owner.</response>
        /// <response code="404">Not Found, enrolment does not exist.</response>
        /// <response code="409">Conflict, event has already taken place.</response>
        [HttpPut("enrolments/{id:int}/category")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ChangeCategory(int id, CategoryChoiceDto dto)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Participant)
            {
                return NoAccess("Only Participants can change their category.");
            }

            var enrolment = await _db.Enrolments
                .Include(en => en.Event)
                .FirstOrDefaultAsync(en => en.EnrolmentId == id);

            if (enrolment == null)
            {
                return NotFound(new { message = "Enrolment not found." });
            }

            if (enrolment.ParticipantId != CurrentUserId)
            {
                return NoAccess("You can only change your own enrolments.");
            }

            var category = await _db.Categories.FindAsync(dto.CategoryId);
            if (category == null || category.EventId != enrolment.EventId)
            {
                return BadRequest(new { message = "This category does not belong to this event." });
            }

            if (enrolment.Event!.EventDate <= DateTime.Now)
            {
                return Conflict(new { message = "This event has already taken place." });
            }

            enrolment.CategoryId = category.CategoryId;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                enrolmentId = enrolment.EnrolmentId,
                eventId = enrolment.EventId,
                eventName = enrolment.Event.Name,
                categoryId = category.CategoryId,
                categoryName = category.Name,
                status = enrolment.EnrolmentStatus
            });
        }

        /// <summary>
        /// Withdraws the authenticated Participant from an event before race day.
        /// </summary>
        /// <param name="id">Enrolment ID.</param>
        /// <response code="204">No Content, enrolment withdrawn.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is not the Participant owner.</response>
        /// <response code="404">Not Found, enrolment does not exist.</response>
        /// <response code="409">Conflict, event has taken place or result captured.</response>
        [HttpDelete("enrolments/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Withdraw(int id)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Participant)
            {
                return NoAccess("Only Participants can withdraw from events.");
            }

            var enrolment = await _db.Enrolments
                .Include(en => en.Event)
                .Include(en => en.Result)
                .FirstOrDefaultAsync(en => en.EnrolmentId == id);

            if (enrolment == null)
            {
                return NotFound(new { message = "Enrolment not found." });
            }

            if (enrolment.ParticipantId != CurrentUserId)
            {
                return NoAccess("You can only withdraw your own enrolments.");
            }

            if (enrolment.Event!.EventDate <= DateTime.Now || enrolment.Result != null)
            {
                return Conflict(new { message = "You cannot withdraw after the event has taken place." });
            }

            _db.Enrolments.Remove(enrolment);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
