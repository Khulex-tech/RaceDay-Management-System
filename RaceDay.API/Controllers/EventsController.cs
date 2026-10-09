using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /*
     * Event endpoints (section 4 of the endpoint plan).
     * Anyone can browse events. Only Organisers can create events, and only
     * the Organiser who created an event can change or delete it.
     */
    [Route("api")]
    public class EventsController : BaseApiController
    {
        private readonly RaceDayDbContext _db;
        private readonly IWebHostEnvironment _env;

        public EventsController(RaceDayDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // GET /api/events?eventType=Run&fromDate=&toDate=&search=&page=1&pageSize=10
        [HttpGet("events")]
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

        // GET /api/events/{id}
        [HttpGet("events/{id:int}")]
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

        // GET /api/events/upcoming?take=10
        [HttpGet("events/upcoming")]
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

        // GET /api/events/my-events (Organiser dashboard list)
        [HttpGet("events/my-events")]
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

        // POST /api/events
        [HttpPost("events")]
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

        // PUT /api/events/{id}
        [HttpPut("events/{id:int}")]
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

        // DELETE /api/events/{id}
        [HttpDelete("events/{id:int}")]
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

        // POST /api/events/{id}/banner
        [HttpPost("events/{id:int}/banner")]
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

        // GET /api/event-types
        [HttpGet("event-types")]
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
