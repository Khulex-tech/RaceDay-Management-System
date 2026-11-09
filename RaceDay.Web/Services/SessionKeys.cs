namespace RaceDay.Web.Services
{
    // Keys stored in the MVC session. The API cookie is kept here so each browser session
    // sends only its own RaceDay.Session value to the API.
    public static class SessionKeys
    {
        public const string UserId = "UserId";
        public const string Role = "Role";
        public const string FullName = "FullName";
        public const string ApiCookie = "ApiSessionCookie";
    }
}
