using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters
{
    // Blocks a logged-in user whose role does not match the page.
    public class RequireRoleAttribute : ActionFilterAttribute
    {
        private readonly string _role;

        public RequireRoleAttribute(string role)
        {
            _role = role;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            if (session.GetInt32(SessionKeys.UserId) == null)
            {
                string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                return;
            }

            if (string.Equals(session.GetString(SessionKeys.Role), _role, StringComparison.Ordinal))
            {
                return;
            }

            var tempData = context.HttpContext.RequestServices
                .GetRequiredService<ITempDataDictionaryFactory>()
                .GetTempData(context.HttpContext);
            tempData["Error"] = "You do not have access to that page.";
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }
}
