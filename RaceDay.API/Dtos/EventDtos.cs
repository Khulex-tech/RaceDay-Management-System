using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Dtos
{
    // Body for POST /api/events and PUT /api/events/{id}
    public class EventRequestDto
    {
        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime EventDate { get; set; }

        [Required, StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [Range(0.01, 9999.99, ErrorMessage = "Distance must be greater than 0.")]
        public decimal DistanceKm { get; set; }

        // Run, Walk or Cycle (checked against the EventTypes table)
        [Required]
        public string EventType { get; set; } = string.Empty;
    }
}
