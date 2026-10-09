namespace RaceDay.API.Models
{
    // Maps to the Events table. Every event belongs to one Organiser and one EventType.
    public class Event
    {
        public int EventId { get; set; }
        public int OrganiserId { get; set; }
        public int EventTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string? BannerImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        public User? Organiser { get; set; }
        public EventType? EventType { get; set; }
        public List<Category> Categories { get; set; } = new();
        public List<Enrolment> Enrolments { get; set; } = new();
    }
}
