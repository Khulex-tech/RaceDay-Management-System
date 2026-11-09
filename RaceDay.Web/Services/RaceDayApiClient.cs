using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services
{
    /*
     * Calls the RaceDay API and keeps the API session cookie inside the current MVC session.
     * HttpClient cookies are turned off so one user's RaceDay.Session value cannot leak to another user
     * who happens to be served by the same process.
     */
    public class RaceDayApiClient : IRaceDayApi
    {
        private const string ApiSessionCookieName = "RaceDay.Session=";
        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _accessor;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public RaceDayApiClient(HttpClient http, IHttpContextAccessor accessor)
        {
            _http = http;
            _accessor = accessor;
        }

        public Task<ApiResponse<RegisterResult>> RegisterParticipantAsync(RegisterForm form)
        {
            return SendAsync<RegisterResult>(HttpMethod.Post, "api/auth/register", new
            {
                form.FirstName,
                form.LastName,
                form.Email,
                form.Password,
                form.ConfirmPassword,
                form.PhoneNumber,
                role = "Participant"
            });
        }

        public Task<ApiResponse<RegisterResult>> RegisterOrganiserAsync(RegisterForm form, string inviteCode)
        {
            return SendAsync<RegisterResult>(HttpMethod.Post, "api/auth/register-organiser", new
            {
                form.FirstName,
                form.LastName,
                form.Email,
                form.Password,
                form.ConfirmPassword,
                form.PhoneNumber
            }, inviteCode);
        }

        public Task<ApiResponse<LoginResult>> LoginAsync(string email, string password)
        {
            return SendAsync<LoginResult>(HttpMethod.Post, "api/auth/login", new { email, password });
        }

        public Task<ApiResponse<object>> LogoutAsync()
        {
            return SendAsync<object>(HttpMethod.Post, "api/auth/logout");
        }

        public Task<ApiResponse<PagedEvents>> GetEventsAsync(string? eventType, string? search, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
        {
            string url = WithQuery("api/events", new Dictionary<string, string?>
            {
                ["eventType"] = eventType,
                ["search"] = search,
                ["fromDate"] = fromDate?.ToString("yyyy-MM-dd"),
                ["toDate"] = toDate?.ToString("yyyy-MM-dd"),
                ["page"] = page.ToString(),
                ["pageSize"] = pageSize.ToString()
            });
            return SendAsync<PagedEvents>(HttpMethod.Get, url);
        }

        public Task<ApiResponse<EventDetail>> GetEventAsync(int id) => SendAsync<EventDetail>(HttpMethod.Get, $"api/events/{id}");

        public Task<ApiResponse<RacePrep>> GetRacePrepAsync(int id) => SendAsync<RacePrep>(HttpMethod.Get, $"api/events/{id}/race-prep");

        public Task<ApiResponse<List<EventTypeItem>>> GetEventTypesAsync() => SendAsync<List<EventTypeItem>>(HttpMethod.Get, "api/event-types");

        public Task<ApiResponse<SavedEvent>> CreateEventAsync(EventForm form)
        {
            return SendAsync<SavedEvent>(HttpMethod.Post, "api/events", EventBody(form));
        }

        public Task<ApiResponse<SavedEvent>> UpdateEventAsync(int id, EventForm form)
        {
            return SendAsync<SavedEvent>(HttpMethod.Put, $"api/events/{id}", EventBody(form));
        }

        public Task<ApiResponse<object>> DeleteEventAsync(int id) => SendAsync<object>(HttpMethod.Delete, $"api/events/{id}");

        public Task<ApiResponse<UploadResult>> UploadBannerAsync(int id, IFormFile file)
        {
            return SendFileAsync($"api/events/{id}/banner", file);
        }

        public Task<ApiResponse<ImageFile>> GetEventBannerAsync(int id)
        {
            return GetFileAsync($"api/events/{id}/banner");
        }

        public Task<ApiResponse<List<OrganiserEventItem>>> GetOrganiserEventsAsync()
        {
            return SendAsync<List<OrganiserEventItem>>(HttpMethod.Get, "api/events/my-events");
        }

        public Task<ApiResponse<OrganiserDashboard>> GetDashboardAsync()
        {
            return SendAsync<OrganiserDashboard>(HttpMethod.Get, "api/dashboard/organiser");
        }

        public Task<ApiResponse<List<CategoryItem>>> GetCategoriesAsync(int eventId)
        {
            return SendAsync<List<CategoryItem>>(HttpMethod.Get, $"api/events/{eventId}/categories");
        }

        public Task<ApiResponse<CategoryItem>> CreateCategoryAsync(int eventId, CategoryForm form)
        {
            return SendAsync<CategoryItem>(HttpMethod.Post, $"api/events/{eventId}/categories", CategoryBody(form));
        }

        public Task<ApiResponse<CategoryItem>> UpdateCategoryAsync(int id, CategoryForm form)
        {
            return SendAsync<CategoryItem>(HttpMethod.Put, $"api/categories/{id}", CategoryBody(form));
        }

        public Task<ApiResponse<object>> DeleteCategoryAsync(int id) => SendAsync<object>(HttpMethod.Delete, $"api/categories/{id}");

        public Task<ApiResponse<object>> EnterEventAsync(int eventId, int categoryId)
        {
            return SendAsync<object>(HttpMethod.Post, $"api/events/{eventId}/enter", new { categoryId });
        }

        public Task<ApiResponse<List<MyEnrolment>>> GetMyEnrolmentsAsync()
        {
            return SendAsync<List<MyEnrolment>>(HttpMethod.Get, "api/enrolments/my-enrolments");
        }

        public Task<ApiResponse<object>> ChangeCategoryAsync(int enrolmentId, int categoryId)
        {
            return SendAsync<object>(HttpMethod.Put, $"api/enrolments/{enrolmentId}/category", new { categoryId });
        }

        public Task<ApiResponse<object>> WithdrawAsync(int enrolmentId)
        {
            return SendAsync<object>(HttpMethod.Delete, $"api/enrolments/{enrolmentId}");
        }

        public Task<ApiResponse<List<EventEnrolmentRow>>> GetEventEnrolmentsAsync(int eventId, string? status)
        {
            string url = WithQuery($"api/events/{eventId}/enrolments", new Dictionary<string, string?>
            {
                ["status"] = status
            });
            return SendAsync<List<EventEnrolmentRow>>(HttpMethod.Get, url);
        }

        public Task<ApiResponse<object>> UpdateEnrolmentStatusAsync(int enrolmentId, string status)
        {
            return SendAsync<object>(HttpMethod.Put, $"api/enrolments/{enrolmentId}/status", new { status });
        }

        public Task<ApiResponse<List<MyResultItem>>> GetMyResultsAsync()
        {
            return SendAsync<List<MyResultItem>>(HttpMethod.Get, "api/results/my-results");
        }

        public Task<ApiResponse<List<EventResultRow>>> GetEventResultsAsync(int eventId)
        {
            return SendAsync<List<EventResultRow>>(HttpMethod.Get, $"api/events/{eventId}/results");
        }

        public Task<ApiResponse<object>> CaptureResultAsync(int enrolmentId, string finishTime, int position)
        {
            return SendAsync<object>(HttpMethod.Post, $"api/enrolments/{enrolmentId}/result", new { finishTime, position });
        }

        public Task<ApiResponse<object>> UpdateResultAsync(int resultId, string finishTime, int position)
        {
            return SendAsync<object>(HttpMethod.Put, $"api/results/{resultId}", new { finishTime, position });
        }

        public Task<ApiResponse<UserProfile>> GetProfileAsync() => SendAsync<UserProfile>(HttpMethod.Get, "api/users/profile");

        public Task<ApiResponse<UserProfile>> UpdateProfileAsync(ProfileForm form)
        {
            return SendAsync<UserProfile>(HttpMethod.Put, "api/users/profile", new
            {
                form.FirstName,
                form.LastName,
                form.PhoneNumber
            });
        }

        public Task<ApiResponse<object>> ChangePasswordAsync(PasswordForm form)
        {
            return SendAsync<object>(HttpMethod.Put, "api/users/profile/password", new
            {
                form.CurrentPassword,
                form.NewPassword,
                form.ConfirmNewPassword
            });
        }

        public Task<ApiResponse<UploadResult>> UploadProfilePictureAsync(IFormFile file)
        {
            return SendFileAsync("api/users/profile/picture", file);
        }

        public Task<ApiResponse<ImageFile>> GetProfilePictureAsync()
        {
            return GetFileAsync("api/users/profile/picture");
        }

        private static object EventBody(EventForm form)
        {
            return new
            {
                form.Name,
                form.Description,
                form.EventDate,
                form.Location,
                form.DistanceKm,
                form.EventType
            };
        }

        private static object CategoryBody(CategoryForm form)
        {
            return new
            {
                form.CategoryName,
                form.Description,
                form.EntryFee,
                form.MinAge,
                form.MaxAge
            };
        }

        private async Task<ApiResponse<T>> SendAsync<T>(HttpMethod method, string url, object? body = null, string? inviteCode = null)
        {
            try
            {
                using var request = new HttpRequestMessage(method, url);
                AttachCookie(request);
                if (!string.IsNullOrWhiteSpace(inviteCode))
                {
                    request.Headers.TryAddWithoutValidation("X-Organiser-Invite-Code", inviteCode);
                }

                if (body != null)
                {
                    request.Content = JsonContent.Create(body, options: JsonOptions);
                }

                using var response = await _http.SendAsync(request);
                StoreCookie(response);
                return await ReadAsync<T>(response);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return ApiResponse<T>.Fail(0, "The RaceDay service is unavailable. Please try again.");
            }
        }

        private async Task<ApiResponse<UploadResult>> SendFileAsync(string url, IFormFile file)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                AttachCookie(request);
                using var content = new MultipartFormDataContent();
                await using var stream = file.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
                content.Add(fileContent, "file", file.FileName);
                request.Content = content;

                using var response = await _http.SendAsync(request);
                StoreCookie(response);
                return await ReadAsync<UploadResult>(response);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return ApiResponse<UploadResult>.Fail(0, "The RaceDay service is unavailable. Please try again.");
            }
        }

        private async Task<ApiResponse<ImageFile>> GetFileAsync(string url)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                AttachCookie(request);
                using var response = await _http.SendAsync(request);
                StoreCookie(response);
                if (!response.IsSuccessStatusCode)
                {
                    string text = await response.Content.ReadAsStringAsync();
                    return ApiResponse<ImageFile>.Fail((int)response.StatusCode, ReadError(text));
                }

                byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                return ApiResponse<ImageFile>.Ok((int)response.StatusCode, new ImageFile
                {
                    Bytes = bytes,
                    ContentType = contentType
                });
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return ApiResponse<ImageFile>.Fail(0, "The RaceDay service is unavailable. Please try again.");
            }
        }

        private async Task<ApiResponse<T>> ReadAsync<T>(HttpResponseMessage response)
        {
            string text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return ApiResponse<T>.Fail((int)response.StatusCode, ReadError(text));
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return ApiResponse<T>.Ok((int)response.StatusCode, default);
            }

            T? data = JsonSerializer.Deserialize<T>(text, JsonOptions);
            return ApiResponse<T>.Ok((int)response.StatusCode, data);
        }

        // Only the cookie belonging to this MVC session is forwarded.
        private void AttachCookie(HttpRequestMessage request)
        {
            string? cookie = _accessor.HttpContext?.Session.GetString(SessionKeys.ApiCookie);
            if (!string.IsNullOrEmpty(cookie))
            {
                request.Headers.TryAddWithoutValidation("Cookie", cookie);
            }
        }

        private void StoreCookie(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values))
            {
                return;
            }

            foreach (string value in values)
            {
                string pair = value.Split(';', 2)[0];
                if (pair.StartsWith(ApiSessionCookieName, StringComparison.OrdinalIgnoreCase))
                {
                    _accessor.HttpContext?.Session.SetString(SessionKeys.ApiCookie, pair);
                }
            }
        }

        private static string ReadError(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "The request could not be completed.";
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(text);
                if (doc.RootElement.TryGetProperty("message", out JsonElement message))
                {
                    string? value = message.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }

                if (doc.RootElement.TryGetProperty("errors", out JsonElement errors))
                {
                    var parts = new List<string>();
                    foreach (JsonProperty property in errors.EnumerateObject())
                    {
                        foreach (JsonElement item in property.Value.EnumerateArray())
                        {
                            string? itemText = item.GetString();
                            if (!string.IsNullOrWhiteSpace(itemText))
                            {
                                parts.Add(itemText);
                            }
                        }
                    }

                    if (parts.Count > 0)
                    {
                        return string.Join(" ", parts);
                    }
                }
            }
            catch (JsonException)
            {
                return "The request could not be completed.";
            }

            return "The request could not be completed.";
        }

        private static string WithQuery(string path, IDictionary<string, string?> query)
        {
            var parts = query
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}");
            string joined = string.Join("&", parts);
            return string.IsNullOrEmpty(joined) ? path : path + "?" + joined;
        }
    }
}
