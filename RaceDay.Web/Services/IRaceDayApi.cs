using Microsoft.AspNetCore.Http;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services
{
    // The MVC project talks only to the Part 2 API. It never opens a database connection.
    public interface IRaceDayApi
    {
        Task<ApiResponse<RegisterResult>> RegisterParticipantAsync(RegisterForm form);
        Task<ApiResponse<RegisterResult>> RegisterOrganiserAsync(RegisterForm form, string inviteCode);
        Task<ApiResponse<LoginResult>> LoginAsync(string email, string password);
        Task<ApiResponse<object>> LogoutAsync();

        Task<ApiResponse<PagedEvents>> GetEventsAsync(string? eventType, string? search, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
        Task<ApiResponse<EventDetail>> GetEventAsync(int id);
        Task<ApiResponse<RacePrep>> GetRacePrepAsync(int id);
        Task<ApiResponse<List<EventTypeItem>>> GetEventTypesAsync();
        Task<ApiResponse<SavedEvent>> CreateEventAsync(EventForm form);
        Task<ApiResponse<SavedEvent>> UpdateEventAsync(int id, EventForm form);
        Task<ApiResponse<object>> DeleteEventAsync(int id);
        Task<ApiResponse<UploadResult>> UploadBannerAsync(int id, IFormFile file);
        Task<ApiResponse<List<OrganiserEventItem>>> GetOrganiserEventsAsync();
        Task<ApiResponse<OrganiserDashboard>> GetDashboardAsync();

        Task<ApiResponse<List<CategoryItem>>> GetCategoriesAsync(int eventId);
        Task<ApiResponse<CategoryItem>> CreateCategoryAsync(int eventId, CategoryForm form);
        Task<ApiResponse<CategoryItem>> UpdateCategoryAsync(int id, CategoryForm form);
        Task<ApiResponse<object>> DeleteCategoryAsync(int id);

        Task<ApiResponse<object>> EnterEventAsync(int eventId, int categoryId);
        Task<ApiResponse<List<MyEnrolment>>> GetMyEnrolmentsAsync();
        Task<ApiResponse<object>> ChangeCategoryAsync(int enrolmentId, int categoryId);
        Task<ApiResponse<object>> WithdrawAsync(int enrolmentId);
        Task<ApiResponse<List<EventEnrolmentRow>>> GetEventEnrolmentsAsync(int eventId, string? status);
        Task<ApiResponse<object>> UpdateEnrolmentStatusAsync(int enrolmentId, string status);

        Task<ApiResponse<List<MyResultItem>>> GetMyResultsAsync();
        Task<ApiResponse<List<EventResultRow>>> GetEventResultsAsync(int eventId);
        Task<ApiResponse<object>> CaptureResultAsync(int enrolmentId, string finishTime, int position);
        Task<ApiResponse<object>> UpdateResultAsync(int resultId, string finishTime, int position);

        Task<ApiResponse<UserProfile>> GetProfileAsync();
        Task<ApiResponse<UserProfile>> UpdateProfileAsync(ProfileForm form);
        Task<ApiResponse<object>> ChangePasswordAsync(PasswordForm form);
        Task<ApiResponse<UploadResult>> UploadProfilePictureAsync(IFormFile file);
    }
}
