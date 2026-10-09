using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Dtos
{
    // Body for POST /api/events/{eventId}/enter and PUT /api/enrolments/{id}/category
    public class CategoryChoiceDto
    {
        [Required]
        public int CategoryId { get; set; }
    }

    // Body for PUT /api/enrolments/{id}/status
    public class EnrolmentStatusDto
    {
        // Pending, Confirmed or Cancelled
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
