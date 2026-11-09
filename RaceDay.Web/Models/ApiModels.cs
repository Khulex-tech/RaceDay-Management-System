namespace RaceDay.Web.Models
{
    public class LoginResult
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class RegisterResult
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class PagedEvents
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<EventListItem> Items { get; set; } = new();
    }

    public class EventListItem
    {
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }
        public int EnrolmentCount { get; set; }
    }

    public class EventDetail
    {
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }
        public PersonRef? Organiser { get; set; }
        public List<CategoryItem> Categories { get; set; } = new();
    }

    public class PersonRef
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
    }

    public class CategoryItem
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal EntryFee { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
        public int EnrolledCount { get; set; }
        public string? EventName { get; set; }
    }

    public class SavedEvent
    {
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }
    }

    public class EventTypeItem
    {
        public int EventTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
    }

    public class OrganiserEventItem
    {
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public decimal DistanceKm { get; set; }
        public int EnrolmentCount { get; set; }
    }

    public class OrganiserDashboard
    {
        public int TotalEvents { get; set; }
        public int TotalEnrolments { get; set; }
        public List<DashboardEvent> UpcomingEvents { get; set; } = new();
    }

    public class DashboardEvent
    {
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public int EnrolmentCount { get; set; }
    }

    public class MyEnrolment
    {
        public int EnrolmentId { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime EnrolledAt { get; set; }
    }

    public class EventEnrolmentRow
    {
        public int EnrolmentId { get; set; }
        public PersonRef? Participant { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime EnrolledAt { get; set; }
        public int? ResultId { get; set; }
        public string? FinishTime { get; set; }
        public int? Position { get; set; }
    }

    public class MyResultItem
    {
        public int ResultId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string FinishTime { get; set; } = string.Empty;
        public int Position { get; set; }
        public int TotalFinishers { get; set; }
    }

    public class EventResultRow
    {
        public int ResultId { get; set; }
        public int EnrolmentId { get; set; }
        public int Position { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string FinishTime { get; set; } = string.Empty;
        public int TotalFinishers { get; set; }
    }

    public class UserProfile
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    public class ImageFile
    {
        public byte[] Bytes { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
    }

    public class UploadResult
    {
        public int EventId { get; set; }
        public string? BannerImageUrl { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }

    public class RacePrep
    {
        public int EventId { get; set; }
        public RouteInfo? Route { get; set; }
        public WeatherInfo? Weather { get; set; }
    }

    public class RouteInfo
    {
        public string StartLocation { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<RouteOption> Options { get; set; } = new();
    }

    public class RouteOption
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class WeatherInfo
    {
        public bool Available { get; set; }
        public string? LocationName { get; set; }
        public double? TemperatureC { get; set; }
        public string? Conditions { get; set; }
        public double? WindKmh { get; set; }
        public string? Source { get; set; }
        public string? Note { get; set; }
        public string? Message { get; set; }
    }
}
