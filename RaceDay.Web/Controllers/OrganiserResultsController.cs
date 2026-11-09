using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [RequireRole("Organiser")]
    [Route("organiser/events/{eventId:int}/results")]
    public class OrganiserResultsController : RaceDayController
    {
        public OrganiserResultsController(IRaceDayApi api) : base(api)
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

            var enrolments = await Api.GetEventEnrolmentsAsync(eventId, null);
            if (!enrolments.Success)
            {
                NoteError(enrolments.Error);
            }

            ViewBag.EventName = ev.Data.Name;
            ViewBag.EventDate = ev.Data.EventDate;
            ViewBag.EventId = eventId;
            ViewBag.CanCapture = ev.Data.EventDate <= DateTime.Now;
            return View(enrolments.Data ?? new List<EventEnrolmentRow>());
        }

        [HttpPost("")]
        public async Task<IActionResult> Save(int eventId, ResultForm form)
        {
            if (!ModelState.IsValid)
            {
                NoteError("Finish time must look like 03:41:18 and position must be 1 or more.");
                return Redirect($"/organiser/events/{eventId}/results");
            }

            var result = form.ResultId == null
                ? await Api.CaptureResultAsync(form.EnrolmentId, form.FinishTime, form.Position)
                : await Api.UpdateResultAsync(form.ResultId.Value, form.FinishTime, form.Position);

            if (Succeeded(result))
            {
                NoteSuccess(form.ResultId == null ? "Result captured." : "Result updated.");
            }

            return Redirect($"/organiser/events/{eventId}/results");
        }
    }
}
