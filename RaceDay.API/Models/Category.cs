namespace RaceDay.API.Models
{
    // Maps to the Categories table (an entry option for an event, e.g. 21.1km Half or Under 20)
    public class Category
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal EntryFee { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
        public DateTime CreatedAt { get; set; }

        public Event? Event { get; set; }
        public List<Enrolment> Enrolments { get; set; } = new();
    }
}
