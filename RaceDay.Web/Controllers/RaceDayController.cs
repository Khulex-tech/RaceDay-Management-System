using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public abstract class RaceDayController : Controller
    {
        protected readonly IRaceDayApi Api;

        protected RaceDayController(IRaceDayApi api)
        {
            Api = api;
        }

        protected int? CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId);

        protected string? CurrentRole => HttpContext.Session.GetString(SessionKeys.Role);

        protected void RememberUser(int userId, string role, string fullName)
        {
            HttpContext.Session.SetInt32(SessionKeys.UserId, userId);
            HttpContext.Session.SetString(SessionKeys.Role, role);
            HttpContext.Session.SetString(SessionKeys.FullName, fullName);
        }

        protected void NoteError(string? message)
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(message)
                ? "The request could not be completed."
                : message;
        }

        protected void NoteSuccess(string message)
        {
            TempData["Success"] = message;
        }

        protected async Task<bool> OwnsEventAsync(int eventId)
        {
            var ev = await Api.GetEventAsync(eventId);
            if (ev.Success && ev.Data?.Organiser != null && ev.Data.Organiser.UserId == CurrentUserId)
            {
                return true;
            }

            NoteError("You can only manage your own events.");
            return false;
        }

        protected bool Succeeded<T>(ApiResponse<T> response)
        {
            if (response.Success)
            {
                return true;
            }

            NoteError(response.Error);
            return false;
        }
    }
}
