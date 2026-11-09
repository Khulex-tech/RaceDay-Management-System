using System.Net;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Tests
{
    public class MvcFlowTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public MvcFlowTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IRaceDayApi>();
                    services.AddSingleton<IRaceDayApi, ScriptedApi>();
                });
            });
        }

        [Fact]
        public async Task OrganiserDashboard_RedirectsAnonymousVisitorsToLogin()
        {
            using var client = CreateClient();
            var response = await client.GetAsync("/organiser");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task Participant_CannotOpenTheOrganiserDashboard()
        {
            using var client = CreateClient();
            await LoginAsync(client, "participant@raceday.test");
            var response = await client.GetAsync("/organiser");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            string location = response.Headers.Location?.OriginalString ?? "";
            Assert.True(location == "/" || location.Contains("/Home", StringComparison.Ordinal));
        }

        [Fact]
        public async Task OrganiserAndParticipant_SeeDifferentMenus()
        {
            using var organiser = CreateClient();
            using var participant = CreateClient();
            await LoginAsync(organiser, "organiser@raceday.test");
            await LoginAsync(participant, "participant@raceday.test");

            string organiserHtml = await organiser.GetStringAsync("/organiser");
            string participantHtml = await participant.GetStringAsync("/");

            Assert.Contains("Organiser dashboard", organiserHtml);
            Assert.DoesNotContain("My Results", organiserHtml);
            Assert.Contains("My Results", participantHtml);
            Assert.DoesNotContain("href=\"/organiser\"", participantHtml);
        }

        [Fact]
        public async Task OtherOrganiser_CannotOpenSomeoneElsesEvent()
        {
            using var client = CreateClient();
            await LoginAsync(client, "other@raceday.test");
            var response = await client.GetAsync("/organiser/events/3/edit");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/organiser/events", response.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task Logout_ClearsAccessToProtectedPages()
        {
            using var client = CreateClient();
            await LoginAsync(client, "organiser@raceday.test");
            string token = await TokenAsync(client, "/organiser");
            var logout = await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token
            }));
            Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);

            var dashboard = await client.GetAsync("/organiser");
            Assert.Equal(HttpStatusCode.Redirect, dashboard.StatusCode);
            Assert.Contains("/Account/Login", dashboard.Headers.Location?.OriginalString);
        }

        private HttpClient CreateClient()
        {
            return _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });
        }

        private static async Task LoginAsync(HttpClient client, string email)
        {
            string token = await TokenAsync(client, "/Account/Login");
            var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = "password1",
                ["__RequestVerificationToken"] = token
            }));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        private static async Task<string> TokenAsync(HttpClient client, string path)
        {
            string html = await client.GetStringAsync(path);
            var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
            Assert.True(match.Success, "Anti-forgery token was not rendered.");
            return match.Groups[1].Value;
        }

        private sealed class ScriptedApi : IRaceDayApi
        {
            public Task<ApiResponse<PagedEvents>> GetEventsAsync(string? eventType, string? search, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
            {
                return Task.FromResult(ApiResponse<PagedEvents>.Ok(200, new PagedEvents
                {
                    Page = 1,
                    PageSize = pageSize,
                    TotalCount = 1,
                    Items = new List<EventListItem>
                    {
                        new()
                        {
                            EventId = 3,
                            Name = "Durban Beachfront Charity Walk",
                            EventDate = DateTime.Now.AddDays(40),
                            Location = "Durban",
                            DistanceKm = 10,
                            EventType = "Walk"
                        }
                    }
                }));
            }

            public Task<ApiResponse<List<EventTypeItem>>> GetEventTypesAsync()
            {
                return Task.FromResult(ApiResponse<List<EventTypeItem>>.Ok(200, new List<EventTypeItem>
                {
                    new() { EventTypeId = 1, TypeName = "Run" },
                    new() { EventTypeId = 2, TypeName = "Walk" },
                    new() { EventTypeId = 3, TypeName = "Cycle" }
                }));
            }

            public Task<ApiResponse<OrganiserDashboard>> GetDashboardAsync()
            {
                return Task.FromResult(ApiResponse<OrganiserDashboard>.Ok(200, new OrganiserDashboard
                {
                    TotalEvents = 2,
                    TotalEnrolments = 4,
                    UpcomingEvents = new List<DashboardEvent>
                    {
                        new() { EventId = 3, Name = "Durban Beachfront Charity Walk", EventDate = DateTime.Now.AddDays(40), EnrolmentCount = 2 }
                    }
                }));
            }

            public Task<ApiResponse<object>> LogoutAsync() => Task.FromResult(ApiResponse<object>.Ok(200, null));
            public Task<ApiResponse<RegisterResult>> RegisterParticipantAsync(RegisterForm form) => Task.FromResult(ApiResponse<RegisterResult>.Ok(201, new RegisterResult()));
            public Task<ApiResponse<RegisterResult>> RegisterOrganiserAsync(RegisterForm form, string inviteCode) => Task.FromResult(ApiResponse<RegisterResult>.Ok(201, new RegisterResult()));
            public Task<ApiResponse<LoginResult>> LoginAsync(string email, string password)
            {
                bool organiser = email.Contains("organiser", StringComparison.OrdinalIgnoreCase);
                bool other = email.Contains("other", StringComparison.OrdinalIgnoreCase);
                return Task.FromResult(ApiResponse<LoginResult>.Ok(200, new LoginResult
                {
                    UserId = other ? 9 : organiser ? 1 : 2,
                    FullName = other ? "Ayesha Patel" : organiser ? "Thabo Mokoena" : "Sipho Ndlovu",
                    Role = organiser || other ? "Organiser" : "Participant"
                }));
            }

            public Task<ApiResponse<EventDetail>> GetEventAsync(int id) => Task.FromResult(ApiResponse<EventDetail>.Ok(200, new EventDetail
            {
                EventId = id,
                Name = "Event",
                EventType = "Run",
                Organiser = new PersonRef { UserId = 1, FullName = "Thabo Mokoena" }
            }));
            public Task<ApiResponse<RacePrep>> GetRacePrepAsync(int id) => Task.FromResult(ApiResponse<RacePrep>.Ok(200, new RacePrep()));
            public Task<ApiResponse<SavedEvent>> CreateEventAsync(EventForm form) => Task.FromResult(ApiResponse<SavedEvent>.Ok(201, new SavedEvent { EventId = 9 }));
            public Task<ApiResponse<SavedEvent>> UpdateEventAsync(int id, EventForm form) => Task.FromResult(ApiResponse<SavedEvent>.Ok(200, new SavedEvent { EventId = id }));
            public Task<ApiResponse<object>> DeleteEventAsync(int id) => Task.FromResult(ApiResponse<object>.Ok(204, null));
            public Task<ApiResponse<UploadResult>> UploadBannerAsync(int id, IFormFile file) => Task.FromResult(ApiResponse<UploadResult>.Ok(200, new UploadResult()));
            public Task<ApiResponse<ImageFile>> GetEventBannerAsync(int id) => Task.FromResult(ApiResponse<ImageFile>.Fail(404, "No banner."));
            public Task<ApiResponse<List<OrganiserEventItem>>> GetOrganiserEventsAsync() => Task.FromResult(ApiResponse<List<OrganiserEventItem>>.Ok(200, new List<OrganiserEventItem>()));
            public Task<ApiResponse<List<CategoryItem>>> GetCategoriesAsync(int eventId) => Task.FromResult(ApiResponse<List<CategoryItem>>.Ok(200, new List<CategoryItem>()));
            public Task<ApiResponse<CategoryItem>> CreateCategoryAsync(int eventId, CategoryForm form) => Task.FromResult(ApiResponse<CategoryItem>.Ok(201, new CategoryItem()));
            public Task<ApiResponse<CategoryItem>> UpdateCategoryAsync(int id, CategoryForm form) => Task.FromResult(ApiResponse<CategoryItem>.Ok(200, new CategoryItem()));
            public Task<ApiResponse<object>> DeleteCategoryAsync(int id) => Task.FromResult(ApiResponse<object>.Ok(204, null));
            public Task<ApiResponse<object>> EnterEventAsync(int eventId, int categoryId) => Task.FromResult(ApiResponse<object>.Ok(201, null));
            public Task<ApiResponse<List<MyEnrolment>>> GetMyEnrolmentsAsync() => Task.FromResult(ApiResponse<List<MyEnrolment>>.Ok(200, new List<MyEnrolment>()));
            public Task<ApiResponse<object>> ChangeCategoryAsync(int enrolmentId, int categoryId) => Task.FromResult(ApiResponse<object>.Ok(200, null));
            public Task<ApiResponse<object>> WithdrawAsync(int enrolmentId) => Task.FromResult(ApiResponse<object>.Ok(204, null));
            public Task<ApiResponse<List<EventEnrolmentRow>>> GetEventEnrolmentsAsync(int eventId, string? status) => Task.FromResult(ApiResponse<List<EventEnrolmentRow>>.Ok(200, new List<EventEnrolmentRow>()));
            public Task<ApiResponse<object>> UpdateEnrolmentStatusAsync(int enrolmentId, string status) => Task.FromResult(ApiResponse<object>.Ok(200, null));
            public Task<ApiResponse<List<MyResultItem>>> GetMyResultsAsync() => Task.FromResult(ApiResponse<List<MyResultItem>>.Ok(200, new List<MyResultItem>()));
            public Task<ApiResponse<List<EventResultRow>>> GetEventResultsAsync(int eventId) => Task.FromResult(ApiResponse<List<EventResultRow>>.Ok(200, new List<EventResultRow>()));
            public Task<ApiResponse<object>> CaptureResultAsync(int enrolmentId, string finishTime, int position) => Task.FromResult(ApiResponse<object>.Ok(201, null));
            public Task<ApiResponse<object>> UpdateResultAsync(int resultId, string finishTime, int position) => Task.FromResult(ApiResponse<object>.Ok(200, null));
            public Task<ApiResponse<UserProfile>> GetProfileAsync() => Task.FromResult(ApiResponse<UserProfile>.Ok(200, new UserProfile()));
            public Task<ApiResponse<UserProfile>> UpdateProfileAsync(ProfileForm form) => Task.FromResult(ApiResponse<UserProfile>.Ok(200, new UserProfile()));
            public Task<ApiResponse<object>> ChangePasswordAsync(PasswordForm form) => Task.FromResult(ApiResponse<object>.Ok(200, null));
            public Task<ApiResponse<UploadResult>> UploadProfilePictureAsync(IFormFile file) => Task.FromResult(ApiResponse<UploadResult>.Ok(200, new UploadResult()));
            public Task<ApiResponse<ImageFile>> GetProfilePictureAsync() => Task.FromResult(ApiResponse<ImageFile>.Fail(404, "No picture."));
        }
    }
}
