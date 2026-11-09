using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [RequireRole("Organiser")]
    [Route("organiser/events/{eventId:int}/enrolments")]
    public class OrganiserEnrolmentsController : RaceDayController
    {
        public OrganiserEnrolmentsController(IRaceDayApi api) : base(api)
        {
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int eventId, string? status)
        {
            var ev = await Api.GetEventAsync(eventId);
            if (!ev.Success || ev.Data == null)
            {
                NoteError(ev.Error ?? "Event not found.");
                return Redirect("/organiser/events");
            }

            var rows = await Api.GetEventEnrolmentsAsync(eventId, status);
            if (!rows.Success)
            {
                NoteError(rows.Error);
            }

            ViewBag.EventName = ev.Data.Name;
            ViewBag.EventId = eventId;
            ViewBag.Status = status;
            return View(rows.Data ?? new List<EventEnrolmentRow>());
        }

        [HttpPost("{enrolmentId:int}/status")]
        public async Task<IActionResult> Status(int eventId, int enrolmentId, string status)
        {
            var result = await Api.UpdateEnrolmentStatusAsync(enrolmentId, status);
            if (Succeeded(result))
            {
                NoteSuccess("Enrolment status updated.");
            }

            return Redirect($"/organiser/events/{eventId}/enrolments");
        }
    }
}
