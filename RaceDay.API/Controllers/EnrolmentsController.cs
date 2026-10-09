using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /*
     * Enrolment endpoints (section 6 of the endpoint plan).
     * Participants enter events and manage their own enrolments.
     * Organisers view and confirm enrolments for the events they own.
     */
    [Route("api")]
    public class EnrolmentsController : BaseApiController
    {
        private readonly RaceDayDbContext _db;

        public EnrolmentsController(RaceDayDbContext db)
        {
            _db = db;
        }

        // POST /api/events/{eventId}/enter
        [HttpPost("events/{eventId:int}/enter")]
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

        // GET /api/enrolments/my-enrolments
        [HttpGet("enrolments/my-enrolments")]
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

        // GET /api/enrolments/{id} - visible to the Participant and the event's Organiser
        [HttpGet("enrolments/{id:int}")]
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

        // GET /api/events/{eventId}/enrolments?status=Confirmed&categoryId=
        [HttpGet("events/{eventId:int}/enrolments")]
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

        // PUT /api/enrolments/{id}/status - Organiser confirms or cancels an enrolment
        [HttpPut("enrolments/{id:int}/status")]
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

        // PUT /api/enrolments/{id}/category - Participant switches category before race day
        [HttpPut("enrolments/{id:int}/category")]
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

        // DELETE /api/enrolments/{id} - Participant withdraws before race day
        [HttpDelete("enrolments/{id:int}")]
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
