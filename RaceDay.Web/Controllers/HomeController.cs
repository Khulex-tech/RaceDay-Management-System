using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class HomeController : RaceDayController
    {
        public HomeController(IRaceDayApi api) : base(api)
        {
        }

        public async Task<IActionResult> Index(HomeFilter filter)
        {
            if (filter.Page < 1)
            {
                filter.Page = 1;
            }

            DateTime? fromDate = filter.FromDate;
            if (!filter.IncludePast && fromDate == null)
            {
                fromDate = DateTime.Today;
            }

            var events = await Api.GetEventsAsync(filter.EventType, filter.Search, fromDate, filter.ToDate, filter.Page, 9);
            var types = await Api.GetEventTypesAsync();
            ViewBag.Types = types.Success && types.Data != null ? types.Data : new List<EventTypeItem>();
            ViewBag.Filter = filter;

            if (!events.Success || events.Data == null)
            {
                NoteError(events.Error);
                return View(new PagedEvents { Page = filter.Page, PageSize = 9 });
            }

            return View(events.Data);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
