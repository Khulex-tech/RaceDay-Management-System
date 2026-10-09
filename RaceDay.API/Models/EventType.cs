namespace RaceDay.API.Models
{
    // Maps to the EventTypes lookup table (Run, Walk, Cycle)
    public class EventType
    {
        public int EventTypeId { get; set; }
        public string Name { get; set; } = string.Empty;

        public List<Event> Events { get; set; } = new();
    }
}
