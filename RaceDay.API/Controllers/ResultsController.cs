using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /*
     * Result endpoints (section 7 of the endpoint plan).
     * Organisers capture results for their own events after race day.
     * Participants view their own race history, and event results are public.
     */
    [Route("api")]
    public class ResultsController : BaseApiController
    {
        private readonly RaceDayDbContext _db;

        public ResultsController(RaceDayDbContext db)
        {
            _db = db;
        }

        // POST /api/enrolments/{enrolmentId}/result
        [HttpPost("enrolments/{enrolmentId:int}/result")]
        public async Task<IActionResult> CaptureResult(int enrolmentId, ResultRequestDto dto)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Organiser)
            {
                return NoAccess("Only Organisers can capture results.");
            }

            if (!TryParseFinishTime(dto.FinishTime, out TimeSpan finishTime))
            {
                return BadRequest(new { message = "Finish time must be in hh:mm:ss format, e.g. 03:41:18." });
            }

            var enrolment = await _db.Enrolments
                .Include(en => en.Event)
                .Include(en => en.Result)
                .FirstOrDefaultAsync(en => en.EnrolmentId == enrolmentId);

            if (enrolment == null)
            {
                return NotFound(new { message = "Enrolment not found." });
            }

            if (enrolment.Event!.OrganiserId != CurrentUserId)
            {
                return NoAccess("You can only capture results for your own events.");
            }

            if (enrolment.Event.EventDate > DateTime.Now)
            {
                return Conflict(new { message = "Results can only be captured after the event has taken place." });
            }

            if (enrolment.EnrolmentStatus == EnrolmentStatuses.Cancelled)
            {
                return Conflict(new { message = "This enrolment was cancelled, so it cannot have a result." });
            }

            if (enrolment.Result != null)
            {
                return Conflict(new { message = "A result has already been captured for this enrolment." });
            }

            var result = new Result
            {
                EnrolmentId = enrolmentId,
                FinishTime = finishTime,
                FinishingPosition = dto.Position,
                PublishedAt = DateTime.Now
            };

            _db.Results.Add(result);
            await _db.SaveChangesAsync();

            int totalFinishers = await CountFinishers(enrolment.EventId);

            return CreatedAtAction(nameof(GetResult), new { id = result.ResultId }, new
            {
                resultId = result.ResultId,
                enrolmentId = result.EnrolmentId,
                finishTime = result.FinishTime,
                position = result.FinishingPosition,
                totalFinishers
            });
        }

        // PUT /api/results/{id}
        [HttpPut("results/{id:int}")]
        public async Task<IActionResult> UpdateResult(int id, ResultRequestDto dto)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Organiser)
            {
                return NoAccess("Only Organisers can update results.");
            }

            if (!TryParseFinishTime(dto.FinishTime, out TimeSpan finishTime))
            {
                return BadRequest(new { message = "Finish time must be in hh:mm:ss format, e.g. 03:41:18." });
            }

            var result = await _db.Results
                .Include(r => r.Enrolment).ThenInclude(en => en!.Event)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
            {
                return NotFound(new { message = "Result not found." });
            }

            if (result.Enrolment!.Event!.OrganiserId != CurrentUserId)
            {
                return NoAccess("You can only update results for your own events.");
            }

            result.FinishTime = finishTime;
            result.FinishingPosition = dto.Position;
            await _db.SaveChangesAsync();

            int totalFinishers = await CountFinishers(result.Enrolment.EventId);

            return Ok(new
            {
                resultId = result.ResultId,
                enrolmentId = result.EnrolmentId,
                finishTime = result.FinishTime,
                position = result.FinishingPosition,
                totalFinishers
            });
        }

        // DELETE /api/results/{id}
        [HttpDelete("results/{id:int}")]
        public async Task<IActionResult> DeleteResult(int id)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Organiser)
            {
                return NoAccess("Only Organisers can delete results.");
            }

            var result = await _db.Results
                .Include(r => r.Enrolment).ThenInclude(en => en!.Event)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
            {
                return NotFound(new { message = "Result not found." });
            }

            if (result.Enrolment!.Event!.OrganiserId != CurrentUserId)
            {
                return NoAccess("You can only delete results for your own events.");
            }

            _db.Results.Remove(result);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // GET /api/events/{eventId}/results?categoryId=
        [HttpGet("events/{eventId:int}/results")]
        public async Task<IActionResult> GetEventResults(int eventId, int? categoryId)
        {
            bool eventExists = await _db.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists)
            {
                return NotFound(new { message = "Event not found." });
            }

            int totalFinishers = await CountFinishers(eventId);

            IQueryable<Result> query = _db.Results.Where(r => r.Enrolment!.EventId == eventId);

            if (categoryId != null)
            {
                query = query.Where(r => r.Enrolment!.CategoryId == categoryId);
            }

            var results = await query
                .OrderBy(r => r.FinishingPosition)
                .Select(r => new
                {
                    resultId = r.ResultId,
                    position = r.FinishingPosition,
                    participantName = r.Enrolment!.Participant!.FirstName + " " + r.Enrolment.Participant.LastName,
                    categoryName = r.Enrolment.Category!.Name,
                    finishTime = r.FinishTime,
                    totalFinishers
                })
                .ToListAsync();

            return Ok(results);
        }

        // GET /api/results/my-results
        [HttpGet("results/my-results")]
        public async Task<IActionResult> GetMyResults()
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Participant)
            {
                return NoAccess("Only Participants have race results.");
            }

            var results = await _db.Results
                .Where(r => r.Enrolment!.ParticipantId == CurrentUserId)
                .OrderByDescending(r => r.Enrolment!.Event!.EventDate)
                .Select(r => new
                {
                    resultId = r.ResultId,
                    eventName = r.Enrolment!.Event!.Name,
                    eventDate = r.Enrolment.Event.EventDate,
                    categoryName = r.Enrolment.Category!.Name,
                    finishTime = r.FinishTime,
                    position = r.FinishingPosition,
                    // Number of captured results for the same event
                    totalFinishers = _db.Results.Count(other => other.Enrolment!.EventId == r.Enrolment.EventId)
                })
                .ToListAsync();

            return Ok(results);
        }

        // GET /api/results/{id} - visible to the Participant and the event's Organiser
        [HttpGet("results/{id:int}")]
        public async Task<IActionResult> GetResult(int id)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            var result = await _db.Results
                .Include(r => r.Enrolment).ThenInclude(en => en!.Event)
                .Include(r => r.Enrolment).ThenInclude(en => en!.Category)
                .Include(r => r.Enrolment).ThenInclude(en => en!.Participant)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
            {
                return NotFound(new { message = "Result not found." });
            }

            var enrolment = result.Enrolment!;
            bool isOwner = enrolment.ParticipantId == CurrentUserId;
            bool isEventOrganiser = enrolment.Event!.OrganiserId == CurrentUserId;
            if (!isOwner && !isEventOrganiser)
            {
                return NoAccess("You do not have access to this result.");
            }

            int totalFinishers = await CountFinishers(enrolment.EventId);

            return Ok(new
            {
                resultId = result.ResultId,
                enrolmentId = result.EnrolmentId,
                eventName = enrolment.Event.Name,
                eventDate = enrolment.Event.EventDate,
                categoryName = enrolment.Category!.Name,
                participantName = enrolment.Participant!.FirstName + " " + enrolment.Participant.LastName,
                finishTime = result.FinishTime,
                position = result.FinishingPosition,
                totalFinishers,
                publishedAt = result.PublishedAt
            });
        }

        // Checks that the finish time is in hh:mm:ss format and greater than zero
        private static bool TryParseFinishTime(string value, out TimeSpan finishTime)
        {
            bool valid = TimeSpan.TryParseExact(value, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out finishTime);
            return valid && finishTime > TimeSpan.Zero;
        }

        // Total finishers = number of results captured for the event
        private async Task<int> CountFinishers(int eventId)
        {
            return await _db.Results.CountAsync(r => r.Enrolment!.EventId == eventId);
        }
    }
}
