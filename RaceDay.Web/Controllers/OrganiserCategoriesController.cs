using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [RequireRole("Organiser")]
    [Route("organiser/events/{eventId:int}/categories")]
    public class OrganiserCategoriesController : RaceDayController
    {
        public OrganiserCategoriesController(IRaceDayApi api) : base(api)
        {
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int eventId)
        {
            if (!await OwnsEventAsync(eventId))
            {
                return Redirect("/organiser/events");
            }

            var ev = await Api.GetEventAsync(eventId);
            if (!ev.Success || ev.Data == null)
            {
                NoteError(ev.Error ?? "Event not found.");
                return Redirect("/organiser/events");
            }

            var categories = await Api.GetCategoriesAsync(eventId);
            ViewBag.EventName = ev.Data.Name;
            ViewBag.EventId = eventId;
            return View(categories.Data ?? new List<CategoryItem>());
        }

        [HttpPost("")]
        public async Task<IActionResult> Create(int eventId, CategoryForm form)
        {
            if (!ModelState.IsValid || form.MaxAge < form.MinAge)
            {
                NoteError(form.MaxAge < form.MinAge
                    ? "Max age cannot be less than min age."
                    : "Check the category details.");
                return Redirect($"/organiser/events/{eventId}/categories");
            }

            var created = await Api.CreateCategoryAsync(eventId, form);
            if (Succeeded(created))
            {
                NoteSuccess("Category added.");
            }

            return Redirect($"/organiser/events/{eventId}/categories");
        }

        [HttpPost("{id:int}")]
        public async Task<IActionResult> Update(int eventId, int id, CategoryForm form)
        {
            if (!ModelState.IsValid || form.MaxAge < form.MinAge)
            {
                NoteError("Check the category details.");
                return Redirect($"/organiser/events/{eventId}/categories");
            }

            var updated = await Api.UpdateCategoryAsync(id, form);
            if (Succeeded(updated))
            {
                NoteSuccess("Category updated.");
            }

            return Redirect($"/organiser/events/{eventId}/categories");
        }

        [HttpPost("{id:int}/delete")]
        public async Task<IActionResult> Delete(int eventId, int id)
        {
            var result = await Api.DeleteCategoryAsync(id);
            if (Succeeded(result))
            {
                NoteSuccess("Category deleted.");
            }

            return Redirect($"/organiser/events/{eventId}/categories");
        }
    }
}
