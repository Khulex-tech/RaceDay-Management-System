using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Handles event creation, search filtering, details lookup, banner uploads, and event types listing.
    /// </summary>
    [Route("api")]
    [Produces("application/json")]
    public class EventsController : BaseApiController
    {
        private readonly RaceDayDbContext _db;
        private readonly IWebHostEnvironment _env;

        public EventsController(RaceDayDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        /// <summary>
        /// Returns all events with optional type, date, search term, and pagination parameters.
        /// </summary>
        /// <param name="eventType">Optional event type filter (Run, Walk, Cycle).</param>
        /// <param name="fromDate">Optional start date filter.</param>
        /// <param name="toDate">Optional end date filter.</param>
        /// <param name="search">Search text matching event name or location.</param>
        /// <param name="page">Page number (default 1).</param>
        /// <param name="pageSize">Page size (default 10, maximum 50).</param>
        /// <response code="200">Paginated list of events returned.</response>
        /// <response code="400">Bad Request, invalid pagination or date range.</response>
        [HttpGet("events")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetEvents(string? eventType, DateTime? fromDate, DateTime? toDate,
            string? search, int page = 1, int pageSize = 10)
        {
            if (page < 1 || pageSize < 1 || pageSize > 50)
            {
                return BadRequest(new { message = "Page must be 1 or more and pageSize must be between 1 and 50." });
            }

            if (fromDate != null && toDate != null && fromDate > toDate)
            {
                return BadRequest(new { message = "fromDate cannot be after toDate." });
            }

            // Start with all events, then add each filter that was supplied
            IQueryable<Event> query = _db.Events;

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                query = query.Where(e => e.EventType!.Name == eventType);
            }

            if (fromDate != null)
            {
                query = query.Where(e => e.EventDate >= fromDate);
            }

            if (toDate != null)
            {
                query = query.Where(e => e.EventDate <= toDate);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e => e.Name.Contains(search) || e.Location.Contains(search));
            }

            int totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(e => e.EventDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new
                {
                    eventId = e.EventId,
                    name = e.Name,
                    eventDate = e.EventDate,
                    location = e.Location,
                    distanceKm = e.DistanceKm,
                    eventType = e.EventType!.Name,
                    bannerImageUrl = e.BannerImageUrl,
                    enrolmentCount = e.Enrolments.Count(en => en.EnrolmentStatus != EnrolmentStatuses.Cancelled)
                })
                .ToListAsync();

            return Ok(new { page, pageSize, totalCount, items });
        }

        /// <summary>
        /// Returns full details for a single event including categories and organiser info.
        /// </summary>
        /// <param name="id">Event ID.</param>
        /// <response code="200">Event details returned.</response>
        /// <response code="404">Not Found, event does not exist.</response>
        [HttpGet("events/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEvent(int id)
        {
            var ev = await _db.Events
                .Where(e => e.EventId == id)
                .Select(e => new
                {
                    eventId = e.EventId,
                    name = e.Name,
                    description = e.Description,
                    eventDate = e.EventDate,
                    location = e.Location,
                    distanceKm = e.DistanceKm,
                    eventType = e.EventType!.Name,
                    bannerImageUrl = e.BannerImageUrl,
                    organiser = new
                    {
                        userId = e.OrganiserId,
                        fullName = e.Organiser!.FirstName + " " + e.Organiser.LastName
                    },
                    categories = e.Categories.Select(c => new
                    {
                        categoryId = c.CategoryId,
                        categoryName = c.Name,
                        description = c.Description,
                        entryFee = c.EntryFee,
                        minAge = c.MinAge,
                        maxAge = c.MaxAge
                    })
                })
                .FirstOrDefaultAsync();

            if (ev == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            return Ok(ev);
        }

        /// <summary>
        /// Returns a quick list of upcoming future events.
        /// </summary>
        /// <param name="take">Number of events to take (default 10).</param>
        /// <response code="200">List of upcoming event summaries returned.</response>
        [HttpGet("events/upcoming")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUpcomingEvents(int take = 10)
        {
            // Keep the number of results sensible
            if (take < 1 || take > 50)
            {
                take = 10;
            }

            var events = await _db.Events
                .Where(e => e.EventDate > DateTime.Now)
                .OrderBy(e => e.EventDate)
                .Take(take)
                .Select(e => new
                {
                    eventId = e.EventId,
                    name = e.Name,
                    eventDate = e.EventDate,
                    location = e.Location,
                    distanceKm = e.DistanceKm,
                    eventType = e.EventType!.Name,
                    bannerImageUrl = e.BannerImageUrl
                })
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>
        /// Returns events created by the logged-in Organiser.
        /// </summary>
        /// <response code="200">List of organiser events returned.</response>
        /// <response code="401">Unauthorized, no active session.</response>
        /// <response code="403">Forbidden, caller is not an Organiser.</response>
        [HttpGet("events/my-events")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyEvents()
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Organiser)
            {
                return NoAccess("Only Organisers have their own events.");
            }

            var events = await _db.Events
                .Where(e => e.OrganiserId == CurrentUserId)
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    eventId = e.EventId,
                    name = e.Name,
                    eventDate = e.EventDate,
                    distanceKm = e.DistanceKm,
                    enrolmentCount = e.Enrolments.Count(en => en.EnrolmentStatus != EnrolmentStatuses.Cancelled)
                })
                .ToListAsync();

            return Ok(events);
        }

        /// <summary>
        /// Creates a new event owned by the authenticated Organiser.
        /// </summary>
        /// <param name="dto">Event details.</param>
        /// <response code="201">Event created.</response>
        /// <response code="400">Bad Request, date in past or invalid event type.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is not an Organiser.</response>
        [HttpPost("events")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateEvent(EventRequestDto dto)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Organiser)
            {
                return NoAccess("Only Organisers can create events.");
            }

            if (dto.EventDate <= DateTime.Now)
            {
                return BadRequest(new { message = "Event date must be in the future." });
            }

            var eventType = await _db.EventTypes.FirstOrDefaultAsync(t => t.Name == dto.EventType);
            if (eventType == null)
            {
                return BadRequest(new { message = "Event type must be Run, Walk or Cycle." });
            }

            var ev = new Event
            {
                OrganiserId = CurrentUserId.Value,
                EventTypeId = eventType.EventTypeId,
                Name = dto.Name.Trim(),
                Description = dto.Description.Trim(),
                EventDate = dto.EventDate,
                Location = dto.Location.Trim(),
                DistanceKm = dto.DistanceKm
            };

            _db.Events.Add(ev);
            await _db.SaveChangesAsync();

            // 201 Created with a Location header pointing to GET /api/events/{id}
            return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, ToEventResponse(ev, eventType.Name));
        }

        /// <summary>
        /// Updates an existing event owned by the authenticated Organiser.
        /// </summary>
        /// <param name="id">Event ID.</param>
        /// <param name="dto">Updated event details.</param>
        /// <response code="200">Event updated.</response>
        /// <response code="400">Bad Request, date in past or invalid event type.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is not the event Organiser.</response>
        /// <response code="404">Not Found, event does not exist.</response>
        [HttpPut("events/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(int id, EventRequestDto dto)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Organiser)
            {
                return NoAccess("Only Organisers can update events.");
            }

            var ev = await _db.Events.FindAsync(id);
            if (ev == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (ev.OrganiserId != CurrentUserId)
            {
                return NoAccess("You can only update your own events.");
            }

            if (dto.EventDate <= DateTime.Now)
            {
                return BadRequest(new { message = "Event date must be in the future." });
            }

            var eventType = await _db.EventTypes.FirstOrDefaultAsync(t => t.Name == dto.EventType);
            if (eventType == null)
            {
                return BadRequest(new { message = "Event type must be Run, Walk or Cycle." });
            }

            ev.EventTypeId = eventType.EventTypeId;
            ev.Name = dto.Name.Trim();
            ev.Description = dto.Description.Trim();
            ev.EventDate = dto.EventDate;
            ev.Location = dto.Location.Trim();
            ev.DistanceKm = dto.DistanceKm;

            await _db.SaveChangesAsync();
            return Ok(ToEventResponse(ev, eventType.Name));
        }

        /// <summary>
        /// Deletes an event if no confirmed enrolments exist.
        /// </summary>
        /// <param name="id">Event ID.</param>
        /// <response code="204">No Content, event deleted.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is not the event Organiser.</response>
        /// <response code="404">Not Found, event does not exist.</response>
        /// <response code="409">Conflict, event has confirmed enrolments.</response>
        [HttpDelete("events/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Organiser)
            {
                return NoAccess("Only Organisers can delete events.");
            }

            var ev = await _db.Events
                .Include(e => e.Enrolments)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (ev.OrganiserId != CurrentUserId)
            {
                return NoAccess("You can only delete your own events.");
            }

            // Never silently remove confirmed Participants
            if (ev.Enrolments.Any(en => en.EnrolmentStatus == EnrolmentStatuses.Confirmed))
            {
                return Conflict(new { message = "This event has confirmed enrolments and cannot be deleted." });
            }

            /*
             * Pending and cancelled enrolments are removed with the event.
             * Categories and results are removed by the database cascade rules.
             */
            _db.Enrolments.RemoveRange(ev.Enrolments);
            _db.Events.Remove(ev);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Uploads an event banner image for an event owned by the authenticated Organiser.
        /// </summary>
        /// <param name="id">Event ID.</param>
        /// <param name="file">Image file (JPEG/PNG, max 5 MB).</param>
        /// <response code="200">Event banner uploaded and URL returned.</response>
        /// <response code="400">Bad Request, invalid image file or size.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is not the event Organiser.</response>
        /// <response code="404">Not Found, event does not exist.</response>
        [HttpPost("events/{id:int}/banner")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadBanner(int id, IFormFile? file)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Organiser)
            {
                return NoAccess("Only Organisers can upload event banners.");
            }

            var ev = await _db.Events.FindAsync(id);
            if (ev == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (ev.OrganiserId != CurrentUserId)
            {
                return NoAccess("You can only change your own events.");
            }

            string? error = ImageUploadHelper.Validate(file);
            if (error != null)
            {
                return BadRequest(new { message = error });
            }

            ev.BannerImageUrl = await ImageUploadHelper.SaveAsync(file!, _env.WebRootPath);
            await _db.SaveChangesAsync();

            return Ok(new { eventId = ev.EventId, bannerImageUrl = ev.BannerImageUrl });
        }

        /// <summary>
        /// Returns available event types (Run, Walk, Cycle).
        /// </summary>
        /// <response code="200">List of event types returned.</response>
        [HttpGet("event-types")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEventTypes()
        {
            var types = await _db.EventTypes
                .OrderBy(t => t.EventTypeId)
                .Select(t => new { eventTypeId = t.EventTypeId, typeName = t.Name })
                .ToListAsync();

            return Ok(types);
        }

        // Builds the response returned after creating or updating an event
        private static object ToEventResponse(Event ev, string eventTypeName)
        {
            return new
            {
                eventId = ev.EventId,
                organiserId = ev.OrganiserId,
                name = ev.Name,
                description = ev.Description,
                eventDate = ev.EventDate,
                location = ev.Location,
                distanceKm = ev.DistanceKm,
                eventType = eventTypeName,
                bannerImageUrl = ev.BannerImageUrl
            };
        }
    }
}
