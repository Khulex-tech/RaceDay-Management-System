using Microsoft.AspNetCore.Mvc;

namespace RaceDay.API.Controllers
{
    /*
     * Base class for all RaceDay controllers.
     * It reads the logged-in user from the session and gives short helper methods
     * for the 401 and 403 responses used throughout the API.
     */
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        // Session keys set by AuthController on login
        protected const string SessionUserId = "UserId";
        protected const string SessionRole = "Role";

        // The logged-in user's id, or null when there is no session
        protected int? CurrentUserId => HttpContext.Session.GetInt32(SessionUserId);

        // The logged-in user's role, or null when there is no session
        protected string? CurrentRole => HttpContext.Session.GetString(SessionRole);

        protected ActionResult NotLoggedIn()
        {
            return Unauthorized(new { message = "You must be logged in." });
        }

        protected ActionResult NoAccess(string message)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message });
        }
    }
}
