using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Handles capture, updates, deletion, and retrieval of participant race results.
    /// </summary>
    [Route("api")]
    [Produces("application/json")]
    public class ResultsController : BaseApiController
    {
        private readonly RaceDayDbContext _db;

        public ResultsController(RaceDayDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Captures a finish time and finishing position for an enrolment after race day.
        /// </summary>
        /// <param name="enrolmentId">Enrolment ID.</param>
        /// <param name="dto">Result details (finish time hh:mm:ss, position).</param>
        /// <response code="201">Result captured.</response>
        /// <response code="400">Bad Request, invalid finish time format.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is not the event Organiser.</response>
        /// <response code="404">Not Found, enrolment does not exist.</response>
        /// <response code="409">Conflict, event is in future or result already recorded.</response>
        [HttpPost("enrolments/{enrolmentId:int}/result")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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

        /// <summary>
        /// Updates a previously captured race result (Organiser only).
        /// </summary>
        /// <param name="id">Result ID.</param>
        /// <param name="dto">Updated finish time and position.</param>
        /// <response code="200">Result updated.</response>
        /// <response code="400">Bad Request, invalid time format.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is not the event Organiser.</response>
        /// <response code="404">Not Found, result does not exist.</response>
        [HttpPut("results/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Deletes a race result (Organiser only).
        /// </summary>
        /// <param name="id">Result ID.</param>
        /// <response code="204">No Content, result deleted.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is not the event Organiser.</response>
        /// <response code="404">Not Found, result does not exist.</response>
        [HttpDelete("results/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Returns public race results for an event ordered by position.
        /// </summary>
        /// <param name="eventId">Event ID.</param>
        /// <param name="categoryId">Optional category filter.</param>
        /// <response code="200">List of results returned.</response>
        /// <response code="404">Not Found, event does not exist.</response>
        [HttpGet("events/{eventId:int}/results")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Returns race result history for the authenticated Participant.
        /// </summary>
        /// <response code="200">List of personal race results returned.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is not a Participant.</response>
        [HttpGet("results/my-results")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        /// <summary>
        /// Returns a single result by ID (visible to Participant owner or event Organiser).
        /// </summary>
        /// <param name="id">Result ID.</param>
        /// <response code="200">Result details returned.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is neither owner nor event Organiser.</response>
        /// <response code="404">Not Found, result does not exist.</response>
        [HttpGet("results/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
