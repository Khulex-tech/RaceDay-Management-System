using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [RequireRole("Organiser")]
    [Route("organiser")]
    public class OrganiserController : RaceDayController
    {
        public OrganiserController(IRaceDayApi api) : base(api)
        {
        }

        [HttpGet("")]
        public async Task<IActionResult> Dashboard()
        {
            var dashboard = await Api.GetDashboardAsync();
            if (!dashboard.Success || dashboard.Data == null)
            {
                NoteError(dashboard.Error);
                return View(new OrganiserDashboard());
            }

            return View(dashboard.Data);
        }

        [HttpGet("events")]
        public async Task<IActionResult> Events()
        {
            var events = await Api.GetOrganiserEventsAsync();
            if (!events.Success || events.Data == null)
            {
                NoteError(events.Error);
                return View(new List<OrganiserEventItem>());
            }

            return View(events.Data);
        }

        [HttpGet("events/create")]
        public async Task<IActionResult> Create()
        {
            await LoadTypesAsync();
            return View("EventForm", new EventForm());
        }

        [HttpPost("events/create")]
        public async Task<IActionResult> Create(EventForm form)
        {
            if (!ModelState.IsValid)
            {
                await LoadTypesAsync();
                return View("EventForm", form);
            }

            var created = await Api.CreateEventAsync(form);
            if (!created.Success || created.Data == null)
            {
                NoteError(created.Error);
                await LoadTypesAsync();
                return View("EventForm", form);
            }

            NoteSuccess("Event created. Add categories before participants can enter.");
            return Redirect($"/organiser/events/{created.Data.EventId}/categories");
        }

        [HttpGet("events/{id:int}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var ev = await Api.GetEventAsync(id);
            if (!ev.Success || ev.Data == null)
            {
                NoteError(ev.Error ?? "Event not found.");
                return RedirectToAction(nameof(Events));
            }

            await LoadTypesAsync();
            ViewBag.EventId = id;
            ViewBag.Banner = ev.Data.BannerImageUrl;
            return View("EventForm", new EventForm
            {
                Name = ev.Data.Name,
                Description = ev.Data.Description,
                EventDate = ev.Data.EventDate,
                Location = ev.Data.Location,
                DistanceKm = ev.Data.DistanceKm,
                EventType = ev.Data.EventType
            });
        }

        [HttpPost("events/{id:int}/edit")]
        public async Task<IActionResult> Edit(int id, EventForm form)
        {
            if (!ModelState.IsValid)
            {
                await LoadTypesAsync();
                ViewBag.EventId = id;
                return View("EventForm", form);
            }

            var updated = await Api.UpdateEventAsync(id, form);
            if (!Succeeded(updated))
            {
                await LoadTypesAsync();
                ViewBag.EventId = id;
                return View("EventForm", form);
            }

            NoteSuccess("Event updated.");
            return RedirectToAction(nameof(Events));
        }

        [HttpPost("events/{id:int}/delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await Api.DeleteEventAsync(id);
            if (Succeeded(result))
            {
                NoteSuccess("Event deleted.");
            }

            return RedirectToAction(nameof(Events));
        }

        [HttpPost("events/{id:int}/banner")]
        public async Task<IActionResult> Banner(int id, IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                NoteError("Choose a JPG or PNG banner.");
                return RedirectToAction(nameof(Edit), new { id });
            }

            var result = await Api.UploadBannerAsync(id, file);
            if (Succeeded(result))
            {
                NoteSuccess("Banner uploaded.");
            }

            return RedirectToAction(nameof(Edit), new { id });
        }

        private async Task LoadTypesAsync()
        {
            var types = await Api.GetEventTypesAsync();
            ViewBag.Types = types.Success && types.Data is { Count: > 0 }
                ? types.Data
                : new List<EventTypeItem>
                {
                    new() { TypeName = "Run" },
                    new() { TypeName = "Walk" },
                    new() { TypeName = "Cycle" }
                };
        }
    }
}
