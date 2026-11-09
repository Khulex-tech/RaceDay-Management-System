using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [RequireRole("Participant")]
    public class MyEventsController : RaceDayController
    {
        public MyEventsController(IRaceDayApi api) : base(api)
        {
        }

        public async Task<IActionResult> Index()
        {
            var mine = await Api.GetMyEnrolmentsAsync();
            var enrolments = mine.Success && mine.Data != null ? mine.Data : new List<MyEnrolment>();
            if (!mine.Success)
            {
                NoteError(mine.Error);
            }

            var categories = new Dictionary<int, List<CategoryItem>>();
            foreach (var enrolment in enrolments.Where(item => item.EventDate > DateTime.Now))
            {
                if (categories.ContainsKey(enrolment.EventId))
                {
                    continue;
                }

                var loaded = await Api.GetCategoriesAsync(enrolment.EventId);
                categories[enrolment.EventId] = loaded.Data ?? new List<CategoryItem>();
            }

            ViewBag.Categories = categories;
            return View(enrolments);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeCategory(int enrolmentId, int categoryId)
        {
            var result = await Api.ChangeCategoryAsync(enrolmentId, categoryId);
            if (Succeeded(result))
            {
                NoteSuccess("Category updated.");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Withdraw(int enrolmentId)
        {
            var result = await Api.WithdrawAsync(enrolmentId);
            if (Succeeded(result))
            {
                NoteSuccess("You have withdrawn from that event.");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
