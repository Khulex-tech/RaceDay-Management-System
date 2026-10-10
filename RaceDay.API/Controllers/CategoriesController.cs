using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Handles creation, updates, deletion, and querying of event categories.
    /// </summary>
    [Route("api")]
    [Produces("application/json")]
    public class CategoriesController : BaseApiController
    {
        private readonly RaceDayDbContext _db;

        public CategoriesController(RaceDayDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Returns all categories defined for a specific event.
        /// </summary>
        /// <param name="eventId">Event ID.</param>
        /// <response code="200">List of categories returned.</response>
        /// <response code="404">Not Found, event does not exist.</response>
        [HttpGet("events/{eventId:int}/categories")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategoriesForEvent(int eventId)
        {
            bool eventExists = await _db.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists)
            {
                return NotFound(new { message = "Event not found." });
            }

            var categories = await _db.Categories
                .Where(c => c.EventId == eventId)
                .OrderBy(c => c.CategoryId)
                .Select(c => new
                {
                    categoryId = c.CategoryId,
                    categoryName = c.Name,
                    description = c.Description,
                    entryFee = c.EntryFee,
                    minAge = c.MinAge,
                    maxAge = c.MaxAge,
                    enrolledCount = c.Enrolments.Count(en => en.EnrolmentStatus != EnrolmentStatuses.Cancelled)
                })
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// Returns details for a single category.
        /// </summary>
        /// <param name="id">Category ID.</param>
        /// <response code="200">Category details returned.</response>
        /// <response code="404">Not Found, category does not exist.</response>
        [HttpGet("categories/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _db.Categories
                .Where(c => c.CategoryId == id)
                .Select(c => new
                {
                    categoryId = c.CategoryId,
                    categoryName = c.Name,
                    description = c.Description,
                    entryFee = c.EntryFee,
                    minAge = c.MinAge,
                    maxAge = c.MaxAge,
                    eventId = c.EventId,
                    eventName = c.Event!.Name
                })
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            return Ok(category);
        }

        /// <summary>
        /// Creates a new category under an event owned by the authenticated Organiser.
        /// </summary>
        /// <param name="eventId">Event ID.</param>
        /// <param name="dto">Category details.</param>
        /// <response code="201">Category created.</response>
        /// <response code="400">Bad Request, invalid age range.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is not the event Organiser.</response>
        /// <response code="404">Not Found, event does not exist.</response>
        /// <response code="409">Conflict, category name already exists on this event.</response>
        [HttpPost("events/{eventId:int}/categories")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateCategory(int eventId, CategoryRequestDto dto)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Organiser)
            {
                return NoAccess("Only Organisers can add categories.");
            }

            var ev = await _db.Events.FindAsync(eventId);
            if (ev == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (ev.OrganiserId != CurrentUserId)
            {
                return NoAccess("You can only add categories to your own events.");
            }

            if (dto.MaxAge < dto.MinAge)
            {
                return BadRequest(new { message = "Max age cannot be less than min age." });
            }

            string name = dto.CategoryName.Trim();
            bool nameTaken = await _db.Categories.AnyAsync(c => c.EventId == eventId && c.Name == name);
            if (nameTaken)
            {
                return Conflict(new { message = "This event already has a category with that name." });
            }

            var category = new Category
            {
                EventId = eventId,
                Name = name,
                Description = dto.Description,
                EntryFee = dto.EntryFee,
                MinAge = dto.MinAge,
                MaxAge = dto.MaxAge
            };

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, ToCategoryResponse(category));
        }

        /// <summary>
        /// Updates an existing category on an event owned by the authenticated Organiser.
        /// </summary>
        /// <param name="id">Category ID.</param>
        /// <param name="dto">Updated category details.</param>
        /// <response code="200">Category updated.</response>
        /// <response code="400">Bad Request, invalid age range.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is not the event Organiser.</response>
        /// <response code="404">Not Found, category does not exist.</response>
        /// <response code="409">Conflict, duplicate category name on event.</response>
        [HttpPut("categories/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateCategory(int id, CategoryRequestDto dto)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Organiser)
            {
                return NoAccess("Only Organisers can update categories.");
            }

            var category = await _db.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            if (category.Event!.OrganiserId != CurrentUserId)
            {
                return NoAccess("You can only update categories on your own events.");
            }

            if (dto.MaxAge < dto.MinAge)
            {
                return BadRequest(new { message = "Max age cannot be less than min age." });
            }

            // The new name must not clash with another category on the same event
            string name = dto.CategoryName.Trim();
            bool nameTaken = await _db.Categories.AnyAsync(c =>
                c.EventId == category.EventId && c.Name == name && c.CategoryId != id);
            if (nameTaken)
            {
                return Conflict(new { message = "This event already has a category with that name." });
            }

            category.Name = name;
            category.Description = dto.Description;
            category.EntryFee = dto.EntryFee;
            category.MinAge = dto.MinAge;
            category.MaxAge = dto.MaxAge;

            await _db.SaveChangesAsync();
            return Ok(ToCategoryResponse(category));
        }

        /// <summary>
        /// Deletes a category if no participants are enrolled in it.
        /// </summary>
        /// <param name="id">Category ID.</param>
        /// <response code="204">No Content, category deleted.</response>
        /// <response code="401">Unauthorized, no session.</response>
        /// <response code="403">Forbidden, caller is not the event Organiser.</response>
        /// <response code="404">Not Found, category does not exist.</response>
        /// <response code="409">Conflict, active enrolments exist in category.</response>
        [HttpDelete("categories/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Organiser)
            {
                return NoAccess("Only Organisers can delete categories.");
            }

            var category = await _db.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            if (category.Event!.OrganiserId != CurrentUserId)
            {
                return NoAccess("You can only delete categories on your own events.");
            }

            bool hasEnrolments = await _db.Enrolments.AnyAsync(en => en.CategoryId == id);
            if (hasEnrolments)
            {
                return Conflict(new { message = "Participants are enrolled in this category, so it cannot be deleted." });
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // Builds the response returned after creating or updating a category
        private static object ToCategoryResponse(Category category)
        {
            return new
            {
                categoryId = category.CategoryId,
                eventId = category.EventId,
                categoryName = category.Name,
                description = category.Description,
                entryFee = category.EntryFee,
                minAge = category.MinAge,
                maxAge = category.MaxAge
            };
        }
    }
}
