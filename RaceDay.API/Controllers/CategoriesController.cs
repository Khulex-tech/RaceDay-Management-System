using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /*
     * Category endpoints (section 5 of the endpoint plan).
     * Anyone can view categories. Only the Organiser who owns the event can add,
     * change or remove its categories.
     */
    [Route("api")]
    public class CategoriesController : BaseApiController
    {
        private readonly RaceDayDbContext _db;

        public CategoriesController(RaceDayDbContext db)
        {
            _db = db;
        }

        // GET /api/events/{eventId}/categories
        [HttpGet("events/{eventId:int}/categories")]
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

        // GET /api/categories/{id}
        [HttpGet("categories/{id:int}")]
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

        // POST /api/events/{eventId}/categories
        [HttpPost("events/{eventId:int}/categories")]
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

        // PUT /api/categories/{id}
        [HttpPut("categories/{id:int}")]
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

        // DELETE /api/categories/{id}
        [HttpDelete("categories/{id:int}")]
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
