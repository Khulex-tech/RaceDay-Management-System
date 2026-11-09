using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [RequireRole("Participant")]
    public class MyResultsController : RaceDayController
    {
        public MyResultsController(IRaceDayApi api) : base(api)
        {
        }

        public async Task<IActionResult> Index()
        {
            var results = await Api.GetMyResultsAsync();
            if (!results.Success || results.Data == null)
            {
                NoteError(results.Error);
                return View(new List<MyResultItem>());
            }

            return View(results.Data);
        }
    }
}
