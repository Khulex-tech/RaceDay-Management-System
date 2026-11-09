using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class EventsController : RaceDayController
    {
        public EventsController(IRaceDayApi api) : base(api)
        {
        }

        public async Task<IActionResult> Details(int id)
        {
            var ev = await Api.GetEventAsync(id);
            if (!ev.Success || ev.Data == null)
            {
                NoteError(ev.Error ?? "Event not found.");
                return RedirectToAction("Index", "Home");
            }

            var prep = await Api.GetRacePrepAsync(id);
            ViewBag.Prep = prep.Success ? prep.Data : null;
            if (!prep.Success)
            {
                ViewBag.PrepError = prep.Error;
            }

            return View(ev.Data);
        }

        [HttpPost]
        [RequireRole("Participant")]
        public async Task<IActionResult> Enter(EnterEventForm form)
        {
            if (!ModelState.IsValid)
            {
                NoteError("Choose a category before entering.");
                return RedirectToAction(nameof(Details), new { id = form.EventId });
            }

            var result = await Api.EnterEventAsync(form.EventId, form.CategoryId);
            if (!Succeeded(result))
            {
                return RedirectToAction(nameof(Details), new { id = form.EventId });
            }

            NoteSuccess("You are entered. Your place stays pending until the organiser confirms it.");
            return RedirectToAction("Index", "MyEvents");
        }
    }
}
