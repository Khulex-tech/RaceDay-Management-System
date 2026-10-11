using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Tests.TestHelpers
{
    /*
     * Configures ControllerContext with HttpContext and TestSession for testing authorization and identity.
     */
    public static class ControllerTestExtensions
    {
        public static T WithSession<T>(this T controller, int? userId = null, string? role = null) where T : ControllerBase
        {
            var session = new TestSession();
            if (userId.HasValue)
            {
                session.SetInt32("UserId", userId.Value);
            }
            if (!string.IsNullOrEmpty(role))
            {
                session.SetString("Role", role);
            }

            var httpContext = new DefaultHttpContext
            {
                Session = session
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }
    }
}
